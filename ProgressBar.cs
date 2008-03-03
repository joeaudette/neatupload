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
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Security.Permissions;
using System.Web.UI.Design;
using System.Drawing.Design;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Displays progress and status of an upload.</summary>
	/// <remarks>
	/// For the progress bar to be displayed, the <see cref="UploadHttpModule"/> must be in use.
	/// For the progress display to be started, the form being submitted must include an <see cref="InputFile"/>
	/// control that is not empty.  Use the <see cref="Inline"/> property to control how the progress bar is
	/// displayed.  If you use the <see cref="Triggers"/> property (or the <see cref="AddTrigger"/> method)
	/// to specify which controls should cause files to be uploaded, then other controls will not start the progress
	/// display (e.g. "Cancel" buttons).
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[DefaultProperty("Inline")]
	[ParseChildren(false)]
	[PersistChildren(true)]
	[ToolboxData("<{0}:ProgressBar runat='server'/>")]
	public class ProgressBar : ProgressBarBase
	{
		private bool InlineRequested
		{
			get { return (ViewState["inline"] != null && (bool)ViewState["inline"]); }
			set { ViewState["inline"] = value; }
		}
			
		/// <summary>
		/// Whether to display the progress bar inline or as a pop-up.  Under Opera, this property will always 
		/// return false, even if you set it to true.  Popup progress bars are automatically used under Opera 
		/// because Opera doesn't refresh the iframe used to display inline progress bars.</summary>
		[DefaultValue(false)]
		public bool Inline
		{
			get
			{
				// Return false when browser is Opera because Opera won't refresh the iframe until the upload completes.
				if (HttpContext.Current != null)
				{
					string userAgent = HttpContext.Current.Request.UserAgent;
					if (userAgent != null && userAgent.ToLower().IndexOf("opera") != -1)
						return false;
				}
				return InlineRequested;
			}
			set { InlineRequested = value; }
		}

		private HtmlTextWriterTag Tag = HtmlTextWriterTag.Div;		
		
		protected override void OnPreRender (EventArgs e)
		{
			base.OnPreRender(e);

			if (!InlineRequested && !Width.IsEmpty && (Width.Type != UnitType.Pixel || Width.Value < 500))
				throw new System.ArgumentOutOfRangeException("Width must be at least 500 pixels and must use pixel(px) units when using a popup ProgressBar.");
			if (!InlineRequested && !Height.IsEmpty && (Height.Type != UnitType.Pixel || Height.Value < 100))
				throw new System.ArgumentOutOfRangeException("Height must be at least 100 pixels and must use pixel(px) units when using a popup ProgressBar.");

			if (Inline)
			{
				Tag = HtmlTextWriterTag.Iframe;
				Attributes["src"] = UploadProgressPath + "&canScript=false&canCancel=false&postBackID=" + FormContext.Current.PostBackID;
				Attributes["frameborder"] = "0";
				Attributes["scrolling"] = "no";
				Attributes["name"] = this.ClientID;
			}
		}
		
			
		public override void RenderBeginTag(HtmlTextWriter writer)
		{
			if (!Inline)
			{
				base.RenderBeginTag(writer);
				return;
			}
			
			if (!Config.Current.UseHttpModule)
			{
				return;
			}
			// We can't register these scripts during PreRender because if we do there will be no way to
			// programmatically add triggers that are in data-bound controls that occur after the ProgressBar.
			RegisterScripts();
			this.Page.RegisterStartupScript("NeatUploadProgressBar-" + this.UniqueID, @"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadPB.prototype.Bars['" + this.ClientID + @"'].DisplayUrl = function(progressUrl) {
	var pb = this;
	setTimeout(function () { frames[pb.ClientID].location.href = progressUrl; }, 0);
};

(function() {
	var pb = NeatUploadPB.prototype.Bars['" + this.ClientID + @"'];
	if (frames[pb.ClientID]) 
		frames[pb.ClientID].location.replace(pb.UploadProgressPath + '&postBackID=' + pb.UploadForm.GetPostBackID() + '&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel());
})();
// -->
</script>");
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(Tag);
		}
		
		protected override void RenderContents(HtmlTextWriter writer)
		{
			// Don't render the no-iframe fallback if the browser supports frames because there have been reports
			// that the fallback is briefly visible in some browsers.
			if (Inline && Context != null && Context.Request != null && Context.Request.Browser != null && Context.Request.Browser.Frames)
			{
		    	return;
		    }
			EnsureChildControls();
			base.RenderContents(writer);
		}
		
		public override void RenderEndTag(HtmlTextWriter writer)
		{
			if (!Inline)
			{
				base.RenderEndTag(writer);
				return;
			}
			if (!Config.Current.UseHttpModule)
				return;
			
			writer.RenderEndTag();
		}
	}
}
