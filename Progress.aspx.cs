/*
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005  Dean Brettle

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

clientRefreshScript updated for Javascript Postback Module
released under GNU Lesser General Public License
Copyright (C) 2005  Stefano Straus (tustena.sf.net)
*/

using System;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload
{
	public class Progress : Page
	{
/*
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
*/
	
		protected HtmlGenericControl barDiv;
		protected HtmlGenericControl statusDiv;
		protected HtmlGenericControl inProgressSpan;
		protected HtmlGenericControl remainingTimeSpan;
		protected HtmlGenericControl completedSpan;
		protected HtmlGenericControl cancelledSpan;
		protected HtmlGenericControl rejectedSpan;
		protected HtmlGenericControl rejectedMessageSpan;
		protected HtmlGenericControl errorSpan;
		protected HtmlGenericControl errorMessageSpan;
		protected HtmlAnchor refreshLink;
		protected HtmlAnchor stopRefreshLink;
		protected HtmlAnchor cancelLink;
		protected HtmlImage refreshImage;
		protected HtmlImage stopRefreshImage;
		protected HtmlImage cancelImage;

		private string nonRefreshScriptFuncs = @"<script language=""javascript"">
<!--
function NeatUploadGetMainWindow() 
{
	var mainWindow;
	if (window.opener) 
		mainWindow = window.opener;
	else 
		mainWindow = window.parent;
	return mainWindow;
};

function NeatUploadCancel() 
{
	var mainWindow = NeatUploadGetMainWindow();
	if (mainWindow && mainWindow.stop)
		mainWindow.stop();
	else if (mainWindow && mainWindow.document && mainWindow.document.execCommand)
		mainWindow.document.execCommand('Stop');
}

function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { origHandler(e); newHandler(e); };
}

function NeatUploadCanCancel()
{
	var mainWindow = NeatUploadGetMainWindow();
	try
	{
		return (mainWindow.stop || mainWindow.document.execCommand);
	}
	catch (ex)
	{
		return false;
	}
}

function NeatUploadRemoveCancelLink()
{
	NeatUploadLinkNode = document.getElementById('cancelLink');
	if (NeatUploadLinkNode) 
		NeatUploadLinkNode.parentNode.removeChild(NeatUploadLinkNode);
}
-->
</script>";
		
		private string clientRefreshScript = @"<script language=""javascript"">

NeatUploadScript = null;

NeatUploadReq = null;
function NeatUploadRefreshWithScript(url) 
{
	var req = null;
	try
	{
		req = new ActiveXObject(""Microsoft.XMLHTTP"");
	}
	catch (ex)
	{
		req = null;
	}
	if (!req && typeof(XMLHttpRequest) != ""undefined"")
	{
		req = new XMLHttpRequest();
	}
	if (req)
	{
		NeatUploadReq = req;
	}
	if (NeatUploadReq)
	{
		NeatUploadReq.onreadystatechange = NeatUploadEvalScript;
		NeatUploadReq.open(""GET"", url);
		NeatUploadReq.send(null);
	}
	else if (document.getElementById) 
	{
		if (navigator.appVersion.indexOf(""Mac"") != -1) 
		{
			NeatUploadScript = document.createElement('div');
			NeatUploadScript.innerHTML = '<sc' + 'ript type=\'text/javascript\' src=\'' + url + '\'><\/sc' + 'ript>';
			document.body.appendChild(NeatUploadScript);
		}
		else
		{
			NeatUploadScript = document.createElement('script');
			NeatUploadScript.defer = true;
			document.body.appendChild(NeatUploadScript);
			NeatUploadScript.src = url;
		}
	}
	else
	{
		return false;
	}
	return true;
}

function NeatUploadEvalScript()
{
	if (typeof(NeatUploadReq) != ""undefined"" && NeatUploadReq.readyState == 4) 
	{
		try
		{
			eval(NeatUploadReq.responseText);
		}
		catch (ex)
		{
			NeatUploadRefreshPage();
		}
	}
}

function NeatUploadRefresh()
{
	if (!NeatUploadRefreshWithScript('PROGRESSSCRIPTURL?' + document.location.search.substring(1) + '&' + Math.random()))
	{
		NeatUploadRefreshPage();
	}
}

function NeatUploadRefreshPage() 
{
	window.location.replace('REFRESHURL');
}

NeatUploadLastUpdate = new Date(); 
function NeatUploadUpdateDom(upload)
{
	if (NeatUploadScript)
	{
		document.body.removeChild(NeatUploadScript);
		NeatUploadScript = null;
	}
	if (typeof( upload.progress ) != ""undefined"")
		document.getElementById(""barDiv"").style.width = upload.progress + ""%"";
	if (typeof( upload.remainingtime ) != ""undefined"")
		document.getElementById(""remainingTimeSpan"").innerHTML = upload.remainingtime;
	if (typeof( upload.status ) != ""undefined"" && upload.status == ""inprogress"")
	{
		var lastMillis = NeatUploadLastUpdate.getTime();
		NeatUploadLastUpdate = new Date();
		var delay = Math.max(lastMillis + 1000 - NeatUploadLastUpdate.getTime(), 1);
		NeatUploadReloadTimeoutId = setTimeout(NeatUploadRefresh, delay);
	}
	else
	{
		NeatUploadRefreshPage();
	}
}

NeatUploadReloadTimeoutId = window.setTimeout(NeatUploadRefresh, 1000);

window.onunload = NeatUpload_CombineHandlers(window.onunload, function () 
{
	if (NeatUploadReq && NeatUploadReq.readystate
		&& NeatUploadReq.readystate >= 1 && NeatUploadReq.readystate <=3)
	{
		NeatUploadReq.abort();
	}
	NeatUploadReq = null;
});

NeatUploadReloadTimeoutId = setTimeout(NeatUploadRefresh, 1000);

NeatUploadMainWindow = NeatUploadGetMainWindow();

if (!NeatUploadCanCancel)
{
	NeatUploadRemoveCancelLink();
}
</script>";
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			this.PreRender += new EventHandler(this.Page_PreRender);
		}

		private void Page_PreRender(object sender, EventArgs e)
		{
			this.RegisterClientScriptBlock("scrNeatUploadNonRefreshFuncs", nonRefreshScriptFuncs);

			// The base refresh url contains just the postBackID (which is the first parameter)
			string refreshUrl = Request.Url.AbsoluteUri;
			int ampIndex = refreshUrl.IndexOf("&");
			if (ampIndex != -1)
			{
				refreshUrl = refreshUrl.Substring(0, ampIndex);
			}
			/*
			Initially, we make only the refresh button visible.  And we make its url contains
			"refresher=server" to indicate that the server should do a server-side refresh 
			if the button is clicked.  Additionally, we register a startup script to remove the
			refresh button because it is unnecessary if client side scripting is available.
			*/ 
			cancelLink.Visible = false;
			stopRefreshLink.Visible = false;
			refreshLink.Visible = true;
			refreshLink.HRef = refreshUrl + "&refresher=server";	
			RegisterStartupScript("scrNeatUpload", @"<script language=""javascript"">
NeatUploadLinkNode = document.getElementById('" + refreshLink.ClientID + @"');
if (NeatUploadLinkNode) NeatUploadLinkNode.parentNode.removeChild(NeatUploadLinkNode);
</script>");

			// Find the current upload context
			string postBackID = Request.Params["postBackID"];
			UploadContext uploadContext = UploadContext.FindByID(postBackID);

			// Set the status to Cancelled if requested.
			if (uploadContext != null && Request.Params["cancelled"] == "true")
			{
				uploadContext.Status = UploadStatus.Cancelled;
			}
			
			UpdateProgressBar(uploadContext);
			
			string prevStatus = Request.Params["prevStatus"];
			if (prevStatus == null)
			{
				prevStatus = UploadStatus.Unknown.ToString();
			}
			string curStatus = UploadStatus.Unknown.ToString();
			if (uploadContext != null)
			{
				curStatus = uploadContext.Status.ToString();
			}

			// If the status is unchanged from the last refresh and it is not Unknown nor InProgress,
			// then the page is not refreshed.  Instead, if an UploadException occurred we try to cancel
			// the upload.  Otherwise, we close the progress display window (if it's a pop-up)  
			if (curStatus == prevStatus 
				&& (curStatus != UploadStatus.Unknown.ToString() && curStatus != UploadStatus.InProgress.ToString()))
			{
				if (curStatus == UploadStatus.Rejected.ToString() 
					&& this.rejectedSpan != null)
				{
					RegisterStartupScript("scrNeatUploadError", @"
<script language=""javascript"">
<!--
if (NeatUploadCanCancel()) 
{
	NeatUploadCancel();
}
-->
</script>");
				}
				else
				{
					RegisterStartupScript("scrNeatUploadClose", "<script language=\"javascript\">window.close();</script>");
				}
			}
			// Otherwise, if refresh!=false we refresh the page in one second
			else if (Request.Params["refresh"] != "false" && Request.Params["refresher"] != null)
			{
				// Since we will be refreshing, we hide the refresh link.
				refreshLink.Visible = false;
				
				// And add a prevStatus param to the url to track the previous status. 			
				refreshUrl += "&prevStatus=" + curStatus;
 				
				Refresh(refreshUrl);
			}
		}

		private void UpdateProgressBar(UploadContext uploadContext)
		{
			inProgressSpan.Visible = false;
			completedSpan.Visible = false;
			cancelledSpan.Visible = false;
			if (rejectedSpan != null)
			{
				rejectedSpan.Visible = false;
			}
			if (errorSpan != null)
			{
				errorSpan.Visible = false;
			}

			if (uploadContext == null || uploadContext.Status == UploadStatus.Unknown)
			{
				// Status is unknown, so try to find the last post back based on the lastPostBackID param.
				// If successful, use the status of the last post back.
				string lastPostBackID = Request.Params["lastPostBackID"];
				if (lastPostBackID != null && lastPostBackID.Length > 0 && Request.Params["refresher"] == null)
				{
					uploadContext = UploadContext.FindByID(lastPostBackID);
					if (uploadContext.NumUploadedFiles == 0)
					{
						uploadContext = null;
					} 
				}
			}			
						
			if (uploadContext == null)
			{
				barDiv.Style["width"] = "0";
			}
			else
			{
				barDiv.Style["width"] = Math.Round(uploadContext.PercentComplete) + "%";
				Exception = uploadContext.Exception;
				if (uploadContext.Status == UploadStatus.Cancelled)
				{
					cancelledSpan.Visible = true;
				}
				else if (uploadContext.Status == UploadStatus.InProgress)
				{
					TimeSpan tr = uploadContext.TimeRemaining;
					remainingTimeSpan.InnerHtml = String.Format("{0:00}:{1:00}", (int)Math.Floor(tr.TotalMinutes), tr.Seconds);
					inProgressSpan.Visible = true;
				}
				else if (uploadContext.Status == UploadStatus.Completed)
				{
					completedSpan.Visible = true;
				}
				else if (uploadContext.Status == UploadStatus.Rejected)
				{
					if (rejectedSpan != null)
					{
						rejectedSpan.Visible = true;
						if (uploadContext.Exception != null && rejectedMessageSpan != null)
						{
							rejectedMessageSpan.InnerText = uploadContext.Exception.Message;
						}
					}
					else
					{
						cancelledSpan.Visible = true;
					}
				}
				else if (uploadContext.Status == UploadStatus.Error)
				{
					if (errorSpan != null)
					{
						errorSpan.Visible = true;
						if (uploadContext.Exception != null && errorMessageSpan != null)
						{
							errorMessageSpan.InnerText = uploadContext.Exception.Message;
						}
					}
					else
					{
						cancelledSpan.Visible = true;
					}
				}
			}
		}

		protected Exception Exception = null;
		
		private void Refresh(string refreshUrl)
		{
			if (Request.Params["refresher"] == "client")
			{
				RefreshWithClientScript(refreshUrl);
			}
			else
			{
				RefreshWithServerHeader(refreshUrl);
			}  
		}
		
		private void RefreshWithClientScript(string refreshUrl)
		{
			refreshUrl += "&refresher=client";
			cancelLink.Visible = true;
			cancelLink.HRef = refreshUrl + "&cancelled=true";	
			cancelLink.Attributes["onclick"] = "javascript: NeatUploadCancel();";
			string progressScriptUrl = Request.Url.AbsoluteUri;
			progressScriptUrl = progressScriptUrl.Substring(0, progressScriptUrl.LastIndexOf('/')) + "/ProgressScript.aspx";
			RegisterStartupScript("scrNeatUploadRefresh", clientRefreshScript.Replace("REFRESHURL", refreshUrl).Replace("PROGRESSSCRIPTURL", progressScriptUrl));
		}
		
		private void RefreshWithServerHeader(string refreshUrl)
		{
			refreshUrl += "&refresher=server";
			Response.AddHeader("Refresh", "1; URL=" + refreshUrl);
			stopRefreshLink.Visible = true;
			stopRefreshLink.HRef = refreshUrl + "&refresh=false";
		}
	}
}
