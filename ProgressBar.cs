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
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace Brettle.Web.NeatUpload
{
	public class ProgressBar : System.Web.UI.HtmlControls.HtmlGenericControl
	{
		private string uploadProgressUrl;
		private string displayStatement;
		
		public ProgressBar(string tagName)
		{
		}
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			if (!UploadHttpModule.IsEnabled)
				return;
			string appPath = Context.Request.ApplicationPath;
			if (appPath == "/")
			{
				appPath = "";
			}
			uploadProgressUrl = Attributes["src"];
			if (uploadProgressUrl == null)
				uploadProgressUrl = appPath + "/NeatUpload/Progress.aspx";

			uploadProgressUrl += "?postBackID=" + FormContext.Current.PostBackID;

			if (Attributes["class"] == null)
			{
				Attributes["class"] = "ProgressBar";
			}

			if (UploadContext.Current != null)
			{
				uploadProgressUrl += "&lastPostBackID=" + UploadContext.Current.PostBackID;
			}
			
			if (Attributes["inline"] == null || Attributes["inline"] == "false")
			{
				TagName = "div";
				displayStatement = @"
	window.open('" + uploadProgressUrl + "&refresher=client', '" + FormContext.Current.PostBackID + @"',
					'width=500,height=100,directories=no,location=no,menubar=no,resizable=yes,scrollbars=no,status=no,toolbar=no');
";
				this.Page.RegisterStartupScript(this.UniqueID + "RemoveDiv", @"
<script language=""javascript"">
<!--
NeatUpload_DivNode = document.getElementById('" + this.ClientID + @"'); 
if (NeatUpload_DivNode)
	NeatUpload_DivNode.parentNode.removeChild(NeatUpload_DivNode);
-->
</script>
");
			}
			else
			{
				TagName = "iframe";					
				Attributes["src"] = uploadProgressUrl;
				Attributes["frameborder"] = "0";
				Attributes["scrolling"] = "no";
				displayStatement = @"
		setTimeout(function() {
			document.getElementById('" + this.UniqueID + "').src='" + uploadProgressUrl + @"&refresher=client'; }, 0);
";
			}
		}
		
		protected override void Render(HtmlTextWriter writer)
		{
			if (!UploadHttpModule.IsEnabled)
				return;
			EnsureChildControls();
			base.RenderBeginTag(writer);
			writer.AddAttribute("href", uploadProgressUrl + "&refresher=server");
			writer.AddAttribute("target", FormContext.Current.PostBackID);
			writer.RenderBeginTag(HtmlTextWriterTag.A);
			if (!HasControls())
			{
				writer.Write("Check Upload Progress");
			}
			base.RenderChildren(writer);
			writer.RenderEndTag();
			base.RenderEndTag(writer);
		}
		
		public void AddTrigger(Control control)
		{
			if (!UploadHttpModule.IsEnabled)
				return;
			
			HtmlControl formControl = null;
			for (Control c = control; c != null; c=c.Parent)
			{
				formControl = c as HtmlControl;
				if (formControl != null && String.Compare(formControl.TagName, "FORM", true) == 0)
					break;
			}
			
			this.Page.RegisterStartupScript(this.UniqueID + "-AddHandler", @"
<script language=""javascript"">
<!--
NeatUpload_DisplayProgress_" + this.ClientID + @" = false;
NeatUpload_AddHandler('" + formControl.ClientID + @"', 'submit', function () { 
	if (NeatUpload_DisplayProgress_" + this.ClientID + @" == true)
	{
		NeatUpload_DisplayProgress_" + this.ClientID + @" = false;
		" + displayStatement + @"
	}
	return true;
});
-->
</script>
");
			if (!this.Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
			{
				this.Page.RegisterClientScriptBlock("NeatUploadProgressBar", clientScript);
			}
			this.Page.RegisterStartupScript(this.UniqueID + "-AddTrigger-" + control.UniqueID, @"
<script language=""javascript"">
<!--
NeatUpload_AddHandler('" + control.ClientID + @"', 'click', function () {
	NeatUpload_DisplayProgress_" + this.ClientID + @" = true;
});
-->
</script>
");			
		}

		
		private string clientScript = @"
<script language=""javascript"">
<!--
NeatUpload_DisplayProgress = false;
function NeatUpload_AddHandler(id, eventName, handler)
{
	var elem = document.getElementById(id);
	if (elem.addEventListener)
	{
		elem.addEventListener(eventName, handler, false);
	}
	else if (elem.attachEvent)
	{
		elem.attachEvent(""on"" + eventName, handler);
	}
	else
	{
		var origHandler = elem[""on"" + eventName];
		if (origHandler)
		{
			var h = new object();
			h.origHandler = origHandler;
			h.newHandler = handler;
			h.both = new function(e) { this.origHandler(e); this.handler(e); };
			elem[""on"" + eventName] = h.both;
		}
		else
		{
			elem[""on"" + eventName] = handler;
		}
	}
}
-->
</script>
";
	}
}
