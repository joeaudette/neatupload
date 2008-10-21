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
using System.IO;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
		
	public class SessionBasedUploadStateStoreProvider : SessionBasedUploadStateStoreProviderBase
	{
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection attrs)
        {
            base.Initialize(name, attrs);
            if (attrs != null)
            {
                foreach (string key in attrs.Keys)
                {
                    string val = attrs[key];
                    if (key == "handlerUrl")
                    {
                        HandlerUrl = val;
                    }
                    else
                    {
                        throw new System.Xml.XmlException("Unrecognized attribute: " + key);
                    }
                }
            }
        }

		public override UploadState Load(string postBackID)
		{
			if (IsSessionReadable)
				return base.Load(postBackID);
			return (UploadState)MakeRemoteCall("Load", postBackID);
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

        public override EventHandler GetCleanUpIfStaleHandler(string postBackID)
        {
            RemoteCall remoteCall = new RemoteCall(HandlerUrl, "CleanUpIfStale", postBackID, null);
            return new EventHandler(remoteCall.Invoke);
        }

        private bool IsSessionReadable
        {
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
            handlerUriBuilder.Path = HttpContext.Current.Response.ApplyAppPathModifier(HandlerUrl);
            return SimpleWebRemoting.MakeRemoteCall(handlerUriBuilder.Uri, methodCall);
        }

        private class RemoteCall
        {
            internal RemoteCall(string handlerUrl, params object[] methodCall)
            {
                UriBuilder handlerUriBuilder = new UriBuilder(HttpContext.Current.Request.Url);
                handlerUriBuilder.Path = HttpContext.Current.Response.ApplyAppPathModifier(handlerUrl);
                HandlerUri = handlerUriBuilder.Uri;
                Cookies = HttpContext.Current.Request.Cookies;
                EncryptionKey = Config.Current.EncryptionKey;
                ValidationKey = Config.Current.ValidationKey;
                MethodCall = methodCall;
            }

            internal void Invoke(object source, EventArgs args)
            {
                SimpleWebRemoting.MakeRemoteCall(HandlerUri, Cookies, EncryptionKey, ValidationKey, MethodCall);
            }

            Uri HandlerUri;
            HttpCookieCollection Cookies;
            byte[] EncryptionKey;
            byte[] ValidationKey;
            object[] MethodCall;
        }

        internal string HandlerUrl = "~/NeatUpload/UploadStateStoreHandler.ashx";
	}
}
