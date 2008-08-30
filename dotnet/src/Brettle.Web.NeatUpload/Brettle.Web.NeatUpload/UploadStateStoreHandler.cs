// UploadStateStoreHandler.ashx.cs created with MonoDevelop
// User: brettle at 2:28 PM 8/29/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Web;
using System.Web.UI;
using System.Web.SessionState;

namespace Brettle.Web.NeatUpload
{	
	public class UploadStateStoreHandler : SessionBasedUploadStateStoreProviderBase, System.Web.IHttpHandler, IRequiresSessionState
	{
		public virtual void ProcessRequest(HttpContext context)
		{
			string protectedPayload = context.Request.Params["ProtectedPayload"];
			object[] methodCall = (object[])ObjectProtector.Unprotect(protectedPayload);
			string methodName = (string)methodCall[0];
			object result = null;
			if (methodName == "Load")
			{
				string postBackID = (string)methodCall[1];
				result = Load(postBackID);
			}
			else if (methodName == "MergeAndSave")
			{
				UploadState uploadState = (UploadState)methodCall[1];
				MergeAndSave(uploadState);
			}
			else if (methodName == "DeleteIfStale")
			{
				string postBackID = (string)methodCall[1];
				DeleteIfStale(postBackID);
			}
			string responseBody = ObjectProtector.Protect(result);
			context.Response.ContentType = "application/octet-stream";
			context.Response.Write(responseBody);
			context.Response.Flush();
		}

		public bool IsReusable { get { return true; } }

	}
}
