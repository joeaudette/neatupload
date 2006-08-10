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
	internal class SessionPreservingWorkerRequest : DecoratedWorkerRequest
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private string Page;
		private string Query;

		internal static SessionPreservingWorkerRequest Create(HttpWorkerRequest worker, string page, string query)
		{
			string sessionIDHeader = worker.GetUnknownRequestHeader("AspFilterSessionId");
			string appPath = worker.GetAppPath();
			if (!appPath.EndsWith("/"))
			{
				appPath += "/";
			}
			if (sessionIDHeader == null)
			{
				string uriPath = worker.GetUriPath();
				if (uriPath.StartsWith(appPath))
				{
					string[] compsOfPathWithinApp = uriPath.Substring(appPath.Length).Split('/');
					if (compsOfPathWithinApp.Length > 0 && compsOfPathWithinApp[0].StartsWith("(")
						&& compsOfPathWithinApp[0].EndsWith(")"))
					{
						page = compsOfPathWithinApp[0] + "/" + page;
					}
				}
			}
			page = appPath + page;
			if (log.IsDebugEnabled) log.Debug("page = " + page);
			return new SessionPreservingWorkerRequest(worker, page,	query);
		}

		protected SessionPreservingWorkerRequest (HttpWorkerRequest worker, 
													string page, string query)
													: base(worker)
		{
			Page = page;
			Query = query;
		}
		
		public override int ReadEntityBody (byte[] buffer, int size)
		{
			return 0;
		}

		public override string GetKnownRequestHeader (int index)
		{
			string header;
			if (index == HttpWorkerRequest.HeaderContentLength)
			{
				header = null;
			}
			else if (index == HttpWorkerRequest.HeaderContentType)
			{
				header = null;
			}
			else
			{
				header = base.GetKnownRequestHeader (index);
			}
			if (log.IsDebugEnabled) log.Debug("GetKnownRequestHeader(" + index + "=" + HttpWorkerRequest.GetKnownRequestHeaderName(index) + ") returning " + header);
			return header;			
		}

		public override void FlushResponse (bool finalFlush)
		{
			return;
		}

		public override string GetFilePath ()
		{
			return Page;
		}

		public override string GetFilePathTranslated ()
		{
			string filePathTranslated = MapPath(Page);
			if (log.IsDebugEnabled) log.Debug("GetFilePathTranslated() returns " + filePathTranslated);
			return filePathTranslated;
		}

		public override string GetHttpVerbName ()
		{
			return "GET";
		}

		public override string GetPathInfo ()
		{
			return "";
		}

		public override byte [] GetPreloadedEntityBody ()
		{
			return new byte[0];
		}

		public override string GetProtocol ()
		{
			return "http";
		}

		public override string GetQueryString ()
		{
			return Query;
		}

		public override byte[] GetQueryStringRawBytes ()
		{
			if (Query == null)
			{
				return null;
			}
			return System.Text.UTF8Encoding.UTF8.GetBytes(Query);
		}

		public override string GetRawUrl ()
		{
			if (Query == null)
			{
				return Page;
			}
				
			return Page + "?" + Query;
		}

		public override string GetUriPath ()
		{
			return Page;
		}

		public override bool IsClientConnected ()
		{
			return true;
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return true;
		}

		public override void SendCalculatedContentLength (int contentLength)
		{
			return;
		}
		public override void SendKnownResponseHeader (int index, string val)
		{
			if (log.IsDebugEnabled) log.Debug("SendKnownResponseHeader(" + HttpWorkerRequest.GetKnownResponseHeaderName(index) + ", " + val);
			return;
		}

		public override void SendResponseFromFile (IntPtr handle, long offset, long length)
		{
			return;
		}

		public override void SendResponseFromFile (string filename, long offset, long length)
		{
			return;
		}

		public override void SendResponseFromMemory (byte [] data, int length)
		{
			return;
		}
			
		public override void SendResponseFromMemory (IntPtr data, int length)
		{
			return;
		}

		public override void SendStatus (int statusCode, string statusDescription)
		{
			if (log.IsDebugEnabled) log.Debug("SendStatus(" + statusCode + ", " + statusDescription + ")");
			return;
		}
		
		public override void SendUnknownResponseHeader (string name, string val)
		{
			if (log.IsDebugEnabled) log.Debug("SendUnknownResponseHeader(" + name + ", " + val);
			return;
		}

		public override void SetEndOfSendNotification (EndOfSendNotification callback, object extraData)
		{
			return;
		}

/*
		public override string GetServerVariable (string name)
		{
			string val;
			if (name == "HTTP_ASPFILTERSESSIONID")
			{
				val = SessionIDHeader;
			}
			else 
			{
				val = base.GetServerVariable (name);
			}
			if (log.IsDebugEnabled) log.Debug("GetServerVariable(" + name + ") returning " + val);
			return val;
		}


		public override string GetUnknownRequestHeader (string name)
		{
			string header;
			if (name == "AspFilterSessionId")
			{
				header = SessionIDHeader;
			}
			else
			{
				header = base.GetUnknownRequestHeader (name);
			}
				
			if (log.IsDebugEnabled) log.Debug("GetUnknownRequestHeader(" + name + ") returning " + header);
			return header;
		}

		public override string [][] GetUnknownRequestHeaders ()
		{
			return base.GetUnknownRequestHeaders();
			if (SessionIDHeader == null)
				return base.GetUnknownRequestHeaders();
			string[][] baseHeaders = base.GetUnknownRequestHeaders ();
			if (baseHeaders == null)
				baseHeaders = new string[0][];
			string[][] allHeaders = new string[baseHeaders.Length + 1][];
			Array.Copy(baseHeaders, allHeaders, baseHeaders.Length);
			allHeaders[baseHeaders.Length] = new string[2] { "AspFilterSessionId", SessionIDHeader };
			string[][] result = allHeaders;
			for (int i = 0; i < result.Length; i++)
			{
				for (int j = 0; j < result[i].Length; j++)
				{
					if (log.IsDebugEnabled) log.Debug("  SessionPreserving.UnknownRequestHeader[" + i + "][" + j + "] = " + result[i][j]);
				}
			}
			return result;
		}
*/
	}
}

