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

	/// <summary>
	/// Stores and retrieves <see cref="IUploadState"/> objects in the user's session.
	/// </summary>
	/// <remarks>If the application is configured to share session state across a web garden/farm,
	/// this provider will ensure that upload state will be shared in the same way.  This class
	/// is instantiated by NeatUpload if it is added in the &lt;providers&gt; section of the &lt;neatUpload&gt;
	/// section.  Application developers should not instantiate it directly.
	/// </remarks>
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

        public override CleanUpIfStaleCallback GetCleanUpIfStaleCallback()
        {
            Cleaner cleaner = new Cleaner(this);
            return cleaner.CleanUpIfStale;
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

        private class Cleaner
        {
            internal Cleaner(SessionBasedUploadStateStoreProvider provider)
            {
                UriBuilder handlerUriBuilder = new UriBuilder(HttpContext.Current.Request.Url);
                handlerUriBuilder.Path = HttpContext.Current.Response.ApplyAppPathModifier(provider.HandlerUrl);
                HandlerUri = handlerUriBuilder.Uri;
                Cookies = HttpContext.Current.Request.Cookies;
                EncryptionKey = Config.Current.EncryptionKey;
                ValidationKey = Config.Current.ValidationKey;
                EncryptionAlgorithm = Config.Current.EncryptionAlgorithm;
                ValidationAlgorithm = Config.Current.ValidationAlgorithm;
            }

            internal void CleanUpIfStale(string postBackID)
            {
                SimpleWebRemoting.MakeRemoteCall(HandlerUri, Cookies, EncryptionKey, ValidationKey, EncryptionAlgorithm, ValidationAlgorithm, "CleanUpIfStale", postBackID);
            }

            Uri HandlerUri;
            HttpCookieCollection Cookies;
            byte[] EncryptionKey;
            byte[] ValidationKey;
            string EncryptionAlgorithm;
            string ValidationAlgorithm;
        }

        internal string HandlerUrl = "~/NeatUpload/UploadStateStoreHandler.ashx";
	}
}
