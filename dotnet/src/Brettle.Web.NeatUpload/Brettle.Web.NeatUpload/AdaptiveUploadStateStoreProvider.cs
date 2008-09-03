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

namespace Brettle.Web.NeatUpload
{
		
	public class AdaptiveUploadStateStoreProvider : UploadStateStoreProvider
	{
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection attrs)
        {
            base.Initialize(name, attrs);
            InProcProvider = new InProcUploadStateStoreProvider();
            SessionBasedProvider = new SessionBasedUploadStateStoreProvider();
            SessionBasedProvider.Initialize(name, attrs);
        }

        public override string Description { get { return "Delegates to InProcUploadStateStoreProvider if SessionStateMode is Off or InProc, otherwise delegates to SessionBasedUploadStateStoreProvider."; } }

        public override UploadState Load(string postBackID)
		{
            return Provider.Load(postBackID);
		}

		public override string[] MergeSaveAndCleanUp(UploadState uploadState, string[] postBackIDsToCleanUpIfStale)
		{
            return Provider.MergeSaveAndCleanUp(uploadState, postBackIDsToCleanUpIfStale);
		}

        public override void Delete(string postBackID)
        {
            Provider.Delete(postBackID);
        }

        private object SyncRoot = new object();
        private InProcUploadStateStoreProvider InProcProvider;
        private SessionBasedUploadStateStoreProvider SessionBasedProvider;

        private UploadStateStoreProvider _Provider;
        private UploadStateStoreProvider Provider {
            get
            {
                if (_Provider != null)
                    return _Provider;

                lock (SyncRoot)
                {
                    if (_Provider == null)
                    {
                        UriBuilder handlerUriBuilder = new UriBuilder(HttpContext.Current.Request.Url);
                        handlerUriBuilder.Path = HttpContext.Current.Request.ApplicationPath + "/NeatUpload/UploadStateStoreHandler.ashx";
                        SessionStateMode mode = (SessionStateMode)SimpleWebRemoting.MakeRemoteCall(handlerUriBuilder.Uri, "GetSessionStateMode");
                        if (mode == SessionStateMode.Off || mode == SessionStateMode.InProc)
                            _Provider = InProcProvider;
                        else
                        {
                            _Provider = SessionBasedProvider;
                        }
                    }
                    return _Provider;
                }
            }
        }
	}
}
