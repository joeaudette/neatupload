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
using log4net;

namespace Brettle.Web.NeatUpload
{
	public class UploadHttpModule : IHttpModule
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		internal static int MaxNormalRequestLength
		{
			get
			{
				string maxNormalRequestLengthSetting 
					= ConfigurationSettings.AppSettings["NeatUpload.MaxNormalRequestLength"];
				if (maxNormalRequestLengthSetting == null)
				{
					maxNormalRequestLengthSetting = "4096"; // 4Mbytes
				}
				return Int32.Parse(maxNormalRequestLengthSetting) * 1024;
			}
		}

		private static bool _isEnabled = false;		
		internal static bool IsEnabled
		{
			get { lock (typeof(UploadHttpModule)) { return _isEnabled;} }
		}
		
		public void Init(HttpApplication app)
		{
			app.BeginRequest += new System.EventHandler(Application_BeginRequest);
			app.Error += new System.EventHandler(Application_Error);
			app.EndRequest += new System.EventHandler(Application_EndRequest);
			app.PreSendRequestHeaders += new System.EventHandler(Application_PreSendRequestHeaders);
			lock (typeof(UploadHttpModule))
			{
				_isEnabled = true;
			}
		}
		
		public void Dispose()
		{
		}
		
		internal static HttpWorkerRequest GetCurrentWorkerRequest()
		{
			HttpContext origContext = HttpContext.Current;
			IServiceProvider provider = (IServiceProvider)origContext;
			HttpWorkerRequest origWorker = (HttpWorkerRequest) provider.GetService(typeof(HttpWorkerRequest));
			return origWorker;
		}

		bool requestHandledBySubRequest;

		private void Application_BeginRequest(object sender, EventArgs e)
		{
			requestHandledBySubRequest = false;
			HttpApplication app = sender as HttpApplication;
			log4net.ThreadContext.Properties["url"] = app.Context.Request.RawUrl;
			
			HttpWorkerRequest origWorker = GetCurrentWorkerRequest();
			
			// Ignore the subrequests we create to avoid infinite recursion...
			if (origWorker is DecoratedWorkerRequest)
				return;

			// Get the Content-Length header and parse it if we find it.  If it's not present we might
			// still be OK.
			int contentLength = 0;
			string contentLengthHeader = origWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentLength);
			if (contentLengthHeader != null)
			{
				try
				{
					contentLength = Int32.Parse(contentLengthHeader);
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
			if (contentTypeHeader != null && contentTypeHeader.StartsWith("multipart/form-data"))
			{
				if (contentLengthHeader == null)
					throw new HttpException(411, "Length Required");
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
					log4net.ThreadContext.Properties["url"] = app.Context.Request.RawUrl;
					
					// Workaround for bug in mod_mono (at least rev 1.0.9) where the response status
					// is overwritten with 200 when app.CompleteRequest() is called.  Status (and headers)
					// *should* be ignored because they were already sent when the subrequest was processed...
					app.Response.StatusCode = subWorker.StatusCode;
					app.Response.StatusDescription = subWorker.StatusDescription;

					// Always call CompleteRequest() to prevent further processing of the original request.
					app.CompleteRequest();
				}
			}
		}

		private void Application_PreSendRequestHeaders(object sender, EventArgs e)
		{
			if (requestHandledBySubRequest)
			{
				HttpApplication app = sender as HttpApplication;
				app.Response.ClearHeaders();
				app.Response.ClearContent();
			}
		}

		private void Application_Error(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_Error");
			UploadContext uploadContext = UploadContext.Current;
			if (uploadContext != null)
			{
				uploadContext.RemoveUploadedFiles();
			}
		}

		private void Application_EndRequest(object sender, EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In Application_EndRequest");
			UploadContext uploadContext = UploadContext.Current;
			if (uploadContext != null)
			{
				uploadContext.RemoveUploadedFiles();
			}
		}

	}
}
