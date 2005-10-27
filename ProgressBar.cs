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
using System.Collections;
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
		private string displayProgressByDefault = "true";
		private ArrayList nonUploadButtonIDs = new ArrayList(); // IDs of buttons refed by NonUploadButtons attr
		private ArrayList nonUploadButtons = new ArrayList(); // Controls passed to AddNonUploadButton()
		private ArrayList triggers = new ArrayList(); // Controls passed to AddTrigger()

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
			if (!Config.Current.UseHttpModule)
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
			string nonUploadButtonsString = Attributes["NonUploadButtons"];
			if (nonUploadButtonsString != null)
			{
				nonUploadButtonIDs.AddRange(nonUploadButtonsString.Split(' '));
			}
		}
		
		[Obsolete("This method is obsolete and will be removed in a future version.  Instead, call AddNonUploadButton()"
					+ " with the buttons which are *not* triggers.")] 
		public void AddTrigger(Control control)
		{
			triggers.Add(control);
		}

		public void AddNonUploadButton(Control control)
		{
			nonUploadButtons.Add(control);
		}

		protected override void OnPreRender (EventArgs e)
		{
			if (!Config.Current.UseHttpModule)
				return;

			if (nonUploadButtonIDs.Count + nonUploadButtons.Count > 0)
			{
				displayProgressByDefault = "true";
				foreach (string buttonID in nonUploadButtonIDs)
				{
					Control c = NamingContainer.FindControl(buttonID);
					if (c == null)
						continue;
					RegisterNonUploadButtonScripts(c);
				}
				foreach (Control c in nonUploadButtons)
				{
					RegisterNonUploadButtonScripts(c);
				}
			}
			else
			{
				// Triggers are deprecated.
				foreach (Control c in triggers)
				{
					displayProgressByDefault = "false";
					RegisterTriggerScripts(c);
				}
			}
			HtmlControl formControl = GetFormControl(this);
			RegisterScriptsForForm(formControl);
		}

		protected override void Render(HtmlTextWriter writer)
		{
			if (!Config.Current.UseHttpModule)
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
		
		private void RegisterNonUploadButtonScripts(Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			HtmlControl formControl = GetFormControl(control);
			
			RegisterScriptsForForm(formControl);
			this.Page.RegisterStartupScript(this.UniqueID + "-AddNonUploadButton-" + control.UniqueID, @"
<script language=""javascript"">
<!--
NeatUpload_AddHandler('" + control.ClientID + @"', 'click', function () {
	var formElem = document.getElementById('" + formControl.ClientID + @"');
	NeatUpload_ClearFileInputs(formElem);
});
-->
</script>
");			
		}

		private void RegisterTriggerScripts(Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			HtmlControl formControl = GetFormControl(control);
			
			RegisterScriptsForForm(formControl);

			this.Page.RegisterStartupScript(this.UniqueID + "-AddTrigger-" + control.UniqueID, @"
<script language=""javascript"">
<!--
NeatUpload_AddHandler('" + control.ClientID + @"', 'click', function () {
	if (NeatUpload_IsFilesToUpload('" + formControl.ClientID + @"'))
	{
		NeatUpload_DisplayProgress_" + this.ClientID + @" = true;
		NeatUpload_DisplayProgressSet_" + this.ClientID + @" = true;
	}
});
-->
</script>
");			
		}

		private HtmlControl GetFormControl(Control control)
		{
			HtmlControl formControl = null;
			for (Control c = control; c != null; c=c.Parent)
			{
				formControl = c as HtmlControl;
				if (formControl != null && String.Compare(formControl.TagName, "FORM", true) == 0)
					break;
			}
			return formControl;
		}

		private void RegisterScriptsForForm(Control formControl)
		{
			this.Page.RegisterStartupScript(formControl.UniqueID + "-OnSubmit", @"
<script language=""javascript"">
<!--
function NeatUpload_OnSubmitForm_" + formControl.ClientID + @"()
{
	var elem = document.getElementById('" + formControl.ClientID + @"');
	elem.NeatUpload_OnSubmit();
}

document.getElementById('" + formControl.ClientID + @"').onsubmit 
	= NeatUpload_CombineHandlers(document.getElementById('" + formControl.ClientID + @"').onsubmit, NeatUpload_OnSubmitForm_" + formControl.ClientID + @");
-->
</script>
");

			this.Page.RegisterStartupScript(this.UniqueID + "-AddHandler", @"
<script language=""javascript"">
<!--
function NeatUpload_InitDisplayProgress_" + this.ClientID + @"()
{
	if (!NeatUpload_DisplayProgressSet_" + this.ClientID + @")
	{
		NeatUpload_DisplayProgress_" + this.ClientID + @" = " + displayProgressByDefault + @";
	}
	NeatUpload_DisplayProgressSet_" + this.ClientID + @" = false;
}
var NeatUpload_DisplayProgressSet_" + this.ClientID + @" = false;
NeatUpload_InitDisplayProgress_" + this.ClientID + @"();
NeatUpload_AddSubmitHandler('" + formControl.ClientID + "'," + (isPopup ? "true" : "false") + @", function () {
		if (NeatUpload_DisplayProgress_" + this.ClientID + @" == true 
			&& NeatUpload_IsFilesToUpload('" + formControl.ClientID + @"'))
		{
			NeatUpload_DisplayProgress_" + this.ClientID + @" = " + displayProgressByDefault + @";
			" + displayStatement + @"
		}
});

NeatUpload_AddHandler('" + formControl.ClientID + @"', 'click', NeatUpload_InitDisplayProgress_" + this.ClientID + @");
-->
</script>
");
			if (!this.Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
			{
				this.Page.RegisterClientScriptBlock("NeatUploadProgressBar", clientScript);
			}
		}

		
		private string clientScript = @"
<script language=""javascript"">
<!--
function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { if (origHandler(e) == false) return false; return newHandler(e); };
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
	var isFilesToUpload = false;
	for (i = 0; i < inputElems.length; i++)
	{
		var inputElem = inputElems.item(i);
		if (inputElem && inputElem.type && inputElem.type.toLowerCase() == ""file"")
		{
			foundFileInput = true;
			if (inputElem.value && inputElem.value.length > 0)
			{
				isFilesToUpload = true;

				// If the browser really is IE on Windows, return false if the path is not absolute because
				// IE will not actually submit the form if any file value is not an absolute path.  If IE doesn't
				// submit the form, any progress bars we start will never finish.  
				if (navigator && navigator.userAgent
					&& navigator.userAgent.toLowerCase().indexOf('msie') != -1 && typeof(ActiveXObject) != 'undefined') 
				{
					var re = new RegExp('^(\\\\\\\\[^\\\\]|([a-zA-Z]:)?\\\\).*');
					var match = re.exec(inputElem.value);
					if (match == null || match[0] == '')
						return false;
				}
			}
		}
	}
	return isFilesToUpload; 
}

function NeatUpload_ClearFileInputs(elem)
{
	var inputFiles = elem.getElementsByTagName('input');
	for (var i=0; i < inputFiles.length; i++ )
	{
		var inputFile = inputFiles.item(i);
		if (inputFile.type == 'file')
		{
			var newInputFile = document.createElement('input');
			for (var a=0; a < inputFile.attributes.length; a++)
			{
				var attr = inputFile.attributes.item(a); 
				if (attr.specified && attr.name != 'type' && attr.name != 'value')
					newInputFile.setAttribute(attr.name, attr.value);
			}
			newInputFile.setAttribute('type', 'file');
			inputFile.parentNode.replaceChild(newInputFile, inputFile);
		}
	}
}

function NeatUpload_AddSubmitHandler(formID, isPopup, handler)
{
	var elem = document.getElementById(formID);
	if (!elem.NeatUpload_OnSubmitHandlers) 
	{
		elem.NeatUpload_OnSubmitHandlers = new Array();
		elem.NeatUpload_OrigSubmit = elem.submit;
		elem.NeatUpload_OnSubmit = NeatUpload_OnSubmit;
		elem.submit = function () {
			elem.NeatUpload_OrigSubmit();
			elem.NeatUpload_OnSubmit();
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

function NeatUpload_OnSubmit()
{
	for (var i=0; i < this.NeatUpload_OnSubmitHandlers.length; i++)
	{
		this.NeatUpload_OnSubmitHandlers[i].call(this);
	}
	return true;
}
-->
</script>
";
	}
}
