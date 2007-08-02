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
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload
{
	public class Progress : ProgressPage
	{
/*
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
*/
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
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			this.Load += new EventHandler(this.Page_Load);
		}
				
		private void Page_Load(object sender, EventArgs e)
		{
			if (cancelLink != null)
			{
				cancelLink.HRef = CancelUrl;
				cancelLink.Visible = CancelVisible;
			}
			if (stopRefreshLink != null)
			{
				stopRefreshLink.HRef = StopRefreshUrl;
				stopRefreshLink.Visible = StopRefreshVisible;
			}
			if (refreshLink != null)
			{
				refreshLink.HRef = StartRefreshUrl;
				refreshLink.Visible = StartRefreshVisible;
			}
			
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
			
			if (Status == UploadStatus.Unknown)
			{
				if (barDiv != null)
					barDiv.Style["width"] = "0";
			}
			else
			{
				if (barDiv != null)
					barDiv.Style["width"] = Math.Round(FractionComplete * 100) + "%";
				
				SetControlText(remainingTimeSpan, FormatTimeSpan(this.TimeRemaining));
				
				if ((Status == UploadStatus.Cancelled
				     || Status == UploadStatus.Rejected
				     || Status == UploadStatus.Failed)
			        && cancelledSpan != null)
				{
					cancelledSpan.Visible = true;
				}
				else if ((Status == UploadStatus.NormalInProgress
				          || Status == UploadStatus.ChunkedInProgress)
				         && inProgressSpan != null)
				{
					inProgressSpan.Visible = true;
				}
				else if (Status == UploadStatus.Completed && completedSpan != null)
				{
					completedSpan.Visible = true;
				}
				if (BytesTotal >= 0 && remainingTimeSpan != null)
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
	}
}