/*
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2008  Dean Brettle

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
using System.Web;
using System.Web.UI;
using System.Web.SessionState;
using System.Collections;

namespace Brettle.Web.NeatUpload
{	
	public class UploadStateStoreHandler : SessionBasedUploadStateStoreProviderBase, System.Web.IHttpHandler, IRequiresSessionState
	{
		public virtual void ProcessRequest(HttpContext context)
		{
            SimpleWebRemoting.ProcessRemoteCallRequest(context, HandleMethodCall);
        }

        private object HandleMethodCall(string methodName, object[] args)
        {
			object retVal = null;
			if (methodName == "Load")
			{
				string postBackID = (string)args[0];
                retVal = Load(postBackID);
			}
			else if (methodName == "MergeSaveAndCleanUp")
			{
				UploadState uploadState = (UploadState)args[0];
                string[] postBackIDsToCleanUpIfStale = (string[])args[1];
                retVal = MergeSaveAndCleanUp(uploadState, postBackIDsToCleanUpIfStale);
			}
            else if (methodName == "Delete")
            {
                string postBackID = (string)args[0];
                Delete(postBackID);
            }
            else if (methodName == "GetSessionStateMode")
            {
                HttpSessionState sessionState = HttpContext.Current.Session;
                if (sessionState == null)
                    retVal = SessionStateMode.Off;
                else
                    retVal = sessionState.Mode;
            }
            return retVal;
		}

        public bool IsReusable { get { return true; } }

	}
}
