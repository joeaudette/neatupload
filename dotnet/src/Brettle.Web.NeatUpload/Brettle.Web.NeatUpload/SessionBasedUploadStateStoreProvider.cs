// SessionBasedUploadStateStoreProvider.cs - Part of NeatUpload
//
//  Copyright (C) 2008 Dean Brettle
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using System.Web;
using System.Web.SessionState;
using System.Net;
using System.IO;
using System.Collections.Specialized;

namespace Brettle.Web.NeatUpload
{
		
	public class SessionBasedUploadStateStoreProvider : SessionBasedUploadStateStoreProviderBase
	{
		public override UploadState Load(string postBackID)
		{
			if (IsSessionReadable)
				return base.Load(postBackID);
			return MakeRemoteCall("Load", postBackID) as UploadState;
		}

		public override void MergeAndSave(UploadState uploadState)
		{
			if (IsSessionWritable)
			{
				base.MergeAndSave(uploadState);
				return;
			}
			MakeRemoteCall("MergeAndSave", uploadState);
		}

		public override void DeleteIfStale (string postBackID)
		{
			if (IsSessionWritable)
			{
				base.DeleteIfStale(postBackID);
				return;
			}
			MakeRemoteCall("DeleteIfStale", postBackID);
		}

		private bool IsSessionReadable {
			get {
				HttpSessionState session = HttpContext.Current.Session;
				return (session != null && session.Mode != SessionStateMode.Off);
			}
		}

		private bool IsSessionWritable {
			get {
				return (IsSessionReadable && !HttpContext.Current.Session.IsReadOnly);
			}
		}

		private object MakeRemoteCall(params object[] methodCall)
		{
			UriBuilder handlerUriBuilder = new UriBuilder(HttpContext.Current.Request.Url);
			handlerUriBuilder.Path = HttpContext.Current.Request.ApplicationPath + "/NeatUpload/UploadStateStoreHandler.ashx";
			CookieContainer cookieContainer = new CookieContainer();
			HttpCookieCollection httpCookies = HttpContext.Current.Request.Cookies;
			if (httpCookies != null)
				foreach (HttpCookie httpCookie in httpCookies)
					cookieContainer.Add(new Cookie(httpCookie.Name, httpCookie.Value, "/", handlerUriBuilder.Host));
			WebClient wc = new WebClient();
			wc.Headers.Add("Cookie", cookieContainer.GetCookieHeader(handlerUriBuilder.Uri));
			string protectedRequestPayload = ObjectProtector.Protect(methodCall);
			NameValueCollection formValues = new NameValueCollection();
			formValues.Add("ProtectedPayload", protectedRequestPayload);
			byte[] responseBytes = wc.UploadValues(handlerUriBuilder.ToString(), formValues);
			string protectedResponsePayload = System.Text.Encoding.ASCII.GetString(responseBytes);
			object result = null;
			if (protectedResponsePayload != null && protectedResponsePayload.Length > 0)
				result = ObjectProtector.Unprotect(protectedResponsePayload);
			return result;
		}
	}
}
