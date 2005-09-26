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
		private bool isPopup {
			get { return Attributes["inline"] == null || Attributes["inline"] == "false"; }
		}
		
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
			
			if (isPopup)
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
				Attributes["name"] = this.ClientID;
				displayStatement = @"
setTimeout(function () {
	frames['" + this.ClientID + @"'].location.href = '" + uploadProgressUrl + @"&refresher=client';
}, 0);
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
			
			this.Page.RegisterStartupScript(formControl.UniqueID + "-OnSubmit", @"
<script language=""javascript"">
<!--
function NeatUpload_OnSubmitForm_" + formControl.ClientID + @"()
{
	var elem = document.getElementById('" + formControl.ClientID + @"');
	for (var i=0; i < elem.NeatUpload_OnSubmitHandlers.length; i++)
	{
		elem.NeatUpload_OnSubmitHandlers[i].call(elem);
	}
	return true;
}

function NeatUpload_AddSubmitHandler_" + formControl.ClientID + @"(isPopup, handler)
{
	var elem = document.getElementById('" + formControl.ClientID + @"');
	if (!elem.NeatUpload_OnSubmitHandlers) 
	{
		elem.NeatUpload_OnSubmitHandlers = new Array();
		elem.NeatUpload_OrigSubmit = elem.submit;
		elem.submit = function () {
			elem.NeatUpload_OrigSubmit();
			NeatUpload_OnSubmitForm_" + formControl.ClientID + @"();
		};
	}
	if (isPopup)
	{
		elem.NeatUpload_OnSubmitHandlers.unshift(handler);
	}
	else
	{
		elem.NeatUpload_OnSubmitHandlers.push(handler);
	}	
}
NeatUpload_AddHandler('" + formControl.ClientID + @"', 'submit', NeatUpload_OnSubmitForm_" + formControl.ClientID + @");
-->
</script>
");

			this.Page.RegisterStartupScript(this.UniqueID + "-AddHandler", @"
<script language=""javascript"">
<!--

NeatUpload_DisplayProgress_" + this.ClientID + @" = false;
NeatUpload_AddSubmitHandler_" + formControl.ClientID + "(" + (isPopup ? "true" : "false") + @", function () {
		if (NeatUpload_DisplayProgress_" + this.ClientID + @" == true)
		{
			NeatUpload_DisplayProgress_" + this.ClientID + @" = false;
			" + displayStatement + @"
		}
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
	if (NeatUpload_IsFilesToUpload('" + formControl.ClientID + @"'))
	{
		NeatUpload_DisplayProgress_" + this.ClientID + @" = true;
	}
});
-->
</script>
");			
		}

		
		private string clientScript = @"
<script language=""javascript"">
<!--
NeatUpload_DisplayProgress = false;
function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { origHandler(e); newHandler(e); };
};
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
		elem[""on"" + eventName] = NeatUpload_CombineHandlers(elem[""on"" + eventName], handler);
	}
}
function NeatUpload_IsFilesToUpload(id)
{
	var formElem = document.getElementById(id);
	while (formElem && formElem.tagName.toLowerCase() != ""form"")
	{
		formElem = formElem.parent;
	}
	if (!formElem) 
	{
		return false;
	}
	var inputElems = formElem.getElementsByTagName(""input"");
	var foundFileInput = false;
	for (i = 0; i < inputElems.length; i++)
	{
		var inputElem = inputElems.item(i);
		if (inputElem && inputElem.type && inputElem.type.toLowerCase() == ""file"")
		{
			foundFileInput = true;
			if (inputElem.value && inputElem.value.length > 0)
				return true;
		}
	}
	return false; 
}
-->
</script>
";
	}
}
