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
	
		protected string DynamicStyle;

		protected HtmlGenericControl barDiv;
		protected HtmlGenericControl statusDiv;
		protected HtmlGenericControl inProgressSpan;
		protected HtmlGenericControl remainingTimeSpan;
		protected HtmlGenericControl completedSpan;
		protected HtmlGenericControl cancelledSpan;
		protected HtmlAnchor refreshLink;
		protected HtmlAnchor stopRefreshLink;
		protected HtmlAnchor cancelLink;
		protected HtmlImage refreshImage;
		protected HtmlImage stopRefreshImage;
		protected HtmlImage cancelImage;
		
		protected double FractionComplete;
		
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
<!--
NeatUploadScript = null;

NeatUploadReq = null;
function NeatUploadRefreshWithScript(url) 
{
	NeatUploadReq = null;
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
		NeatUploadReq.onreadystatechange = NeatUploadUpdateHtml;
		NeatUploadReq.open(""GET"", url);
		NeatUploadReq.send(null);
	}
	else
	{
		return false;
	}
	return true;
}

function NeatUploadUpdateHtml()
{
	if (typeof(NeatUploadReq) != ""undefined"" && NeatUploadReq.readyState == 4) 
	{
		try
		{
			var responseXmlDoc = NeatUploadReq.responseXML;
			if (responseXmlDoc.parseError && responseXmlDoc.parseError.errorCode != 0)
			{
//				window.alert('parse error: ' + responseXmlDoc.parseError.reason);
			}
			var templates = responseXmlDoc.getElementsByTagName('neatUploadDetails');
			var status = templates.item(0).getAttribute('status');
			for (var t = 0; t < templates.length; t++)
			{
				var srcElem = templates.item(t);
				var innerXml = '';
				for (var i = 0; i < srcElem.childNodes.length; i++)
				{
					var childNode = srcElem.childNodes.item(i);
					var xml = childNode.xml;
					if (xml == null)
						xml = new XMLSerializer().serializeToString(childNode);
					innerXml += xml;
				}
				var id = srcElem.getAttribute('id');
				var destElem = document.getElementById(id);
				destElem.innerHTML = innerXml;
				for (var a=0; a < srcElem.attributes.length; a++)
				{
					var attr = srcElem.attributes.item(a);
					if (attr.specified)
					{
						if (attr.name == 'style' && destElem.style && destElem.style.cssText)
							destElem.style.cssText = attr.value;
						else
							destElem.setAttribute(attr.name, attr.value);
					}
				}
			}
			if (status != 'NormalInProgress' && status != 'ChunkedInProgress' && status != 'Unknown')
			{
				NeatUploadRefreshPage();
			}
			var lastMillis = NeatUploadLastUpdate.getTime();
			NeatUploadLastUpdate = new Date();
			var delay = Math.max(lastMillis + 1000 - NeatUploadLastUpdate.getTime(), 1);
			NeatUploadReloadTimeoutId = setTimeout(NeatUploadRefresh, delay);
		}
		catch (ex)
		{
//			window.alert(ex);
			NeatUploadRefreshPage();
		}
	}
}

function NeatUploadRefresh()
{
	if (!NeatUploadRefreshWithScript('REFRESHURL&useXml=true'))
	{
		NeatUploadRefreshPage();
	}
}

function NeatUploadRefreshPage() 
{
	window.location.replace('REFRESHURL');
}

NeatUploadLastUpdate = new Date(); 

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
// -->
</script>";
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			this.PreRender += new EventHandler(this.Page_PreRender);
			this.Load += new EventHandler(this.Page_Load);
		}
		
		private UploadContext UploadContext;					
		
		private void Page_Load(object sender, EventArgs e)
		{
			// Find the current upload context
			string postBackID = Request.Params["postBackID"];
			this.UploadContext = UploadContext.FindByID(postBackID);

			if (this.UploadContext == null || this.UploadContext.Status == UploadStatus.Unknown)
			{
				// Status is unknown, so try to find the last post back based on the lastPostBackID param.
				// If successful, use the status of the last post back.
				string lastPostBackID = Page.Request.Params["lastPostBackID"];
				if (lastPostBackID != null && lastPostBackID.Length > 0 && Page.Request.Params["refresher"] == null)
				{
					this.UploadContext = UploadContext.FindByID(lastPostBackID);
					if (this.UploadContext.NumUploadedFiles == 0)
					{
						this.UploadContext = null;
					}
				}
			}
			
			if (this.UploadContext != null)
			{
				lock (this.UploadContext)
				{
					FractionComplete = this.UploadContext.FractionComplete;
					BytesRead = this.UploadContext.BytesRead;
					BytesTotal = this.UploadContext.ContentLength;
					BytesPerSec = this.UploadContext.BytesPerSec;
					if (this.UploadContext.Exception is UploadException)
					{
						Rejection = (UploadException)this.UploadContext.Exception;
					}
					else
					{
						Failure = this.UploadContext.Exception;
					}
					TimeRemaining = this.UploadContext.TimeRemaining;
					TimeElapsed = this.UploadContext.TimeElapsed;
					CurrentFileName = this.UploadContext.CurrentFileName;
				}
			}
			this.DataBind();
		}
		
		protected long BytesRead;
		protected long BytesTotal;
		protected int BytesPerSec;
		protected UploadException Rejection;
		protected Exception Failure;
		protected TimeSpan TimeRemaining;
		protected TimeSpan TimeElapsed;
		protected string CurrentFileName;
		
		protected string GetResourceString(string resourceName)
		{
			return Config.Current.ResourceManager.GetString(resourceName);
		}
		
		protected string FormatCount(long count)
		{
			lock(this)
			{
				string format;
				if (UnitSelector < 1000)
					format = GetResourceString("ByteCountFormat");
				else if (UnitSelector < 1000*1000)
					format = GetResourceString("KBCountFormat");
				else
					format = GetResourceString("MBCountFormat");
				return String.Format(format, count);
			}
		}
		
		private long UnitSelector
		{
			get { return (BytesTotal < 0) ? BytesRead : BytesTotal; }
		}
				
		protected string CountUnits
		{
			get
			{
				if (UnitSelector < 1000)
					return GetResourceString("ByteUnits");
				else if (UnitSelector < 1000*1000)
					return GetResourceString("KBUnits");
				else
					return GetResourceString("MBUnits");
			}
		}
		
		protected string FormatRate(int rate)
		{
			string format;
			if (rate < 1000)
				format = GetResourceString("ByteRateFormat");
			else if (rate < 1000*1000)
				format = GetResourceString("KBRateFormat");
			else
				format = GetResourceString("MBRateFormat");
			return String.Format(format, rate);
		}					
				
		protected string FormatTimeSpan(TimeSpan ts)
		{
			string format;
			if (ts.TotalSeconds < 60)
				format = GetResourceString("SecondsFormat");
			else if (ts.TotalSeconds < 60*60)
				format = GetResourceString("MinutesFormat");
			else
				format = GetResourceString("HoursFormat");
			return String.Format(format,
			                          (int)Math.Floor(ts.TotalHours),
			                          (int)Math.Floor(ts.TotalMinutes),
			                          ts.Seconds,
			                          ts.TotalHours,
			                          ts.TotalMinutes);
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
<!--
NeatUploadLinkNode = document.getElementById('" + refreshLink.ClientID + @"');
if (NeatUploadLinkNode) NeatUploadLinkNode.parentNode.removeChild(NeatUploadLinkNode);
// -->
</script>");

			// Set the status to Cancelled if requested.
			if (this.UploadContext != null && Request.Params["cancelled"] == "true")
			{
				this.UploadContext.Status = UploadStatus.Cancelled;
			}
			
			UpdateProgressBar();
			
			string prevStatus = Request.Params["prevStatus"];
			if (prevStatus == null)
			{
				prevStatus = UploadStatus.Unknown.ToString();
			}
			string curStatus = UploadStatus.Unknown.ToString();
			if (this.UploadContext != null)
			{
				curStatus = this.UploadContext.Status.ToString();
			}

			// If the status is unchanged from the last refresh and it is not Unknown nor *InProgress,
			// then the page is not refreshed.  Instead, if an UploadException occurred we try to cancel
			// the upload.  Otherwise, we close the progress display window (if it's a pop-up)  
			if (curStatus == prevStatus 
			       && (curStatus != UploadStatus.Unknown.ToString()
			           && curStatus != UploadStatus.NormalInProgress.ToString()
			           && curStatus != UploadStatus.ChunkedInProgress.ToString()))
			{
				if (curStatus == UploadStatus.Rejected.ToString())
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

		private void UpdateProgressBar()
		{
			
			HtmlGenericControl[] spans = new HtmlGenericControl[]
			{
				inProgressSpan,
				completedSpan,
				cancelledSpan,
			};
			foreach (HtmlGenericControl s in spans)
			{
				if (s != null) s.Visible = false;
			}
			
			if (Request.Params["useXml"] == "true")
			{
				Response.ContentType = "text/xml";
				Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
				Response.Write(@"<?xml version=""1.0"" encoding=""utf-8"" ?>");
			}
					
			if (this.UploadContext == null)
			{
				if (barDiv != null)
					barDiv.Style["width"] = "0";
			}
			else
			{
				if (barDiv != null)
					barDiv.Style["width"] = Math.Round(this.UploadContext.FractionComplete * 100) + "%";
				
				SetControlText(remainingTimeSpan, FormatTimeSpan(this.TimeRemaining));
				
				if ((this.UploadContext.Status == UploadStatus.Cancelled
				     || this.UploadContext.Status == UploadStatus.Rejected
				     || this.UploadContext.Status == UploadStatus.Failed)
			        && cancelledSpan != null)
				{
					cancelledSpan.Visible = true;
				}
				else if ((this.UploadContext.Status == UploadStatus.NormalInProgress
				          || this.UploadContext.Status == UploadStatus.ChunkedInProgress)
				         && inProgressSpan != null)
				{
					inProgressSpan.Visible = true;
				}
				else if (this.UploadContext.Status == UploadStatus.Completed && completedSpan != null)
				{
					completedSpan.Visible = true;
				}
				if (this.UploadContext.ContentLength >= 0 && remainingTimeSpan != null)
				{
					remainingTimeSpan.Visible = true;
				}
			}
		}

		private void SetControlText(HtmlGenericControl c, string text)
		{
			if (c != null)
			{
				c.Visible = true;
				c.InnerText = text;
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
			RegisterStartupScript("scrNeatUploadRefresh", clientRefreshScript.Replace("REFRESHURL", refreshUrl));
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
