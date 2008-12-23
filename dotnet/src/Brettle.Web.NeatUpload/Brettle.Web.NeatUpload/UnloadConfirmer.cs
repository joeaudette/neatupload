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
using System.ComponentModel;
using System.Security.Permissions;
using Brettle.Web.NeatUpload.Internal.UI;
using Brettle.Web.NeatUpload.Internal;

namespace Brettle.Web.NeatUpload
{	
	/// <summary>
	/// Asks for confirmation if the user tries to unload the page while an upload is in progress.
	/// </summary>
	/// <remarks>
	/// If the user tries to follow a link in the same window/tab or re-submit the form, the current
    /// page would unload and the upload would be interrupted.  This control displays a confirmation
    /// dialog when that happens so that the user doesn't interrupt the upload accidentally.
	/// </remarks>
    [AspNetHostingPermissionAttribute(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermissionAttribute(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    [DefaultProperty("Text")]
    public class UnloadConfirmer : Control
	{
		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/

        internal bool IsDesignTime = (HttpContext.Current == null);

        protected override void OnPreRender(EventArgs e)
        {
            if (!Page.IsClientScriptBlockRegistered("NeatUploadJs"))
            {
                Page.RegisterClientScriptBlock("NeatUploadJs", @"
<script type='text/javascript' language='javascript' src='" 
                    + UploadModule.GetCacheBustedPath("/NeatUpload/NeatUpload.js")
                    + @"'></script>");
            }
            this.Page.RegisterStartupScript("NeatUploadUnloadConfirmer-" + this.UniqueID, GetStartupScript());
            base.OnPreRender(e);
        }

        protected virtual string GetStartupScript()
        {
            return String.Format(@"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadUnloadConfirmerCreate('{0}', '"
                        + FormContext.Current.PostBackID + @"','"
                        + this.Text + @"');
// -->
</script>", ClientID);
        }

        private string _Text = ResourceManagerSingleton.GetResourceString("UnloadConfirmation");
        /// <summary>
        /// The confirmation message to display if the user attempts to unload the page.
        /// </summary>
        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, ClientID);
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            if (IsDesignTime)
                writer.Write("<i>UnloadConfirmer</i>");
            writer.RenderEndTag();
        }
    }
}
