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
			if (log.IsDebugEnabled) log.Debug("Ignoring call to CloseConnection()");
			// OrigWorker.CloseConnection();
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
				if (log.IsDebugEnabled) log.Debug("Calling FlushResponse(" + finalFlush + ")");
				OrigWorker.FlushResponse(false);
			}
		}

		public override string GetAppPath ()
		{
			string result = OrigWorker.GetAppPath();
			if (log.IsDebugEnabled) log.Debug("GetAppPath () returns " + result);
			return result;
		}

		public override string GetAppPathTranslated ()
		{
			string result = OrigWorker.GetAppPathTranslated ();
			if (log.IsDebugEnabled) log.Debug("GetAppPathTranslated () returns " + result);
			return result;
		}

		public override string GetAppPoolID ()
		{
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
			long connID = base.GetConnectionID ();
			if (log.IsDebugEnabled) log.Debug("GetConnectionID() returning " + connID);
			return connID;
		}

		public override string GetFilePath ()
		{
			string result = OrigWorker.GetFilePath ();
			if (log.IsDebugEnabled) log.Debug("GetFilePath() returning " + result);			
			return result;
		}

		public override string GetFilePathTranslated ()
		{
			string result = OrigWorker.GetFilePathTranslated ();
			if (log.IsDebugEnabled) log.Debug("GetFilePathTranslated() returning " + result);			
			return result;
		}

		public override string GetHttpVerbName ()
		{
			string result = OrigWorker.GetHttpVerbName ();
			if (log.IsDebugEnabled) log.Debug("GetHttpVerbName() returning " + result);			
			return result;
		}

		public override string GetHttpVersion ()
		{
			string result = OrigWorker.GetHttpVersion ();
			if (log.IsDebugEnabled) log.Debug("GetHttpVersion() returning " + result);			
			return result;
		}

		public override string GetLocalAddress ()
		{
			string result = OrigWorker.GetLocalAddress ();
			if (log.IsDebugEnabled) log.Debug("GetLocalAddress() returning " + result);			
			return result;
		}

		public override int GetLocalPort ()
		{
			int result = OrigWorker.GetLocalPort ();
			if (log.IsDebugEnabled) log.Debug("GetLocalPort() returning " + result);			
			return result;
		}

		public override string GetPathInfo ()
		{
			string result = OrigWorker.GetPathInfo ();
			if (log.IsDebugEnabled) log.Debug("GetPathInfo() returning " + result);			
			return result;
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			return OrigWorker.GetPreloadedEntityBody();
		}

		public override string GetProtocol ()
		{
			string result = OrigWorker.GetProtocol ();
			if (log.IsDebugEnabled) log.Debug("GetProtocol() returning " + result);			
			return result;
		}

		public override string GetQueryString ()
		{
			string result = OrigWorker.GetQueryString ();
			if (log.IsDebugEnabled) log.Debug("GetQueryString() returning " + result);			
			return result;
		}

		public override byte[] GetQueryStringRawBytes ()
		{
			byte[] result = OrigWorker.GetQueryStringRawBytes ();
			if (result != null && log.IsDebugEnabled) log.Debug("GetQueryStringRawBytes() returning " + System.Text.Encoding.UTF8.GetString(result));
			return result;
		}

		public override string GetRawUrl ()
		{
			string result = OrigWorker.GetRawUrl ();
			if (log.IsDebugEnabled) log.Debug("GetRawUrl() returning " + result);			
			return result;			
		}

		public override string GetRemoteAddress ()
		{
			string result = OrigWorker.GetRemoteAddress ();
			if (log.IsDebugEnabled) log.Debug("GetRemoteAddress() returning " + result);			
			return result;			
		}


		public override string GetRemoteName ()
		{
			string result = OrigWorker.GetRemoteName ();
			if (log.IsDebugEnabled) log.Debug("GetRemoteName() returning " + result);			
			return result;			
		}

		public override int GetRemotePort ()
		{
			int result = OrigWorker.GetRemotePort ();
			if (log.IsDebugEnabled) log.Debug("GetRemotePort() returning " + result);			
			return result;			
		}

		public override int GetRequestReason ()
		{
			int result = OrigWorker.GetRequestReason ();
			if (log.IsDebugEnabled) log.Debug("GetRequestReason() returning " + result);			
			return result;			
		}

		public override string GetServerName ()
		{
			string result = OrigWorker.GetServerName ();  
			if (log.IsDebugEnabled) log.Debug("GetServerName() returning " + result);			
			return result;			
		}

		public override string GetServerVariable (string name)
		{
			string result = OrigWorker.GetServerVariable (name);
			if (log.IsDebugEnabled) log.Debug("GetServerVariable(" + name + ") returning " + result);			
			return result;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			string result = OrigWorker.GetUnknownRequestHeader (name);
			if (log.IsDebugEnabled) log.Debug("GetUnknownRequestHeader(" + name + ") returning " + result);			
			return result;
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			if (log.IsDebugEnabled) log.Debug("GetUnknownRequestHeaders() called ");			
			string[][] result = OrigWorker.GetUnknownRequestHeaders ();
			if (result != null && log.IsDebugEnabled)
			{
				for (int i = 0; i < result.Length; i++)
				{
					for (int j = 0; j < result[i].Length; j++)
					{
						log.Debug("  UnknownRequestHeader[" + i + "][" + j + "] = " + result[i][j]);
					}
				}
			}
			return result;
		}

		public override string GetUriPath ()
		{
			string result = OrigWorker.GetUriPath ();
			if (log.IsDebugEnabled) log.Debug("GetUriPath() returning " + result);			
			return result;
		}

		public override long GetUrlContextID ()
		{
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
			string result = OrigWorker.MapPath (virtualPath);
			if (log.IsDebugEnabled) log.Debug("MapPath(" + virtualPath + ") returning " + result);			
			return result;			
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

