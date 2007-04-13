// 
// Brettle.Web.NeatUpload.DecoratedWorkerRequest
//   based on System.Web.HttpWorkerRequest
//
// Authors:
// 	Patrik Torstensson (Patrik.Torstensson@labs2.com)
//   	(constants from Bob Smith (bob@thestuff.net))
//   	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//      modifications to become DecoratedWorkerRequest by Dean Brettle
//
// (c) Patrik Torstensson
// (c) 2002 Ximian, Inc. (http://www.ximian.com)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
using System.IO;
using System.Threading;

namespace Brettle.Web.NeatUpload
{
	internal abstract class DecoratedWorkerRequest : System.Web.HttpWorkerRequest
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		internal int StatusCode = 200;
		internal string StatusDescription = null;
		protected HttpWorkerRequest OrigWorker;
		private object sync = new object();
		private bool isEndOfRequest = false;
		internal HttpContext OrigContext;
		internal Exception Exception;
				
		protected DecoratedWorkerRequest (HttpWorkerRequest origWorker) 
		{
			if (log.IsDebugEnabled) log.Debug("origWorker=" + origWorker);
			OrigWorker = origWorker;
			// Remember the original HttpContext so that it can be used by UploadHttpModule.AppendToLog().
			OrigContext = HttpContext.Current;
		}
		
		internal void WaitForEndOfRequest()
		{
			lock (sync)
			{
				while (!isEndOfRequest)
				{
					Monitor.Wait(sync);
				}
			}
			return;
		}
		
		internal void ProcessRequest(object state)
		{
			if (log.IsDebugEnabled) log.Debug("Calling HttpRuntime.ProcessRequest()");
			HttpRuntime.ProcessRequest(this);
			if (log.IsDebugEnabled) log.Debug("HttpRuntime.ProcessRequest() returned");
		}
		
		protected void IgnoreRemainingBodyAndThrow(Exception ex)
		{
			byte[] buffer = new byte[4096];
			while (0 < OrigWorker.ReadEntityBody(buffer, buffer.Length))
				; // Ignore the remaining body
			throw ex;
		}
		
		public override int ReadEntityBody (byte[] buffer, int size)
		{
			return OrigWorker.ReadEntityBody(buffer, size);
		}

		public override string GetKnownRequestHeader (int index)
		{
			return OrigWorker.GetKnownRequestHeader (index);
		}

		public override long GetBytesRead ()
		{
			return OrigWorker.GetBytesRead();
		}

		public override void CloseConnection ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling CloseConnection()");
			OrigWorker.CloseConnection();
		}
		
		public override void EndOfRequest ()
		{
			if (log.IsDebugEnabled) log.Debug("In EndOfRequest(), calling Monitor.PulseAll(Sync)");
			lock (sync)
			{
				isEndOfRequest = true;
				Monitor.PulseAll(sync);
			}
		}
		
		public override void FlushResponse (bool finalFlush)
		{
			if (Exception == null)
			{
				if (log.IsDebugEnabled) log.Debug("FlushResponse(" + finalFlush + ") called -> Calling FlushResponse(false)");
				// Always pass false so that ASP.NET doesn't recycle response buffers while they are still in use.
				OrigWorker.FlushResponse(false);
			}
		}

		public override string GetAppPath ()
		{
			return OrigWorker.GetAppPath();
		}

		public override string GetAppPathTranslated ()
		{
			return OrigWorker.GetAppPathTranslated ();
		}

		public override string GetAppPoolID ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling base.GetAppPoolID()");
			string result = base.GetAppPoolID ();
			if (log.IsDebugEnabled) log.Debug("GetAppPoolID() returning " + result);			
			return result;
		}

		public override byte[] GetClientCertificate ()
		{
			return OrigWorker.GetClientCertificate ();
		}

		public override byte[] GetClientCertificateBinaryIssuer ()
		{
			return OrigWorker.GetClientCertificateBinaryIssuer ();
		}

		public override int GetClientCertificateEncoding ()
		{
			return OrigWorker.GetClientCertificateEncoding ();
		}

		public override byte[] GetClientCertificatePublicKey ()
		{
			return OrigWorker.GetClientCertificatePublicKey ();
		}

		public override DateTime GetClientCertificateValidFrom ()
		{
			return OrigWorker.GetClientCertificateValidFrom ();
		}

		public override DateTime GetClientCertificateValidUntil ()
		{
			return OrigWorker.GetClientCertificateValidUntil ();
		}

		public override long GetConnectionID ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling base.GetConnectionID()");
			long connID = base.GetConnectionID ();
			if (log.IsDebugEnabled) log.Debug("GetConnectionID() returning " + connID);
			return connID;
		}

		public override string GetFilePath ()
		{
			return OrigWorker.GetFilePath ();
		}

		public override string GetFilePathTranslated ()
		{
			return OrigWorker.GetFilePathTranslated ();
		}

		public override string GetHttpVerbName ()
		{
			return OrigWorker.GetHttpVerbName ();
		}

		public override string GetHttpVersion ()
		{
			return OrigWorker.GetHttpVersion ();
		}

		public override string GetLocalAddress ()
		{
			return OrigWorker.GetLocalAddress ();
		}

		public override int GetLocalPort ()
		{
			return OrigWorker.GetLocalPort ();
		}

		public override string GetPathInfo ()
		{
			return OrigWorker.GetPathInfo ();
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			return OrigWorker.GetPreloadedEntityBody();
		}

		public override string GetProtocol ()
		{
			return OrigWorker.GetProtocol ();
		}

		public override string GetQueryString ()
		{
			return OrigWorker.GetQueryString ();
		}

		public override byte[] GetQueryStringRawBytes ()
		{
			return OrigWorker.GetQueryStringRawBytes ();
		}

		public override string GetRawUrl ()
		{
			return OrigWorker.GetRawUrl ();
		}

		public override string GetRemoteAddress ()
		{
			return OrigWorker.GetRemoteAddress ();
		}


		public override string GetRemoteName ()
		{
			return OrigWorker.GetRemoteName ();
		}

		public override int GetRemotePort ()
		{
			return OrigWorker.GetRemotePort ();
		}

		public override int GetRequestReason ()
		{
			return OrigWorker.GetRequestReason ();
		}

		public override string GetServerName ()
		{
			return OrigWorker.GetServerName ();  
		}

		public override string GetServerVariable (string name)
		{
			return OrigWorker.GetServerVariable (name);
		}

		public override string GetUnknownRequestHeader (string name)
		{
			return OrigWorker.GetUnknownRequestHeader (name);
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			return OrigWorker.GetUnknownRequestHeaders ();
		}

		public override string GetUriPath ()
		{
			return OrigWorker.GetUriPath ();
		}

		public override long GetUrlContextID ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling base.GetUrlContextID()");
			long result = base.GetUrlContextID ();
			if (log.IsDebugEnabled) log.Debug("GetUrlContextID() returning " + result);			
			return result;
		}

		public override IntPtr GetUserToken ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling OrigWorker.GetUserToken()");
			return OrigWorker.GetUserToken ();
		}

		public override IntPtr GetVirtualPathToken ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling base.GetVirtualPathToken()");
			return base.GetVirtualPathToken ();
		}

		public override bool HeadersSent ()
		{
			return OrigWorker.HeadersSent ();
		}

		public override bool IsClientConnected ()
		{
			return OrigWorker.IsClientConnected ();
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return OrigWorker.IsEntireEntityBodyIsPreloaded();
		}

		public override bool IsSecure () 
		{
			return OrigWorker.IsSecure () ;
		}

		public override string MapPath (string virtualPath)
		{
			return OrigWorker.MapPath (virtualPath);
		}

		public override void SendCalculatedContentLength (int contentLength)
		{
			if (Exception == null)
			{
				OrigWorker.SendCalculatedContentLength (contentLength);
			}
		}

		public override void SendKnownResponseHeader (int index, string value)
		{
			if (Exception == null)
			{
				OrigWorker.SendKnownResponseHeader (index, value);
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			if (Exception == null)
			{
				OrigWorker.SendResponseFromFile (handle, offset, length);
			}
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			if (Exception == null)
			{
				OrigWorker.SendResponseFromFile (filename, offset, length);
			}
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			if (Exception == null)
			{
				OrigWorker.SendResponseFromMemory (data, length);
			}
		}

		public override void SendResponseFromMemory (IntPtr data, int length)
		{
			if (Exception == null)
			{
				OrigWorker.SendResponseFromMemory (data, length);
			}
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			StatusCode = statusCode;
			StatusDescription = statusDescription;
			if (Exception == null)
			{
				OrigWorker.SendStatus (statusCode, statusDescription);
			}
		}
		
		public override void SendUnknownResponseHeader (string name, string value)
		{
			if (Exception == null)
			{
				OrigWorker.SendUnknownResponseHeader (name, value);
			}
		}

		public override void SetEndOfSendNotification (EndOfSendNotification callback, object extraData)
		{
//			OrigWorker.SetEndOfSendNotification (callback, extraData);
		}

		public override string MachineConfigPath
		{
			get {
				return OrigWorker.MachineConfigPath;
			}
		}

		public override string MachineInstallDirectory
		{
			get {
				return OrigWorker.MachineInstallDirectory;
			}
		}
	}
}

