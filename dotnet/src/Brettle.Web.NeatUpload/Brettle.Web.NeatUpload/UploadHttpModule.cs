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
using System.Collections;
using System.Text.RegularExpressions;
using System.Reflection;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// An <see cref="IUploadModule"/> and <see cref="IMultiRequestUploadModule"/> that uses child requests
	/// to extract uploaded files and maintain upload state across a web garden/farm.</summary>
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
	public class UploadHttpModule : IHttpModule, IMultiRequestUploadModule
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		string IUploadModule.PostBackIDQueryParam { 
			get { return Config.Current.PostBackIDQueryParam; } 
		}

		string IUploadModule.PostBackIDFieldName { 
			get { return Config.Current.PostBackIDQueryParam; } 
		}

		string IUploadModule.FileFieldNamePrefix { 
			get { return Constants.NamePrefix; } 
		}

		string IUploadModule.ConfigFieldNamePrefix { 
			get { return Constants.ConfigNamePrefix; } 
		}

		bool IUploadModule.IsEnabled {
			get { return Config.Current.UseHttpModule; } 
		}

        UploadStorageConfig IUploadModule.CreateUploadStorageConfig()
        {
            return null;
        }

        void IUploadModule.AppendToLog(string param)
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

        [Obsolete("Use UploadModule.AppendToLog() instead.")]
		public static void AppendToLog(string param)
		{
            UploadModule.AppendToLog(param);
		}

		UploadedFileCollection IUploadModule.Files {
			get { return UploadHttpModule.Files; }
		}

		string IUploadModule.PostBackID {
			get {
				if (CurrentUploadState == null)
					return null;
				return CurrentUploadState.PostBackID;
			}
		}

		bool IUploadModule.SetProcessingState(string controlUniqueID, object state)
		{
			
			UploadState uploadState = CurrentUploadState;
			if (uploadState != null && uploadState.Status == UploadStatus.Unknown)
				return true;
			uploadState.ProcessingStateDict[controlUniqueID] = state;
            uploadState.OnChanged();
			return true;
		}

		void IUploadModule.BindProgressState(string postBackID, string controlUniqueID, IUploadProgressState progressState)
		{
			UploadState uploadState = UploadStateStore.OpenReadOnly(postBackID);
			if (uploadState == null)
			{
				progressState.Status = UploadStatus.Unknown;
				return;
			}

			progressState.Status = uploadState.Status;
			if (uploadState.Status == UploadStatus.Unknown)
				return;			
			progressState.BytesRead = uploadState.BytesRead;
			progressState.BytesTotal = uploadState.BytesTotal;
			progressState.BytesPerSec = uploadState.BytesPerSec;
			progressState.FileBytesRead = uploadState.FileBytesRead;
			progressState.FractionComplete 
				= (uploadState.BytesTotal <= 0 || uploadState.FileBytesRead <= 0) 
                ? 0 
                : ((double)uploadState.BytesRead / uploadState.BytesTotal);
            progressState.CurrentFileName = uploadState.CurrentFileName;
			progressState.Files = uploadState.Files.GetReadOnlyCopy();
			progressState.Failure = uploadState.Failure;
			progressState.Rejection = uploadState.Rejection;
			progressState.ProcessingState = uploadState.ProcessingStateDict[controlUniqueID];
			progressState.TimeElapsed = uploadState.TimeElapsed;
			if (uploadState.BytesRead == 0 || uploadState.BytesTotal < 0)
			{
                progressState.Status = UploadStatus.Unknown;
				progressState.TimeRemaining = TimeSpan.MaxValue;
			}
			else
			{
				double bytesRemaining = ((double)(uploadState.BytesTotal - uploadState.BytesRead));
				double ticksRemaining = bytesRemaining * uploadState.TimeElapsed.Ticks;
				progressState.TimeRemaining = new TimeSpan((long)(ticksRemaining/uploadState.BytesRead));
			}
			UploadStateStore.Close(uploadState);
		}

		void IUploadModule.CancelPostBack(string postBackID)
		{
			UploadState uploadState = UploadStateStore.OpenReadWrite(postBackID);
			if (uploadState.Status == UploadStatus.Unknown)
				return;
			uploadState.Status = UploadStatus.Cancelled;
			UploadStateStore.Close(uploadState);
		}

		UploadedFile IUploadModule.ConvertToUploadedFile(string controlUniqueID, HttpPostedFile file)
		{
			return UploadStorage.ConvertToUploadedFile(controlUniqueID, file);
		}

		string IMultiRequestUploadModule.FileSizesFieldName {
			get { return Constants.FileSizesName; } 
		}

		string IMultiRequestUploadModule.UploadPath {
			get { return Config.Current.MultiRequestUploadHandlerUrl; }
		}

		string IMultiRequestUploadModule.ControlIDQueryParam {
			get { return "NeatUpload_MultiRequestControlID"; }
		}
		
		string IMultiRequestUploadModule.ArmoredCookiesQueryParam {
			get { return "NeatUpload_ArmoredCookies"; }
		}

        string IMultiRequestUploadModule.GetArmoredCookies()
        {
            return null;
        }

        internal static UploadedFileCollection Files
		{
			get 
			{
				// If the upload is being handled by this module, then return the collection that it maintains.
				if (!Config.Current.UseHttpModule || CurrentUploadState == null)
					return null;
				UploadedFileCollection files = CurrentUploadState.Files;
				if (files == null)
					return null;
				return files.GetReadOnlyCopy();
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
			FilteringWorkerRequest worker 
				= GetCurrentWorkerRequest() as FilteringWorkerRequest;
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

		private static object StaticSync = new object();
		
		private static bool _isInited = false;		
		internal static bool IsInited
		{
			get { lock (StaticSync) { return _isInited;} }
		}
						
		public void Init(HttpApplication app)
		{
			if (log.IsDebugEnabled) log.Debug("In UploadHttpModule.Init()");
			// When tracing is enabled at the application level ASP.NET reads the entire request before
			// BeginRequest is fired.  So, we should not use our module at all.
			bool isTracingEnabled = HttpContext.Current.Trace.IsEnabled;
			if (isTracingEnabled)
			{
				lock (StaticSync)
				{
					_isInited = false;
				}
				return;
			}
			app.BeginRequest += new System.EventHandler(Application_BeginRequest);
			app.EndRequest += new System.EventHandler(Application_EndRequest);
			app.ResolveRequestCache += new System.EventHandler(Application_ResolveRequestCache);
			app.AcquireRequestState += new System.EventHandler(Application_AcquireRequestState);
			app.ReleaseRequestState += new System.EventHandler(Application_ReleaseRequestState);
			app.PreRequestHandlerExecute += new System.EventHandler(Application_PreRequestHandlerExecute);
			app.Error += new System.EventHandler(Application_Error);
			RememberErrorHandler = new System.EventHandler(RememberError);
			
			lock (StaticSync)
			{
				_isInited = true;
			}
			if (log.IsDebugEnabled) log.Debug("Leaving UploadHttpModule.Init()");
		}		
		
		public void Dispose()
		{
		}

        private static string WorkerRequestKey = "NeatUpload_worker";

        [SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
		internal static HttpWorkerRequest GetCurrentWorkerRequest()
		{
			HttpContext context = HttpContext.Current;
            HttpWorkerRequest worker = context.Items[WorkerRequestKey] as HttpWorkerRequest;
            if (worker != null)
                return worker;
			IServiceProvider provider = (IServiceProvider)context;
			if (provider == null)
			{
				return null;
			}
			worker = (HttpWorkerRequest) provider.GetService(typeof(HttpWorkerRequest));
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

		private void Application_Error(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_Error");
		}
		
		private void Application_BeginRequest(object sender, EventArgs e)
		{
            if (log.IsDebugEnabled) log.Debug("In Application_BeginRequest");
            HttpApplication app = sender as HttpApplication;

			// Restore the cookies for the MultiRequestUploadModule.UploadPath page.
			HttpWorkerRequest wr = GetCurrentWorkerRequest();
			string filePath = wr.GetFilePath().ToLower();
            string multiRequestUploadPath = MultiRequestUploadModule.UploadPath;
            multiRequestUploadPath = app.Response.ApplyAppPathModifier(multiRequestUploadPath);
			if (log.IsDebugEnabled) log.DebugFormat("filePath={0}", filePath);
			string qs = wr.GetQueryString();
			if (filePath.StartsWith(multiRequestUploadPath.ToLower()))
			{
                // The module should always be used for the multi-request upload handler
                // Note that for compatibility reasons we can't set useHttpModule="true"
                // in the NeatUpload folder's Web.config because we can't be sure where
                // the app put the top-level <neatUpload> section and if guess wrong we
                // won't inherit the top-level settings (e.g. validationKey and 
                // encryptionKey)
                HttpContext.Current.Items["NeatUpload_UseHttpModule"] = true;
				if (qs != null)
				{
					HttpCookieCollection cookies = UploadHttpModule.GetCookiesFromQueryString(qs);
					if (log.IsDebugEnabled) log.DebugFormat("cookies={0}", cookies);
					if (cookies != null)
					{
						foreach (string k in cookies.AllKeys)
						{
							HttpCookie c = cookies[k];
							if (log.IsDebugEnabled) log.DebugFormat("Calling SetCookie({0}, {1})", c.Name, c.Value);
							SetCookie(c.Name, c.Value);
						}
					}
				}
			}
			
			if (!Config.Current.UseHttpModule)
			{
				return;
			}

			HttpWorkerRequest origWorker = GetCurrentWorkerRequest();
			if (origWorker == null)
			{
				if (log.IsDebugEnabled) log.Debug("origWorker = null");
				return;
			}
				
			if (log.IsDebugEnabled) log.Debug(origWorker.GetType() + " for " + origWorker.GetRawUrl() + " with AspFilterSessionId = " + origWorker.GetUnknownRequestHeader("AspFilterSessionId"));
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
			// requests that could contain a body, we use a SizeLimitingWorkerRequest to ensure that the 
            // size of the request is within
			// the user configured limit.  We need the SizeLimitingWorkerRequest, because httpRuntime's 
			// maxRequestLength attribute needs to be set to a large value to allow large file upload request
			// to get to this module at all.  That means that large normal requests will also get to this
			// module.  SizeLimitingWorkerRequest ensures that normal requests which are too large are
			// rejected.
			string contentTypeHeader = origWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentType);
			string transferEncodingHeader = origWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderTransferEncoding);
			if (contentTypeHeader != null && contentTypeHeader.ToLower().StartsWith("multipart/form-data"))
			{
				// If this is a multi-request upload get the post-back ID from the query string
				if (qs != null && UploadHttpModule.GetMultiRequestControlIDFromQueryString(qs) != null)
				{				
					CurrentUploadState = UploadStateStore.OpenReadWriteOrCreate(UploadHttpModule.GetPostBackIDFromQueryString(qs));
					if (transferEncodingHeader != "chunked")
						CurrentUploadState.Status = UploadStatus.NormalInProgress;
					else
						CurrentUploadState.Status = UploadStatus.ChunkedInProgress;
				}
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

                // Only requests which match the following criteria could contain a body.
                if ((transferEncodingHeader != null && transferEncodingHeader != "identity")
                    || (contentLengthHeader != null && contentLengthHeader != "0")
                    || (contentTypeHeader != null && contentTypeHeader.StartsWith("multipart/byteranges")))
                {
				    subWorker = new SizeLimitingWorkerRequest(origWorker, MaxNormalRequestLength);
                }
                else
                {
                    if (origWorker.HasEntityBody())
                        throw new HttpException(400, "Unexpected body in request for " + rawUrl);
                }
			}
			
			if (subWorker != null)
			{
                if (!ReplaceWorkerRequest(app, subWorker))
                {
                    MakeChildRequest(app, subWorker);
                }
			}
		}

        private bool ReplaceWorkerRequest(HttpApplication app, DecoratedWorkerRequest subWorker)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            HttpRequest request = app.Request;
            FieldInfo wrField = request.GetType().GetField("_wr", bindingFlags);
            // In Mono, the field has a different name.
            if (wrField == null)
                wrField = request.GetType().GetField("worker_request", bindingFlags);
            if (wrField == null)
                return false;
            wrField.SetValue(request, subWorker);
            app.Context.Items[WorkerRequestKey] = subWorker;
            return true;
        }

        private void MakeChildRequest(HttpApplication app, DecoratedWorkerRequest subWorker)
        {
            // Process the subrequest.
            HttpContext savedContext = HttpContext.Current;
            try
            {
                subWorker.ProcessRequest(null);
                if (log.IsDebugEnabled) log.Debug("Called ProcessRequest().  Calling subWorker.WaitForEndOfRequest().");
                subWorker.WaitForEndOfRequest();
                if (log.IsDebugEnabled) log.Debug("subWorker.WaitForEndOfRequest() returned.");
            }
            finally
            {
                HttpContext.Current = savedContext;
                string rawUrl = app.Context.Request.RawUrl;
                log4net.ThreadContext.Properties["url"] = rawUrl;

                // Workaround for bug in mod_mono (at least rev 1.0.9) where the response status
                // is overwritten with 200 when app.CompleteRequest() is called.  Status (and headers)
                // *should* be ignored because they were already sent when the subrequest was processed...
                app.Response.StatusCode = subWorker.StatusCode;
                app.Response.StatusDescription = subWorker.StatusDescription;

                // If there was an error, rethrow it so that ASP.NET uses any custom error pages.
                if (subWorker.Exception != null)
                {
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

		private void Application_ResolveRequestCache(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_ResolveRequestCache");
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
			// Wait for the upload to complete before AcquireRequestState fires.  If we don't then the session
			// will be locked while the upload completes
			WaitForUploadToComplete();

			// If the request is not a multi-request upload and contains the PostBackIDQueryParam,
            // then get an UploadState for it and set it's status to ProcessingInProgress.
			// This occurs when the module disabled,
            // this is not a form/multipart POST request, etc.  This allows ProgressBars
            // to be used for pages where no upload is occuring.
			if (CurrentMultiRequestControlID != null)
				return;
            UploadState uploadState = CurrentUploadState;
			if (uploadState == null)
				return;

			if (uploadState.BytesTotal == 0)
				uploadState.BytesTotal = HttpContext.Current.Request.ContentLength;
			if (uploadState.BytesRead == 0)
				uploadState.BytesRead = HttpContext.Current.Request.ContentLength;
            uploadState.Status = UploadStatus.ProcessingInProgress;
        }

		private static void SetCookie(string name, string val)
		{
			HttpCookieCollection cookies = HttpContext.Current.Request.Cookies;
			HttpCookie cookie = cookies[name];
			if (cookie == null)
			{
				cookie = new HttpCookie(name);
				cookies.Add(cookie);
			}
			cookie.Value = val;
			cookies.Set(cookie);
		}

		internal static UploadState CurrentUploadState {
			get {
				HttpContext httpContext = HttpContext.Current;
				if (httpContext != null)
				{
					if (httpContext.Items["NeatUpload_UploadState"] == null)
					{
						if (Config.Current.UseHttpModule)
						{
							HttpWorkerRequest worker = UploadHttpModule.GetCurrentWorkerRequest();
							if (log.IsDebugEnabled) log.DebugFormat("worker={0}", worker);
				            if (worker == null)
				                return null;
							string qs = worker.GetQueryString();
				           	string postBackID = GetPostBackIDFromQueryString(qs);
							if (log.IsDebugEnabled) log.DebugFormat("postBackID={0}, qs={1}", postBackID, qs);							
				           	if (postBackID == null)
				           		return null;
				
							UploadState uploadState = UploadStateStore.OpenReadWriteOrCreate(postBackID);
							string transferEncodingHeader = worker.GetKnownRequestHeader(HttpWorkerRequest.HeaderTransferEncoding);
							if (transferEncodingHeader != "chunked")
								uploadState.Status = UploadStatus.NormalInProgress;
							else
								uploadState.Status = UploadStatus.ChunkedInProgress;
							CurrentUploadState = uploadState;
							return uploadState;
						}
					}
					return (UploadState)httpContext.Items["NeatUpload_UploadState"];
				}
				return null;
			}
			set {
				HttpContext httpContext = HttpContext.Current;
				if (httpContext != null)
				{
					HttpContext.Current.Items["NeatUpload_UploadState"] = value;
				}
			}
		}
        
		internal static string CurrentMultiRequestControlID {
			get {
				HttpContext httpContext = HttpContext.Current;
				if (httpContext != null)
				{
					if (httpContext.Items["NeatUpload_MultiRequestControlID"] == null)
					{
						if (Config.Current.UseHttpModule)
						{
							HttpWorkerRequest worker = UploadHttpModule.GetCurrentWorkerRequest();
				            if (worker == null)
				                return null;
							string qs = worker.GetQueryString();
				           	string controlID = GetMultiRequestControlIDFromQueryString(qs);
				           	if (controlID == null)
				           		return null;
							httpContext.Items["NeatUpload_MultiRequestControlID"] = controlID;
						}
					}
					return (string)httpContext.Items["NeatUpload_MultiRequestControlID"];
				}
				return null;
			}
		}
        
        internal static string GetPostBackIDFromQueryString(string qs)
        {
			// Parse out the postBackID.  Note, we can't just do:
			//   string postBackID = httpContext.Request.Params[Config.Current.PostBackIDQueryParam];
			// because that will prevent ASP.NET from getting a new Params array if Server.Transfer() is called.
			string postBackIDQueryParam = Config.Current.PostBackIDQueryParam;
            if (qs == null || postBackIDQueryParam == null)
                return null;
			Match match = Regex.Match(qs, @"(^|\?|&)" + Regex.Escape(postBackIDQueryParam) + @"=([^&]+)");
			if (!match.Success)
				return null;
			return HttpUtility.UrlDecode(match.Groups[2].Value);
		}

        internal static string GetMultiRequestControlIDFromQueryString(string qs)
        {
            if (qs == null)
                return null;
			Match match = Regex.Match(qs, @"(^|\?|&)" + MultiRequestUploadModule.ControlIDQueryParam + "=([^&]+)");
			if (!match.Success)
				return null;
			return HttpUtility.UrlDecode(match.Groups[2].Value);
		}

		internal static string GetArmoredCookiesStringFromQueryString(string qs)
		{
            if (qs == null)
                return null;
			Match match = Regex.Match(qs, @"(^|\?|&)" + MultiRequestUploadModule.ArmoredCookiesQueryParam + "=([^&]+)");
			if (!match.Success)
				return null;
			return HttpUtility.UrlDecode(match.Groups[2].Value);
		}
		
        internal static HttpCookieCollection GetCookiesFromQueryString(string qs)
        {
			string armoredCookiesString = GetArmoredCookiesStringFromQueryString(qs);
			if (log.IsDebugEnabled) log.DebugFormat("armoredCookiesString={0}", armoredCookiesString);
			HttpCookieCollection cookies = new HttpCookieCollection();
			if (armoredCookiesString != null && armoredCookiesString.Length > 0)
			{
                Hashtable armoredCookies = (Hashtable)ObjectProtector.Unprotect(armoredCookiesString);
				foreach (string k in armoredCookies.Keys)
				{
					if (log.IsDebugEnabled) log.DebugFormat("armoredCookies[{0}]={1}", k, armoredCookies[k]);
					cookies.Add(new HttpCookie(k, (string)armoredCookies[k]));
				}
			}
			return cookies;
		}

        internal static long[] GetFileSizesFromQueryString(string qs)
        {
            if (qs == null)
                return null;
			Match match = Regex.Match(qs, @"(^|\?|&)FileSizes=([^&]+)");
			if (!match.Success)
				return null;
			string[] fileSizeStrings = HttpUtility.UrlDecode(match.Groups[2].Value).Split(' ');
			long[] fileSizes = new long[fileSizeStrings.Length];
			for (int i = 0; i < fileSizes.Length; i++)
			{
				fileSizes[i] = Int64.Parse(fileSizeStrings[i]);
			}
			return fileSizes;
		}

		private void Application_AcquireRequestState(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_AcquireRequestState");
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
			HttpContext.Current.Items["NeatUpload_RequestStateAcquired"] = true;
		}

		private void Application_ReleaseRequestState(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_ReleaseRequestState");
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
			HttpContext.Current.Items.Remove("NeatUpload_RequestStateAcquired");
		}

		private void Application_PreRequestHandlerExecute(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_PreRequestHandlerExecute");
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
		}

		private EventHandler RememberErrorHandler; 
		private void RememberError(object sender, EventArgs e)
		{
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
			DecoratedWorkerRequest decoratedWorker = GetCurrentWorkerRequest() as DecoratedWorkerRequest;
			HttpApplication app = sender as HttpApplication;
			
			if (decoratedWorker != null)
			{
				Exception ex = app.Server.GetLastError();
				if (ex != null)
				{
					// We are here because an exception was thrown while the subrequest was being proceessed.
					// Ideally, we'd like it to appear as though the exception was thrown in the context of the
					// original request so that thigs which rely on the original context (e.g. custom error pages)
					// operate properly.  To achieve that, we'd like to end the subrequest without sending any
					// response, and remember the exception so that we can rethrow it in the original
					// request context.  However, if some headers or content have been already been sent to the
					// client, then that is impossible.  In that case we'll continue processing normally which
					// will mean that the exception is handled in the context of the subrequest.

					// Try to clear the headers.  This will throw an HttpException if headers have already been
					// sent to the client.
					try
					{
						app.Response.ClearHeaders();
					}
					catch (HttpException)
					{
						if (log.IsDebugEnabled)
							log.DebugFormat("The following error will be processed in NeatUpload's subrequest context because the response has already been at least partially sent {0}", ex);
						return;
					}
					// Clear any buffered content as well so that it isn't 
					// written when we end the subrequest.
					app.Response.ClearContent();

					decoratedWorker.Exception = ex;
					if (log.IsDebugEnabled) log.DebugFormat("Remembering error: {0}", decoratedWorker.Exception);

					// For the remainder of the subrequest, act as though there was no error.
					app.Server.ClearError();

					// Finish the subrequest.
					app.CompleteRequest();					
				}
			}
		}		

		private void Application_EndRequest(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_EndRequest");
			if (!Config.Current.UseHttpModule)
			{
				return;
			}

			HttpApplication app = sender as HttpApplication;
			if (RememberErrorHandler != null)
			{
				app.Error -= RememberErrorHandler;
			}

            HttpContext ctx = HttpContext.Current;
            // Get the list of files to dispose to the current context if one hasn't been added yet
            ArrayList filesToDispose = ctx.Items["NeatUpload_FilesToDispose"] as ArrayList;
            if (filesToDispose != null)
            {
                foreach (UploadedFile file in filesToDispose)
                    file.Dispose();
            }

            // Get CurrentUploadState only if it has already been set to
            // avoid deadlocks.
            UploadState uploadState = null;
            if (HttpContext.Current != null)
                uploadState = (UploadState)HttpContext.Current.Items["NeatUpload_UploadState"];
			if (uploadState != null)
			{
				if (CurrentMultiRequestControlID == null
			    	&& uploadState.Status != UploadStatus.Failed && uploadState.Status != UploadStatus.Rejected)
				{
					uploadState.Status = UploadStatus.Completed;
				}
				UploadStateStore.Close(CurrentUploadState);
				CurrentUploadState = null;
			}
		}
	}
}
