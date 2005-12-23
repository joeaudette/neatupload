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
window.onload = function() {
	if (NeatUploadCanCancel()) 
	{
		NeatUploadCancel();
	}
}
// -->
</script>");
				}
				else
				{
					RegisterStartupScript("scrNeatUploadClose", @"<script language='javascript'>
<!--
window.close();
// -->
</script>");
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
			RegisterStartupScript("scrNeatUploadRefresh", @"<script language='javascript'>
<!--
NeatUploadRefreshUrl = '" + refreshUrl + @"';
// -->
</script>");
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
