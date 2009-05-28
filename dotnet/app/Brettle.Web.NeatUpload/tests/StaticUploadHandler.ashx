<%@ WebHandler Language="C#" Class="StaticUploadHandler" %>

using System;
using System.Web;
using Brettle.Web.NeatUpload;

public class StaticUploadHandler : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
		// Force NeatUpload to parse the request.
		// TODO: Make accessing UploadModule.Files force the request to parsed.
		string submitValue = context.Request.Form["submit"];

		int numFilesReceived = 0;
		for (int i = 0; i < UploadModule.Files.Count; i++)
		{
			UploadedFile file = UploadModule.Files[i];
			if (file.IsUploaded)
			{
				// Process the files as desired, calling file.MoveTo() if you want to keep it.
				// This example just counts the files received.
				numFilesReceived++;
			}
		}

		// Uncomment the next section for a simple example showing updating of processing state
		/*
		ProgressInfo progressInfo = new ProgressInfo(numFilesReceived, "Files");
		for (int i = 0; i < numFilesReceived; i++)
		{
			progressInfo.Value++;
			UploadModule.SetProcessingState("progressID", progressInfo);
			System.Threading.Thread.Sleep(1000);
		}
		*/
		
		// Write a response or redirect as desired.
		// This example redirects back to the upload page, passing the number of files received
		// and the postBackID as query params.  The postBackID allows the page to retrieve and
		// display the final status of the upload.
		string redirectPath = String.Format("~/Brettle.Web.NeatUpload/tests/StaticPage.htm?numFilesReceived={0}&postBackID={1}",
			numFilesReceived, UploadModule.PostBackID);
        context.Response.Redirect(redirectPath);
    }
 
    public bool IsReusable {
        get {
            return true;
        }
    }

}