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

namespace Brettle.Web.NeatUpload.Internal.Module
{
	internal abstract class DecoratedWorkerRequest : System.Web.HttpWorkerRequest
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		internal int StatusCode = 200;
		internal string StatusDescription = null;
		internal HttpWorkerRequest OrigWorker;
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
			if (log.IsDebugEnabled) log.Debug("Calling GetKnownRequestHeader()");
			return OrigWorker.GetKnownRequestHeader (index);
		}

		public override long GetBytesRead ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetBytesRead()");
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
			if (log.IsDebugEnabled) log.Debug("FlushResponse() called");
			if (Exception == null)
			{
				if (log.IsDebugEnabled) log.Debug("FlushResponse(" + finalFlush + ") called -> Calling FlushResponse(false)");
				// Always pass false so that ASP.NET doesn't recycle response buffers while they are still in use.
				OrigWorker.FlushResponse(false);
			}
		}

		public override string GetAppPath ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetAppPath");
			string result = OrigWorker.GetAppPath();
			if (log.IsDebugEnabled) log.Debug("GetAppPath () returns " + result);
			return result;
		}

		public override string GetAppPathTranslated ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetAppPathTranslated");
			string result = OrigWorker.GetAppPathTranslated ();
			if (log.IsDebugEnabled) log.Debug("GetAppPathTranslated () returns " + result);
			return result;
		}

		public override string GetAppPoolID ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetAppPoolID");
			string result = base.GetAppPoolID ();
			if (log.IsDebugEnabled) log.Debug("GetAppPoolID() returning " + result);			
			return result;
		}

		public override byte[] GetClientCertificate ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetClientCertificate");
			return OrigWorker.GetClientCertificate ();
		}

		public override byte[] GetClientCertificateBinaryIssuer ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetClientCertificateBinaryIssuer");
			return OrigWorker.GetClientCertificateBinaryIssuer ();
		}

		public override int GetClientCertificateEncoding ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetClientCertificateEncoding");
			return OrigWorker.GetClientCertificateEncoding ();
		}

		public override byte[] GetClientCertificatePublicKey ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetClientCertificatePublicKey");
			return OrigWorker.GetClientCertificatePublicKey ();
		}

		public override DateTime GetClientCertificateValidFrom ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetClientCertificateValidFrom");
			return OrigWorker.GetClientCertificateValidFrom ();
		}

		public override DateTime GetClientCertificateValidUntil ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetClientCertificateValidUntil");
			return OrigWorker.GetClientCertificateValidUntil ();
		}

		public override long GetConnectionID ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetConnectionID");
			long connID = base.GetConnectionID ();
			if (log.IsDebugEnabled) log.Debug("GetConnectionID() returning " + connID);
			return connID;
		}

		public override string GetFilePath ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetFilePath");
			string result = OrigWorker.GetFilePath ();
			if (log.IsDebugEnabled) log.Debug("GetFilePath() returning " + result);			
			return result;
		}

		public override string GetFilePathTranslated ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetFilePathTranslated");
			string result = OrigWorker.GetFilePathTranslated ();
			if (log.IsDebugEnabled) log.Debug("GetFilePathTranslated() returning " + result);			
			return result;
		}

		public override string GetHttpVerbName ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetHttpVerbName");
			string result = OrigWorker.GetHttpVerbName ();
			if (log.IsDebugEnabled) log.Debug("GetHttpVerbName() returning " + result);			
			return result;
		}

		public override string GetHttpVersion ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetHttpVersion");
			string result = OrigWorker.GetHttpVersion ();
			if (log.IsDebugEnabled) log.Debug("GetHttpVersion() returning " + result);			
			return result;
		}

		public override string GetLocalAddress ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetLocalAddress");
			string result = OrigWorker.GetLocalAddress ();
			if (log.IsDebugEnabled) log.Debug("GetLocalAddress() returning " + result);			
			return result;
		}

		public override int GetLocalPort ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetLocalPort");
			int result = OrigWorker.GetLocalPort ();
			if (log.IsDebugEnabled) log.Debug("GetLocalPort() returning " + result);			
			return result;
		}

		public override string GetPathInfo ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetPathInfo");
			string result = OrigWorker.GetPathInfo ();
			if (log.IsDebugEnabled) log.Debug("GetPathInfo() returning " + result);			
			return result;
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetPreloadedEntityBody");
			return OrigWorker.GetPreloadedEntityBody();
		}

		public override string GetProtocol ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetProtocol");
			string result = OrigWorker.GetProtocol ();
			if (log.IsDebugEnabled) log.Debug("GetProtocol() returning " + result);			
			return result;
		}

		public override string GetQueryString ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetQueryString");
			string result = OrigWorker.GetQueryString ();
			if (log.IsDebugEnabled) log.Debug("GetQueryString() returning " + result);			
			return result;
		}

		public override byte[] GetQueryStringRawBytes ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetQueryStringRawBytes");
			byte[] result = OrigWorker.GetQueryStringRawBytes ();
			if (result != null && log.IsDebugEnabled) log.Debug("GetQueryStringRawBytes() returning " + System.Text.Encoding.UTF8.GetString(result));
			return result;
		}

		public override string GetRawUrl ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetRawUrl");
			string result = OrigWorker.GetRawUrl ();
			if (log.IsDebugEnabled) log.Debug("GetRawUrl() returning " + result);			
			return result;			
		}

		public override string GetRemoteAddress ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetRemoteAddress");
			string result = OrigWorker.GetRemoteAddress ();
			if (log.IsDebugEnabled) log.Debug("GetRemoteAddress() returning " + result);			
			return result;			
		}


		public override string GetRemoteName ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetRemoteName");
			string result = OrigWorker.GetRemoteName ();
			if (log.IsDebugEnabled) log.Debug("GetRemoteName() returning " + result);			
			return result;			
		}

		public override int GetRemotePort ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetRemotePort");
			int result = OrigWorker.GetRemotePort ();
			if (log.IsDebugEnabled) log.Debug("GetRemotePort() returning " + result);			
			return result;			
		}

		public override int GetRequestReason ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetRequestReason");
			int result = OrigWorker.GetRequestReason ();
			if (log.IsDebugEnabled) log.Debug("GetRequestReason() returning " + result);			
			return result;			
		}

		public override string GetServerName ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetServerName");
			string result = OrigWorker.GetServerName ();  
			if (log.IsDebugEnabled) log.Debug("GetServerName() returning " + result);			
			return result;			
		}

		public override string GetServerVariable (string name)
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetServerVariable");
			string result = OrigWorker.GetServerVariable (name);
			if (log.IsDebugEnabled) log.Debug("GetServerVariable(" + name + ") returning " + result);			
			return result;
		}

		public override string GetUnknownRequestHeader (string name)
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetUnknownRequestHeader");
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
			if (log.IsDebugEnabled) log.Debug("Calling GetUriPath");
			string result = OrigWorker.GetUriPath ();
			if (log.IsDebugEnabled) log.Debug("GetUriPath() returning " + result);			
			return result;
		}

		public override long GetUrlContextID ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling GetUrlContextID");
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
			if (log.IsDebugEnabled) log.Debug("Calling HeadersSent");
			return OrigWorker.HeadersSent ();
		}

		public override bool IsClientConnected ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling IsClientConnected");
			return OrigWorker.IsClientConnected ();
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			if (log.IsDebugEnabled) log.Debug("Calling IsEntireEntityBodyPreloaded");
			return OrigWorker.IsEntireEntityBodyIsPreloaded();
		}

		public override bool IsSecure () 
		{
			if (log.IsDebugEnabled) log.Debug("Calling IsSecure");
			return OrigWorker.IsSecure () ;
		}

		public override string MapPath (string virtualPath)
		{
			if (log.IsDebugEnabled) log.Debug("Calling MapPath");
			string result = OrigWorker.MapPath (virtualPath);
			if (log.IsDebugEnabled) log.Debug("MapPath(" + virtualPath + ") returning " + result);			
			return result;			
		}

		public override void SendCalculatedContentLength (int contentLength)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendCalculatedContentLength");
			if (Exception == null)
			{
				OrigWorker.SendCalculatedContentLength (contentLength);
			}
		}

		public override void SendKnownResponseHeader (int index, string value)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendKnownResponseHeader");
			if (Exception == null)
			{
				OrigWorker.SendKnownResponseHeader (index, value);
			}
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendResponseFromFile");
			if (Exception == null)
			{
				long bufSize = 1024*1024;
				while (length > bufSize  && OrigWorker.IsClientConnected())
				{
					OrigWorker.SendResponseFromFile (handle, offset, bufSize);
					OrigWorker.FlushResponse(false);
					offset += bufSize;
					length -= bufSize;
				}
				OrigWorker.SendResponseFromFile (handle, offset, length);
			}
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendResponseFromFile");
			if (Exception == null)
			{
				long bufSize = 1024*1024;
				while (length > bufSize && OrigWorker.IsClientConnected())
				{
					OrigWorker.SendResponseFromFile (filename, offset, bufSize);
					OrigWorker.FlushResponse(false);
					offset += bufSize;
					length -= bufSize;
				}
				OrigWorker.SendResponseFromFile (filename, offset, length);
			}
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendResponseFromMemory");
			if (Exception == null)
			{
				OrigWorker.SendResponseFromMemory (data, length);
			}
		}

		public override void SendResponseFromMemory (IntPtr data, int length)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendResponseFromMemory");
			if (Exception == null)
			{
				OrigWorker.SendResponseFromMemory (data, length);
			}
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendStatus");
			StatusCode = statusCode;
			StatusDescription = statusDescription;
			if (Exception == null)
			{
				OrigWorker.SendStatus (statusCode, statusDescription);
			}
		}
		
		public override void SendUnknownResponseHeader (string name, string value)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SendUnknownResponseHeader");
			if (Exception == null)
			{
				OrigWorker.SendUnknownResponseHeader (name, value);
			}
		}

		public override void SetEndOfSendNotification (EndOfSendNotification callback, object extraData)
		{
			if (log.IsDebugEnabled) log.Debug("Calling SetEndOfSendNotification");
//			OrigWorker.SetEndOfSendNotification (callback, extraData);
		}

		public override string MachineConfigPath
		{
			get {
				if (log.IsDebugEnabled) log.Debug("get_MachineConfigPath called");
				return OrigWorker.MachineConfigPath;
			}
		}

		public override string MachineInstallDirectory
		{
			get {
				if (log.IsDebugEnabled) log.Debug("get_MachineInstallDirectory called");
				return OrigWorker.MachineInstallDirectory;
			}
		}
	}
}

