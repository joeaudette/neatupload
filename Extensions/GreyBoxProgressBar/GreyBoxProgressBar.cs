/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005-2008  Dean Brettle

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
	/// Displays progress and status of an upload using a GreyBox pop-up window.</summary>
	/// <remarks>
	/// For the progress bar to be displayed, the <see cref="UploadHttpModule"/> must be in use.
	/// For the progress display to be started, the form being submitted must include an <see cref="InputFile"/>
	/// control that is not empty.  If you use the <see cref="Triggers"/> property (or the <see cref="AddTrigger"/> method)
	/// to specify which controls should cause files to be uploaded, then other controls will not start the progress
	/// display (e.g. "Cancel" buttons).
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[ParseChildren(false)]
	[PersistChildren(true)]
	[ToolboxData("<{0}:GreyBoxProgressBar runat='server'/>")]
	public class GreyBoxProgressBar : ProgressBarBase
	{
		/// <summary>
		/// URL of the directory containing the GreyBox support files.</summary>
		/// <remarks>
		/// The URL should not include a the trailing "/".
		/// You may use an absolute or relative URL that refers to a page in the same web application.  If the URL
		/// starts with "~", the "~" will be replaced with the web application root as returned by
		/// <see cref="HttpRequest.ApplicationPath"/>.  By default, "~/greybox" will be used.</remarks>
		[Editor(typeof(UrlEditor), typeof(UITypeEditor)), Bindable(true), DefaultValue("~/greybox")]
		public string GreyBoxRoot
		{
			get { return (string)ViewState["GreyBoxRoot"]; }
			set { ViewState["GreyBoxRoot"] = value; }
		}

		protected override void OnPreRender (EventArgs e)
		{
			if (!Width.IsEmpty && Width.Type != UnitType.Pixel)
				throw new System.ArgumentOutOfRangeException("Width must use pixel(px) units when using a GreyBoxProgressBar.");
			if (!Height.IsEmpty && Height.Type != UnitType.Pixel)
				throw new System.ArgumentOutOfRangeException("Height must use pixel(px) units when using a GreyBoxProgressBar.");
			base.OnPreRender(e);
			if (!Config.Current.UseHttpModule)
				return;
			string expandedGreyBoxRoot = GreyBoxRoot;
			if (expandedGreyBoxRoot == null)
			{
				expandedGreyBoxRoot = "~/greybox";
			}
			expandedGreyBoxRoot = HttpContext.Current.Response.ApplyAppPathModifier(expandedGreyBoxRoot);

			if (!Page.IsClientScriptBlockRegistered("GreyBoxJs"))
			{
				Page.RegisterClientScriptBlock("GreyBoxJs", String.Format(@"
<script type='text/javascript' language='javascript'>
    var GB_ROOT_DIR = '{0}/';
</script>
<script type='text/javascript' language='javascript' src='{0}/AJS.js?guid={1}'></script>
<script type='text/javascript' language='javascript' src='{0}/AJS_fx.js?guid={1}'></script>
<script type='text/javascript' language='javascript' src='{0}/gb_scripts.js?guid={1}'></script>
<link href='{0}/gb_styles.css' rel='stylesheet' type='text/css' />
", expandedGreyBoxRoot, CacheBustingGuid));
			}
		}
		
		protected override string GetStartupScript()
		{
			return base.GetStartupScript() + String.Format(@"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadPB.prototype.Bars['{0}'].DisplayUrl = function(progressUrl) {{
	var pb = this;
	GB_showCenter('', progressUrl, {1}, {2});
}};
NeatUploadPB.prototype.Bars['{0}'].EvalOnClose = ""NeatUploadMainWindow.GB_hide();"";
// -->
</script>", ClientID, (Height.IsEmpty ? 100 : Height.Value), (Width.IsEmpty ? 500 : Width.Value));
		}
			

		// This is used to ensure that the browser gets the latest GreyBox files each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when those files change,
		// but computing a hash on those files everytime this assembly is loaded strikes me as overkill.
		private static Guid CacheBustingGuid = System.Guid.NewGuid();
		
	}
}
