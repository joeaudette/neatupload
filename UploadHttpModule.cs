/*

NeatUpload - an HttpModule and User Control for uploading large files
Copyright (C) 2005  Dean Brettle

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Configuration;
using System.Web;
using System.Threading;
using System.Security.Permissions;
using System.Collections.Specialized;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Limits the size of incoming HTTP requests and allows <see cref="ProgressBar"/> to monitor the progress of 
	/// upload requests associated with <see cref="InputFile"/> controls.</summary>
	/// <remarks>
	/// To use this module, add it to the <see href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/gngrfhttpmodulessection.asp">
	/// httpModules section</see> of your Web.config like this:
	/// <example>
	/// <code escaped="true">
	/// <configuration>
	///   <system.web>
	///	    <httpModules>
	///		  <add name="UploadHttpModule" type="Brettle.Web.NeatUpload.UploadHttpModule, Brettle.Web.NeatUpload" />
	///	    </httpModules>
	///   </system.web>
	/// </configuration>
	/// </code>
	/// </example>
	/// </remarks>
	public class UploadHttpModule : IHttpModule
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void AppendToLog(string param)
		{
			HttpContext context = HttpContext.Current;
			if (context == null)
			{
				return;
			}
			HttpContext origContext = context.Items["NeatUpload_origContext"] as HttpContext;
			if (origContext != null)
			{
				context = origContext;
			}
			context.Response.AppendToLog(param);
		}
		
		public static UploadedFileCollection Files
		{
			get 
			{
				// If the upload is being handled by this module, then return the collection that it maintains.
				FilteringWorkerRequest worker = UploadHttpModule.GetCurrentWorkerRequest() as FilteringWorkerRequest;
				if (worker != null) return worker.GetUploadContext().Files;
				// Otherwise return a fake one that will use the HttpPostedFiles in the Request.Files collection.
				HttpContext ctx = HttpContext.Current;
				UploadedFileCollection aspNetFiles = ctx.Items["NeatUpload_AspNetFiles"] as UploadedFileCollection;
				if (aspNetFiles == null)
				{
					ctx.Items["NeatUpload_AspNetFiles"] = aspNetFiles = new UploadedFileCollection();
					HttpFileCollection files = ctx.Request.Files;
					string[] fieldNames = files.AllKeys;
					for (int i = 0; i < fieldNames.Length; i++)
					{
						AspNetUploadedFile aspNetFile = new AspNetUploadedFile(fieldNames[i]);
						aspNetFiles.Add(fieldNames[i], aspNetFile);
					}
				}
				return aspNetFiles;
			}
		}
		
		/// <summary>
		/// Waits for the current upload request to finish.</summary>
		/// <remarks>
		/// <para>
		/// If the UploadHttpModule is being used for the current request, this method will not return until the
		/// module has received and processed the entire request.  If the UploadHttpModule is not being used for
		/// the current request, this method will return immediately.  Note: the UploadHttpModule is only used if
		/// it has been added in the httpModules section of the Web.config, the neatUpload section's
		/// useHttpModule attribute is "true" for the page being requested (which is the default), and the
		/// request has a content type of multipart/form-data.</para>
		/// </remarks>
		public static void WaitForUploadToComplete()
		{
			// If the original request hasn't been parsed (and any upload received) by now,
			// we force parsing to ensure that the upload is received.
			FilteringWorkerRequest worker = GetCurrentWorkerRequest() as FilteringWorkerRequest;
			if (worker != null)
			{
				worker.ParseMultipart();
			}
		}
				
		public static long MaxNormalRequestLength
		{
			get
			{
				return Config.Current.MaxNormalRequestLength;
			}
		}

		public static long MaxRequestLength
		{
			get
			{
				return Config.Current.MaxRequestLength;
			}
		}

		private static bool _isInited = false;		
		internal static bool IsInited
		{
			get { lock (typeof(UploadHttpModule)) { return _isInited;} }
		}
						
		public void Init(HttpApplication app)
		{
			// When tracing is enabled at the application level ASP.NET reads the entire request before
			// BeginRequest is fired.  So, we should not use our module at all.
			bool isTracingEnabled = HttpContext.Current.Trace.IsEnabled;
			if (isTracingEnabled)
			{
				lock (typeof(UploadHttpModule))
				{
					_isInited = false;
				}
				return;
			}
			
			app.BeginRequest += new System.EventHandler(Application_BeginRequest);
			app.Error += new System.EventHandler(Application_Error);
			app.EndRequest += new System.EventHandler(Application_EndRequest);
			app.PreSendRequestHeaders += new System.EventHandler(Application_PreSendRequestHeaders);
			app.ResolveRequestCache += new System.EventHandler(Application_ResolveRequestCache);
			app.AcquireRequestState += new System.EventHandler(Application_AcquireRequestState);
			app.ReleaseRequestState += new System.EventHandler(Application_ReleaseRequestState);
			app.PreRequestHandlerExecute += new System.EventHandler(Application_PreRequestHandlerExecute);
			RememberErrorHandler = new System.EventHandler(RememberError);
			
			lock (typeof(UploadHttpModule))
			{
				_isInited = true;
			}
		}		
		
		public void Dispose()
		{
		}
		
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
		internal static HttpWorkerRequest GetCurrentWorkerRequest()
		{
			HttpContext context = HttpContext.Current;
			IServiceProvider provider = (IServiceProvider)context;
			if (provider == null)
			{
				return null;
			}
			HttpWorkerRequest worker = (HttpWorkerRequest) provider.GetService(typeof(HttpWorkerRequest));
			return worker;
		}

		internal static HttpWorkerRequest GetOrigWorkerRequest()
		{
			HttpWorkerRequest worker = GetCurrentWorkerRequest();
			DecoratedWorkerRequest decoratedWorker = worker as DecoratedWorkerRequest;
			if (decoratedWorker != null)
			{
				worker = decoratedWorker.OrigWorker;
			}
			return worker;
		}

		bool requestHandledBySubRequest;

		private void Application_BeginRequest(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_BeginRequest");
			HttpWorkerRequest origWorker = GetCurrentWorkerRequest();
			if (origWorker == null)
			{
				if (log.IsDebugEnabled) log.Debug("origWorker = null");
				return;
			}
				
			if (log.IsDebugEnabled) log.Debug(origWorker.GetType() + " for " + origWorker.GetRawUrl() + " with AspFilterSessionId = " + origWorker.GetUnknownRequestHeader("AspFilterSessionId"));
			requestHandledBySubRequest = false;
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
			HttpApplication app = sender as HttpApplication;
			string rawUrl = app.Context.Request.RawUrl;
			log4net.ThreadContext.Properties["url"] = rawUrl;

			if (origWorker is DecoratedWorkerRequest)
			{
				// If an unhandled error occurs, we want to remember it so that we can rethrow it
				// in the original context.
				if (RememberErrorHandler != null)
				{
					app.Error += RememberErrorHandler;
				}
				// Save a reference to the original HttpContext in the subrequest context so that 
				// AppendToLog() can use it.
				DecoratedWorkerRequest decoratedWorkerRequest = origWorker as DecoratedWorkerRequest;
				if (decoratedWorkerRequest.OrigContext != null)
				{
					HttpContext.Current.Items["NeatUpload_origContext"] = decoratedWorkerRequest.OrigContext;
				}
				// Ignore the subrequests to avoid infinite recursion...
				return;
			}

			// Get the Content-Length header and parse it if we find it.  If it's not present we might
			// still be OK.
			long contentLength = 0;
			string contentLengthHeader = origWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentLength);
			if (contentLengthHeader != null)
			{
				try
				{
					contentLength = Int64.Parse(contentLengthHeader);
				}
				catch (Exception ex)
				{
					throw new HttpException(400, "Bad Request", ex);
				}
			}
			
			DecoratedWorkerRequest subWorker = null;
			
			// Create a subrequest for each request.  For multipart/form-data requests, we use a 
			// FilteringWorkerRequest which filters the file parts into temp files.  For all other
			// requests, we use a SizeLimitingWorkerRequest to ensure that the size of the request is within
			// the user configured limit.  We need the SizeLimitingWorkerRequest, because httpRuntime's 
			// maxRequestLength attribute needs to be set to a large value to allow large file upload request
			// to get to this module at all.  That means that large normal requests will also get to this
			// module.  SizeLimitingWorkerRequest ensures that normal requests which are too large are
			// rejected.
			string contentTypeHeader = origWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentType);
			if (contentTypeHeader != null && contentTypeHeader.ToLower().StartsWith("multipart/form-data"))
			{
				subWorker = new FilteringWorkerRequest(origWorker);
			}
			else
			{
				// If the client-specified content length is too large, we reject the request
				// immediately.  If it's not, the client could be lying so we need to use
				// SizeLimitingWorkerRequest to actually count the bytes.
				if (contentLength > MaxNormalRequestLength)
				{
					throw new HttpException(413, "Request Entity Too Large");
				}
				subWorker = new SizeLimitingWorkerRequest(origWorker, MaxNormalRequestLength);
//				subWorker = null;
			}
			
			if (subWorker != null)
			{
				// Process the subrequest.
				HttpContext savedContext = HttpContext.Current;
				try
				{
					subWorker.ProcessRequest(null);
					if (log.IsDebugEnabled) log.Debug("Called ProcessRequest().  Calling subWorker.WaitForEndOfRequest().");
					subWorker.WaitForEndOfRequest();
					if (log.IsDebugEnabled) log.Debug("subWorker.WaitForEndOfRequest() returned.");
					requestHandledBySubRequest = true;
				}
				finally
				{
					HttpContext.Current = savedContext;
					log4net.ThreadContext.Properties["url"] = rawUrl;
					
					// Workaround for bug in mod_mono (at least rev 1.0.9) where the response status
					// is overwritten with 200 when app.CompleteRequest() is called.  Status (and headers)
					// *should* be ignored because they were already sent when the subrequest was processed...
					app.Response.StatusCode = subWorker.StatusCode;
					app.Response.StatusDescription = subWorker.StatusDescription;

					// If there was an error, rethrow it so that ASP.NET uses any custom error pages.
					if (subWorker.Exception != null)
					{
						requestHandledBySubRequest = false;
						HttpException httpException = subWorker.Exception as HttpException;
						if (httpException != null)
						{
							throw new HttpException(httpException.GetHttpCode(), "Unhandled HttpException while processing NeatUpload child request",
											httpException);
						}
						UploadException uploadException = subWorker.Exception as UploadException;
						if (uploadException != null)
						{
							throw new HttpException(uploadException.HttpCode, "Unhandled UploadException while processing NeatUpload child request",
											uploadException);
						}
						
						throw new Exception("Unhandled Exception while processing NeatUpload child request",
											subWorker.Exception);
					}

					// Otherwise call CompleteRequest() to prevent further processing of the original request.
					app.CompleteRequest();
				}
			}
		}

		private void Application_ResolveRequestCache(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_ResolveRequestCache");
			// Wait for the upload to complete before AcquireRequestState fires.  If we don't then the session
			// will be locked while the upload completes
			WaitForUploadToComplete();
            UploadContext uploadContext = UploadContext.Current;
            if (uploadContext == null)
            {
                // If a FilteringWorkerRequest was not used, but the request contains the PostBackIDQueryParam, 
                // then create and register an UploadContext.  This occurs the module disabled, 
                // this is not a form/multipart POST request, etc.  This allows ProgressBars
                // to be used for pages where no upload is occuring.
                HttpContext httpContext = HttpContext.Current;
                string postBackID = httpContext.Request.Params[Config.Current.PostBackIDQueryParam];
                if (postBackID != null)
                {
                    uploadContext = new UploadContext();
                    uploadContext.SetContentLength(HttpContext.Current.Request.ContentLength);
                    uploadContext.RegisterPostBack(postBackID);
                    UploadContext.Current = uploadContext;
                }
            }

            if (uploadContext != null)
            {
                uploadContext.Status = UploadStatus.ProcessingInProgress;
				UploadHttpModule.AccessSession(new SessionAccessCallback(uploadContext.SyncWithSession));
            }
        }

		private void Application_PreSendRequestHeaders(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_PreSendRequestHeaders");
			if (requestHandledBySubRequest)
			{
				HttpApplication app = sender as HttpApplication;
				app.Response.ClearHeaders();
				app.Response.ClearContent();
			}
		}

		private void Application_AcquireRequestState(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_AcquireRequestState");
			HttpContext.Current.Items["NeatUpload_RequestStateAcquired"] = true;
		}

		private void Application_ReleaseRequestState(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_ReleaseRequestState");
			HttpContext.Current.Items.Remove("NeatUpload_RequestStateAcquired");
		}

		private void Application_PreRequestHandlerExecute(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_PreRequestHandlerExecute");
		}

		private void Application_Error(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_Error");
			HttpApplication app = sender as HttpApplication;
			if (log.IsDebugEnabled) log.DebugFormat("Error is: {0}", app.Server.GetLastError());

			UploadContext uploadContext = UploadContext.Current;
			if (uploadContext != null)
			{
				uploadContext.RemoveUploadedFiles();
			}
		}

		private EventHandler RememberErrorHandler; 
		private void RememberError(object sender, EventArgs e)
		{
			DecoratedWorkerRequest decoratedWorker = GetCurrentWorkerRequest() as DecoratedWorkerRequest;
			HttpApplication app = sender as HttpApplication;
			
			if (decoratedWorker != null)
			{
				decoratedWorker.Exception = app.Server.GetLastError();
				if (log.IsDebugEnabled) log.DebugFormat("Remembering error: {0}", decoratedWorker.Exception);
				if (decoratedWorker.Exception != null)
				{
					// For the remainder of the subrequest, act as though there was no error.
					app.Server.ClearError();
					
					// Clear any content that has not yet been written to the client so that it isn't written
					// when we end the subrequest.
					app.Response.ClearHeaders();
					app.Response.ClearContent();
					
					// Finish the subrequest.
					app.CompleteRequest();
				}
			}
		}		

		private void Application_EndRequest(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_EndRequest");

			HttpApplication app = sender as HttpApplication;
			if (RememberErrorHandler != null)
			{
				app.Error -= RememberErrorHandler;
			}
			UploadContext uploadContext = UploadContext.Current;
			if (uploadContext != null)
			{
				uploadContext.RemoveUploadedFiles();
				if (uploadContext.Status != UploadStatus.Failed && uploadContext.Status != UploadStatus.Rejected)
				{
					uploadContext.Status = UploadStatus.Completed;
					UploadHttpModule.AccessSession(new SessionAccessCallback(uploadContext.SyncWithSession));
				}
			}
		}
		
		public static void AccessSession(SessionAccessCallback accessor)
		{
			if (log.IsDebugEnabled) log.Debug("In AccessSession");
			HttpContext savedContext = HttpContext.Current;
            if (savedContext == null)
            {
                return;
            }
            if (savedContext.Session != null && savedContext.Items["NeatUpload_RequestStateAcquired"] != null)
            {
                throw new NotSupportedException("Can't access the session because it is locked.  Your"
                    + " page must use EnableSessionState='false' to call this method.  See the NeatUpload"
                    + " documentation for details.");
            }
			
			string page = "NeatUpload/AccessSession.aspx";
			SessionAccessingWorkerRequest req 
				= SessionAccessingWorkerRequest.Create(GetOrigWorkerRequest(), page, accessor);
			try
			{
				req.ProcessRequest(null);
				req.WaitForEndOfRequest();
			}
			finally
			{
				HttpContext.Current = savedContext;
			}		
		}
	}
}
