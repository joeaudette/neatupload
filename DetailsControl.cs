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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Security.Permissions;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Abstract base class for controls which are dynamically filled with upload progress/status information using
	/// data-binding expressions.</summary>
	/// <remarks>
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[ParseChildren(false)]
	[PersistChildren(true)]
	public abstract class DetailsControl : System.Web.UI.WebControls.WebControl
	{
		private bool IsDesignTime = (HttpContext.Current == null);
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			PreRender += new EventHandler(Control_PreRender);
		}
		
		private void Control_PreRender(object sender, EventArgs args)
		{
			if (IsDesignTime) return;
			
			// Find the current upload context
			string postBackID = Page.Request.Params["postBackID"];
			UploadContext uploadContext = UploadContext.FindByID(postBackID);

			// Set the status to Cancelled if requested.
			if (uploadContext != null && Page.Request.Params["cancelled"] == "true")
			{
				uploadContext.Status = UploadStatus.Cancelled;
			}
			
			if (uploadContext == null || uploadContext.Status == UploadStatus.Unknown)
			{
				// Status is unknown, so try to find the last post back based on the lastPostBackID param.
				// If successful, use the status of the last post back.
				string lastPostBackID = Page.Request.Params["lastPostBackID"];
				if (lastPostBackID != null && lastPostBackID.Length > 0 && Page.Request.Params["refresher"] == null)
				{
					uploadContext = UploadContext.FindByID(lastPostBackID);
					if (uploadContext.NumUploadedFiles == 0)
					{
						uploadContext = null;
					}
				}
			}
			
			Status = (uploadContext == null) ? UploadStatus.Unknown : uploadContext.Status;
			Attributes["status"] = Status.ToString();
			
			string whenStatus = Attributes["WhenStatus"];
			if (whenStatus != null)
			{
				string[] matchingStatuses = whenStatus.Split(' ');
				if (Array.IndexOf(matchingStatuses, Status.ToString()) == -1)
				{
					this.Visible = false;
				}
			}
		}
		
		private UploadStatus Status = UploadStatus.Unknown;
		
		protected override void Render(HtmlTextWriter writer)
		{
			EnsureChildControls();
			base.AddAttributesToRender(writer);
			if (!IsDesignTime && Page.Request.Params["useXml"] == "true")
			{
				writer.RenderBeginTag("neatUploadDetails"); // Ignored by browser.  Children are displayed.
			}
			else
			{
				writer.RenderBeginTag(TagName);
			}
			base.RenderChildren(writer);
			writer.RenderEndTag();
		}		
	}
}
