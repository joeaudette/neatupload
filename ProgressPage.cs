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
*/

using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload
{
	public class ProgressPage : Page
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
		}
		
		protected long BytesRead;
		protected long BytesTotal;
		protected double FractionComplete;
		protected int BytesPerSec;
		protected UploadException Rejection;
		protected Exception Failure;
		protected TimeSpan TimeRemaining;
		protected TimeSpan TimeElapsed;
		protected string CurrentFileName;
		protected string ProcessingHtml;		
		
		protected UploadStatus Status = UploadStatus.Unknown;
		
		protected bool CancelVisible;
		protected bool StartRefreshVisible;
		protected bool StopRefreshVisible;
		protected string CancelUrl;
		protected string StartRefreshUrl;
		protected string StopRefreshUrl;
		
		protected virtual string GetResourceString(string resourceName)
		{
			return Config.Current.GetResourceString(resourceName);
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
		
		private UploadContext UploadContext;					
		private string ProgressBarID;					
		private UploadStatus CurrentStatus = UploadStatus.Unknown;
		private bool CanScript;
		private bool CanCancel;
		private bool IsRefreshing;
		private string RefreshUrl;
		
		protected override void OnLoad(EventArgs e)
		{
			RegisterClientScriptBlock("NeatUpload-ProgressPage", "<script type='text/javascript' language='javascript' src='Progress.js'></script>");
			SetupContext();
			SetupBindableProps();
			
			// Set the status to Cancelled if requested.
			if (this.UploadContext != null && Request.Params["cancelled"] == "true")
			{
				this.UploadContext.Status = UploadStatus.Cancelled;
			}
			
			if (Request.Params["useXml"] == "true")
			{
				Response.ContentType = "text/xml";
				Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
				Response.Write(@"<?xml version=""1.0"" encoding=""utf-8"" ?>");
			}
			
			string prevStatus = Request.Params["prevStatus"];
			if (prevStatus == null)
			{
				prevStatus = UploadStatus.Unknown.ToString();
			}
			string curStatus = Status.ToString();

			// If the status is unchanged from the last refresh and it is not Unknown nor *InProgress,
			// then the page is not refreshed.  Instead, if an UploadException occurred we try to cancel
			// the upload.  Otherwise, no exception occurred we close the progress display window (if it's a pop-up)  
			if (curStatus == prevStatus 
			       && (curStatus != UploadStatus.Unknown.ToString()
			           && curStatus != UploadStatus.NormalInProgress.ToString()
			           && curStatus != UploadStatus.ChunkedInProgress.ToString()
			           && curStatus != UploadStatus.ProcessingInProgress.ToString()
			           ))
			{
				if (curStatus == UploadStatus.Rejected.ToString())
				{
					if (CanCancel)
					{
						RegisterStartupScript("scrNeatUploadError", @"
<script type=""text/javascript"" language=""javascript"">
<!--
window.onload = function() {
		NeatUploadCancel();
}
// -->
</script>");
					}
				}
				else if (curStatus != UploadStatus.Failed.ToString())
				{
					RegisterStartupScript("scrNeatUploadClose", @"<script type='text/javascript' language='javascript'>
<!--
NeatUploadMainWindow.NeatUploadPB.prototype.Bars['" + ProgressBarID + @"'].Close();
// -->
</script>");
				}
			}
			// Otherwise, if we are refreshing, we refresh the page in one second
			else if (IsRefreshing)
			{
				// And add a prevStatus param to the url to track the previous status. 			
				string refreshUrl = RefreshUrl + "&prevStatus=" + curStatus;
				Refresh(refreshUrl);
			}
			base.OnLoad(e);			
		}
		
		private void SetupContext()
		{
			// Find the current upload context
			string postBackID = Request.Params["postBackID"];
			ProgressBarID = Request.Params["barID"];
			this.UploadContext = UploadContext.FindByID(postBackID);
			if (log.IsDebugEnabled) log.Debug("FindByID() returned " + this.UploadContext + " when SessionID = " 
				+ (HttpContext.Current.Session != null ? HttpContext.Current.Session.SessionID : null));

			if (this.UploadContext == null || this.UploadContext.Status == UploadStatus.Unknown)
			{
				CurrentStatus = UploadStatus.Unknown;
				// Status is unknown, so try to find the last post back based on the lastPostBackID param.
				// If successful, use the status of the last post back.
				string lastPostBackID = Page.Request.Params["lastPostBackID"];
				if (lastPostBackID != null && lastPostBackID.Length > 0 && Page.Request.Params["refresher"] == null)
				{
					this.UploadContext = UploadContext.FindByID(lastPostBackID);
					if (this.UploadContext != null && this.UploadContext.FileBytesRead == 0
						&& this.UploadContext.ProgressInfoByID.Count == 0)
					{
						this.UploadContext = null;
					}
				}
			}
			else
			{
				if (log.IsDebugEnabled) log.Debug("In ProgressPage, UploadContext.PostBackID = " + this.UploadContext.PostBackID);
				if (log.IsDebugEnabled) log.Debug("In ProgressPage, UploadContext.Status = " + this.UploadContext.Status);
				if (log.IsDebugEnabled) log.Debug("In ProgressPage, UploadContext.Exception = " + this.UploadContext.Exception);
				CurrentStatus = this.UploadContext.Status;
			}
			
		}
		
		private void SetupBindableProps()
		{
			if (this.UploadContext != null)
			{
				lock (this.UploadContext)
				{
					ProgressInfo progress = null;
					if (ProgressBarID != null)
					{
						progress = (ProgressInfo)this.UploadContext.ProgressInfoByID[ProgressBarID];
					}
					if (progress != null)
					{
						FractionComplete = 1.0 * progress.Value / progress.Maximum;
						ProcessingHtml = progress.ToHtml();
					}
					else
					{
						FractionComplete = this.UploadContext.FractionComplete;
						ProcessingHtml = GetResourceString("ProcessingMessage");
					}
					BytesRead = this.UploadContext.BytesRead;
					BytesTotal = this.UploadContext.BytesTotal;
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
					Status = this.UploadContext.Status;
				}
			}
			CanScript = (Request.Params["canScript"] != null && Boolean.Parse(Request.Params["canScript"]));
			CanCancel = (Request.Params["canCancel"] != null && Boolean.Parse(Request.Params["canCancel"]));
			IsRefreshing = (Request.Params["refresh"] != "false" && Request.Params["refresher"] != null);
			StartRefreshVisible = (!CanScript && !IsRefreshing
		                          && (CurrentStatus == UploadStatus.Unknown
                                      || CurrentStatus == UploadStatus.NormalInProgress
                                      || CurrentStatus == UploadStatus.ChunkedInProgress));
			StopRefreshVisible = (!CanScript && IsRefreshing
		                          && (CurrentStatus == UploadStatus.Unknown
                                      || CurrentStatus == UploadStatus.NormalInProgress
                                      || CurrentStatus == UploadStatus.ChunkedInProgress));
			CancelVisible = (CanCancel
		                    && (CurrentStatus == UploadStatus.NormalInProgress || CurrentStatus == UploadStatus.ChunkedInProgress));
			
			// The base refresh url contains just the barID and postBackID

			RefreshUrl = Request.Url.AbsolutePath;
			RefreshUrl += "?barID=" + ProgressBarID + "&postBackID=" + Request.Params["postBackID"];
			
			// Workaround Mono XSP bug where ApplyAppPathModifier() removes the session id
			RefreshUrl = ProgressBar.ApplyAppPathModifier(RefreshUrl);

			RefreshUrl += "&canScript=" + CanScript + "&canCancel=" + CanCancel;
			StartRefreshUrl = RefreshUrl + "&refresher=server";	
			StopRefreshUrl = RefreshUrl + "&refresh=false";	
			CancelUrl = "javascript:NeatUpload_CancelClicked()";
			
			DataBind();			
		}
		
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
			RegisterStartupScript("scrNeatUploadRefresh", @"<script type='text/javascript' language='javascript'>
<!--
NeatUploadRefreshUrl = '" + refreshUrl + @"';
window.onload = NeatUpload_CombineHandlers(window.onload, function () 
{
	NeatUploadReloadTimeoutId = setTimeout('NeatUploadRefresh()', 1000);
});
// -->
</script>");
		}
		
		private void RefreshWithServerHeader(string refreshUrl)
		{
			refreshUrl += "&refresher=server";
			Response.AddHeader("Refresh", "1; URL=" + refreshUrl);
		}
	}
}
