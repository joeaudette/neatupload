// UploadStateStoreHandler.ashx.cs created with MonoDevelop
// User: brettle at 2:28 PM 8/29/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

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
			string protectedPayload = context.Request.Params["ProtectedPayload"];
			object[] methodCall = (object[])ObjectProtector.Unprotect(protectedPayload);
			string methodName = (string)methodCall[0];
			object retVal = null;
			if (methodName == "Load")
			{
				string postBackID = (string)methodCall[1];
                retVal = Load(postBackID);
			}
			else if (methodName == "MergeSaveAndCleanUp")
			{
				UploadState uploadState = (UploadState)methodCall[1];
                string[] postBackIDsToCleanUpIfStale = (string[])methodCall[2];
                retVal = MergeSaveAndCleanUp(uploadState, postBackIDsToCleanUpIfStale);
			}
            ArrayList resultsArray = new ArrayList();
            resultsArray.Add(retVal);
            for (int i = 1; i < methodCall.Length; i++)
            {
                if (methodCall[i] is ICopyFromObject)
                    resultsArray.Add(methodCall[i]);
            }
            
			string responseBody = ObjectProtector.Protect(resultsArray.ToArray());
			context.Response.ContentType = "application/octet-stream";
			context.Response.Write(responseBody);
			context.Response.Flush();
		}

		public bool IsReusable { get { return true; } }

	}
}
