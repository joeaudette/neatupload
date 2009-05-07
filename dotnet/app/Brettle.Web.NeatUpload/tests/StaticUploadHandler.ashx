<%@ WebHandler Language="C#" Class="StaticUploadHandler" %>

using System;
using System.Web;
using Brettle.Web.NeatUpload;

public class StaticUploadHandler : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
		// Force NeatUpload to parse the request.
		// TODO: Make accessing UploadModule.Files force the request to parsed.
		string submitValue = context.Request.Form["submit"]; 
		string fileName = null;
		// If a file was uploaded, get it
		if (UploadModule.Files != null && UploadModule.Files.Count > 0)
		{
			UploadedFile file = UploadModule.Files[0];
			// Process the file as desired, calling UploadedFile.MoveTo() if you want to keep it
			fileName = file.FileName;
		}
		
		// Write a response or redirect as desired.
        context.Response.Redirect(context.Response.ApplyAppPathModifier("~/Brettle.Web.NeatUpload/tests/StaticPage.htm?fileName=" + context.Server.UrlEncode(fileName)));
    }
 
    public bool IsReusable {
        get {
            return true;
        }
    }

}