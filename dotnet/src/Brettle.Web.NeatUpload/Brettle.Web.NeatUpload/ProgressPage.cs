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
using Brettle.Web.NeatUpload.Internal;

namespace Brettle.Web.NeatUpload
{
	public class ProgressPage : Page, IUploadProgressState
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
		
		private UploadStatus _Status = UploadStatus.Unknown;
		public UploadStatus Status {
			get { return _Status; } 
			set { _Status = value; } 
		}

		private long _BytesRead;
		public long BytesRead {
			get { return _BytesRead; }
			set { _BytesRead = value; }
		}

		private long _FileBytesRead;
		public long FileBytesRead {
			get { return _FileBytesRead; }
			set { _FileBytesRead = value; }
		}

		private long _BytesTotal;
		public long BytesTotal {
			get { return _BytesTotal; }
			set { _BytesTotal = value; }
		}

		private double _FractionComplete;
		public double FractionComplete {
			get { return _FractionComplete; }
			set { _FractionComplete = value; }
		}

		private int _BytesPerSec;
		public int BytesPerSec {
			get { return _BytesPerSec; }
			set { _BytesPerSec = value; }
		}

		private UploadException _Rejection;
		public UploadException Rejection {
			get { return _Rejection; }
			set { _Rejection = value; }
		}

		private Exception _Failure;
		public Exception Failure {
			get { return _Failure; }
			set { _Failure = value; }
		}

		private TimeSpan _TimeRemaining;
		public TimeSpan TimeRemaining {
			get { return _TimeRemaining; }
			set { _TimeRemaining = value; }
		}

		private TimeSpan _TimeElapsed;
		public TimeSpan TimeElapsed {
			get { return _TimeElapsed; }
			set { _TimeElapsed = value; }
		}

		private string _CurrentFileName;
		public string CurrentFileName {
			get { return _CurrentFileName; }
			set { _CurrentFileName = value; }
		}

		private UploadedFileCollection _Files;
		/// <summary>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the upload.
		/// </summary>
		/// <value>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the upload.
		/// </value>
		public UploadedFileCollection Files { 
			get { return _Files; }
			set	{ _Files = value; }
		}

		private object _ProcessingState;
		public object ProcessingState {
			get { return _ProcessingState; }
			set { _ProcessingState = value; }
		}

		protected string ProcessingHtml;
		protected bool CancelVisible;
		protected bool StartRefreshVisible;
		protected bool StopRefreshVisible;
		protected string CancelUrl;
		protected string StartRefreshUrl;
		protected string StopRefreshUrl;
		
		// This is used to ensure that the browser gets the latest NeatUpload.js each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when NeatUpload.js changes,
		// but computing a hash on that file everytime this assembly is loaded strikes me as overkill.
		private static Guid CacheBustingGuid = System.Guid.NewGuid();

		protected virtual string GetResourceString(string resourceName)
		{
			return ResourceManagerSingleton.GetResourceString(resourceName);
		}
		
		protected string FormatCount(long count)
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
		
		private string ProgressBarID;					
		private string PostBackID;					
		private UploadStatus CurrentStatus = UploadStatus.Unknown;
		private bool CanScript;
		private bool CanCancel;
		private bool IsRefreshing;
		private string RefreshUrl;
		
		protected override void OnLoad(EventArgs e)
		{
			RegisterClientScriptBlock("NeatUpload-ProgressPage", "<script type='text/javascript' language='javascript' src='Progress.js?guid=" 
			                          + CacheBustingGuid + @"'></script>");
			SetupContext();
			SetupBindableProps();
			
			// Set the status to Cancelled if requested.
			if (Request.Params["cancelled"] == "true")
			{
				UploadModule.CancelPostBack(PostBackID);
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
                   && (Status != UploadStatus.Unknown
                       && Status != UploadStatus.NormalInProgress
                       && Status != UploadStatus.ChunkedInProgress
                       && Status != UploadStatus.ProcessingInProgress
			           ))
			{
                if (Status == UploadStatus.Rejected
                    || (Status == UploadStatus.Failed && Failure is HttpException
                        && ((HttpException)Failure).GetHttpCode() == 400))
				{
					if (CanCancel)
					{
						RegisterStartupScript("scrNeatUploadError", @"
<script type=""text/javascript"" language=""javascript"">
<!--
window.onload = function() {
		NeatUploadStop('" + Status + @"');
}
// -->
</script>");
					}
				}
                else if (Status != UploadStatus.Failed)
				{
					RegisterStartupScript("scrNeatUploadClose", @"<script type='text/javascript' language='javascript'>
<!--
if (NeatUploadMainWindow.NeatUploadPB.prototype.Bars['" + ProgressBarID + @"'].EvalOnClose)
	eval(NeatUploadMainWindow.NeatUploadPB.prototype.Bars['" + ProgressBarID + @"'].EvalOnClose);
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
			PostBackID = Request.Params["postBackID"];
			ProgressBarID = Request.Params["barID"];
			UploadModule.BindProgressState(PostBackID, ProgressBarID, this);
			if (log.IsDebugEnabled) log.Debug("Status " + Status + " when SessionID = " 
				+ (HttpContext.Current.Session != null ? HttpContext.Current.Session.SessionID : null));

			if (Status == UploadStatus.Unknown)
			{
				CurrentStatus = UploadStatus.Unknown;
				// Status is unknown, so try to find the last post back based on the lastPostBackID param.
				// If successful, use the status of the last post back.
				string lastPostBackID = Page.Request.Params["lastPostBackID"];
				if (lastPostBackID != null && lastPostBackID.Length > 0 && Page.Request.Params["refresher"] == null)
				{
					UploadModule.BindProgressState(lastPostBackID, ProgressBarID, this);
					if (FileBytesRead == 0 && ProcessingState != null)
					{
						Status = UploadStatus.Unknown;
					}
				}
			}
			else
			{
				if (log.IsDebugEnabled) log.Debug("In ProgressPage, PostBackID = " + PostBackID);
				if (log.IsDebugEnabled) log.Debug("In ProgressPage, Status = " + Status);
				CurrentStatus = Status;
			}
			
		}
		
		private void SetupBindableProps()
		{
			ProcessingHtml = GetResourceString("ProcessingMessage");
			if (ProcessingState != null)
			{
				ProgressInfo progress = (ProgressInfo)ProcessingState;
				FractionComplete = 1.0 * progress.Value / progress.Maximum;
				ProcessingHtml = progress.ToHtml();
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
			RefreshUrl += "?barID=" + ProgressBarID + "&postBackID=" + PostBackID;
			
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
