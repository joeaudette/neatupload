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
	/// A &lt;span&gt; to be dynamically filled with upload progress/status information.</summary>
	/// <remarks>
	/// <para>
	/// Each <c>{<i>variableName[</i>:<i>resourceName]</i>}</c> in this control's literal content or
	/// attributes is replaced with the value of <i>variableName</i> formatted according to a resource located using
	/// <i>resourceName</i>.  For certain variables, the resource name that is used is constructed by
	/// appending ".0", ".1", or ".2" to the end of the <i>resourceName</i> depending on the
	/// magnituded of the value being formatted.  This allows 30000000 to be formatted as "30 MB" while 30000 is
	/// formatted as "30 KB".  Here is a table of the available variables:"/>.
	/// </para>
	/// <list type="table">
	/// <listheader><term>variableName    </term>  <term>Meaning               </term> <term>Formatted via                                  </term> <description>Notes</description></listheader>
	///	
	/// <item>      <term>BytesTotal      </term>  <term>upload size           </term> <term><c>String.Format(<i>format</i>, BytesTotal)</c></term> <description>
	///                                                                                                                             <list type="table">
	///                                                                                                                             <listheader><term>BytesTotal </term> <description>Resource containing <i>format</i></description></listheader>
	///                                                                                                                             <item>      <term>0-999      </term> <description><i>resourceName</i>.0</description></item>
	///                                                                                                                             <item>      <term>1000-999999</term> <description><i>resourceName</i>.1</description></item>
	///                                                                                                                             <item>      <term>1000000-   </term> <description><i>resourceName</i>.2</description></item>
	///                                                                                                                             </list></description></item>
	/// <item>      <term>BytesRead       </term>  <term>bytes uploaded so far </term> <term><c>String.Format(<i>format</i>, BytesRead)</c> </term> <description>
	///                                                                                                                             <list type="table">
	///                                                                                                                             <listheader><term>BytesTotal </term> <description>Resource containing <i>format</i></description></listheader>
	///                                                                                                                             <item>      <term>0-999      </term> <description><i>resourceName</i>.0</description></item>
	///                                                                                                                             <item>      <term>1000-999999</term> <description><i>resourceName</i>.1</description></item>
	///                                                                                                                             <item>      <term>1000000-   </term> <description><i>resourceName</i>.2</description></item>
	///                                                                                                                             </list></description></item>
	/// <item>      <term>FractionComplete</term>  <term>BytesRead / BytesTotal</term> <term><c>String.Format(<i>format</i>, FractionComplete)</c></term><description>FractionComplete is in the range 0.0-1.0</description></item>
	/// <item>      <term>BytesPerSec     </term>  <term>upload rate in bytes/s</term> <term><c>String.Format(<i>format</i>, BytesPerSec)</c></term> <description>
	///                                                                                                                             <list type="table">
	///                                                                                                                             <listheader><term>BytesPerSec</term> <description>Resource containing <i>format</i></description></listheader>
	///                                                                                                                             <item>      <term>0-999      </term> <description><i>resourceName</i>.0</description></item>
	///                                                                                                                             <item>      <term>1000-999999</term> <description><i>resourceName</i>.1</description></item>
	///                                                                                                                             <item>      <term>1000000-   </term> <description><i>resourceName</i>.2</description></item>
	///                                                                                                                             </list></description></item>
	/// <item>      <term>TimeRemaining   </term>  <term>TimeSpan until done   </term> <term><c>String.Format(<i>format</i>,
	///                                                                                     (int)Math.Floor(TimeRemaining.TotalHours),
	///	                                                                                    (int)Math.Floor(TimeRemaining.TotalMinutes),
	///	                                                                                    TimeRemaining.Seconds,
	///	                                                                                    TimeRemaining.TotalHours,
	///	                                                                                    TimeRemaining.TotalMinutes)</c></term> <description>
	///                                                                                                                             <list type="table">
	///                                                                                                                             <listheader><term>TimeRemaining</term> <description>Resource containing <i>format</i></description></listheader>
	///                                                                                                                             <item>      <term>0-60 seconds </term> <description><i>resourceName</i>.0</description></item>
	///                                                                                                                             <item>      <term>1-60 minutes </term> <description><i>resourceName</i>.1</description></item>
	///                                                                                                                             <item>      <term>1- hours     </term> <description><i>resourceName</i>.2</description></item>
	///                                                                                                                             </list></description></item>
	/// <item>      <term>TimeElapsed     </term>  <term>TimeSpan since start  </term> <term><c>String.Format(<i>format</i>,
	///                                                                                     (int)Math.Floor(TimeElapsed.TotalHours),
	///	                                                                                    (int)Math.Floor(TimeElapsed.TotalMinutes),
	///	                                                                                    TimeElapsed.Seconds,
	///	                                                                                    TimeElapsed.TotalHours,
	///	                                                                                    TimeElapsed.TotalMinutes)</c></term> <description>
	///                                                                                                                             <list type="table">
	///                                                                                                                             <listheader><term>TimeElapsed</term> <description>Resource containing <i>format</i></description></listheader>
	///                                                                                                                             <item>      <term>0-60 seconds </term> <description><i>resourceName</i>.0</description></item>
	///                                                                                                                             <item>      <term>1-60 minutes </term> <description><i>resourceName</i>.1</description></item>
	///                                                                                                                             <item>      <term>1- hours     </term> <description><i>resourceName</i>.2</description></item>
	///                                                                                                                             </list></description></item>
	/// <item>      <term>Rejection       </term>  <term>reason for rejection  </term> <term><c>String.Format(<i>format</i>,
    ///                                                                                     Rejection.Message,
    ///                                                                                     Rejection.GetType(),
    ///                                                                                     Rejection.StackTrace,
    ///                                                                                     Rejection.HelpLink)</c></term> <description>Rejection is an <see cref="UploadException"/></description></item>
	/// <item>      <term>Error           </term>  <term>reason for error      </term> <term><c>String.Format(<i>format</i>,
    ///                                                                                     Error.Message,
    ///                                                                                     Error.GetType(),
    ///                                                                                     Error.StackTrace,
    ///                                                                                     Error.HelpLink)</c></term> <description>ErrorMessage is an <see cref="Exception"/></description></item>
	/// </list>
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[ParseChildren(false)]
	[PersistChildren(true)]
	public class SpanTemplate : System.Web.UI.WebControls.WebControl
	{
		private bool IsDesignTime = (HttpContext.Current == null);

		private	HtmlTextWriterTag Tag;
		private UploadView View;
		private Progress ProgressDisplay;

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			Load += new EventHandler(Control_Load);
		}
		
		private void Control_Load(object sender, EventArgs args)
		{
			ProgressDisplay = Page as Progress;
			if (ProgressDisplay == null)
				throw new ApplicationException(this.TagName + " can only be used within a Progress page.");
			
			ProcessControl(this);
		}
		
		private void ProcessControl(Control control)
		{
			control.EnsureChildControls();
			if (control is HtmlControl)
			{
				ProcessAttributes(control.ClientID, ((HtmlControl)control).Attributes);
			}
			else if (control is WebControl)
			{
				ProcessAttributes(control.ClientID, ((WebControl)control).Attributes);
			}
			if (control is LiteralControl)
			{
				ProcessLiteralControl((LiteralControl)control);
			}
			else if (control.HasControls)
			{
				foreach (Control c in control.Controls)
				{
					ProcessControl(c);
				}
			}
		}
		
		private void ProcessAttributes(string clientID, AttributeCollection attrs)
		{
			foreach (string attrName in attrs)
			{
				string attrValueTemplate = attrs[attrName];
				if (View.ReplaceInAttribute(ref attrs[attrName], new GetResourceCallBack(GetResource)))
				{
					View.AddAttributeReplacement(clientID, attrName, attrValueTemplate);
				}
			}
		}
		
		private string GetResource(string resourceName)
		{
			string resourceValue = ProgressDisplay.GetResource(resourceName);
			if (resourceValue == null)
			{
				throw new System.Resources.MissingManifestResourceException(resourceName);
			}
			View.Resources[resourceName] = resourceValue;
			return resourceValue;
		}
				
			
		
		protected override void OnPreRender (EventArgs e)
		{
			if (IsDesignTime || !Config.Current.UseHttpModule)
				return;
			
			HtmlControl formControl = GetFormControl(this);
			
			if (!Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
			{
				Page.RegisterClientScriptBlock("NeatUploadProgressBar", clientScript);
			}
			
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

			StringBuilder scriptBuilder = new StringBuilder();
			scriptBuilder.Append(@"
<script language=""javascript"">
<!--
");
			AddPerProgressBarScripts(scriptBuilder, formControl);
			
			ArrayList nonUploadButtonIDs = new ArrayList(); // IDs of buttons refed by NonUploadButtons property
			if (NonUploadButtons != null)
			{
				nonUploadButtonIDs.AddRange(NonUploadButtons.Split(' '));
			}
			foreach (string buttonID in nonUploadButtonIDs)
			{
				Control c = NamingContainer.FindControl(buttonID);
				if (c == null)
					continue;
				AddNonUploadButtonScripts(scriptBuilder, c);
			}
			foreach (Control c in otherNonUploadButtons)
			{
				AddNonUploadButtonScripts(scriptBuilder, c);
			}
			
			ArrayList triggerIDs = new ArrayList(); // IDs of controls refed by Triggers property
			if (Triggers != null)
			{
				triggerIDs.AddRange(Triggers.Split(' '));
			}
			foreach (string buttonID in triggerIDs)
			{
				Control c = NamingContainer.FindControl(buttonID);
				if (c == null)
					continue;
				AddTriggerScripts(scriptBuilder, c);
			}
			foreach (Control c in otherTriggers)
			{
				AddTriggerScripts(scriptBuilder, c);
			}
			
			scriptBuilder.Append(@"
// -->
</script>
");
			Page.RegisterStartupScript(this.UniqueID, scriptBuilder.ToString());
		}

		protected override void Render(HtmlTextWriter writer)
		{
			if (IsDesignTime)
			{
				Tag = HtmlTextWriterTag.Div;
			}
			else if (!Config.Current.UseHttpModule)
				return;
			EnsureChildControls();
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(Tag);
			if (IsDesignTime)
			{
				if (Inline)
				{
					writer.Write("<i>Inline ProgressBar - no-IFRAME fallback = {</i>");
				}
				else
				{
					writer.Write("<i>Pop-up ProgressBar - no-Javascript fallback = {</i>");
				}
			}
			writer.AddAttribute("href", uploadProgressUrl + "&refresher=server");
			string target = IsDesignTime ? "_blank" : FormContext.Current.PostBackID;
			writer.AddAttribute("target", target);
			writer.RenderBeginTag(HtmlTextWriterTag.A);
			if (!HasControls())
			{
				writer.Write("Check Upload Progress");
			}
			base.RenderChildren(writer);
			writer.RenderEndTag();
			if (IsDesignTime)
			{
				writer.Write("<i>}</i>");
			}
			writer.RenderEndTag();
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

		private void AddPerProgressBarScripts(StringBuilder scriptBuilder, Control formControl)
		{
			scriptBuilder.Append(@"
NeatUpload_NonUploadIDs_" + this.ClientID + @" = new Object();
NeatUpload_NonUploadIDs_" + this.ClientID + @".NeatUpload_length = 0;
NeatUpload_TriggerIDs_" + this.ClientID + @" = new Object();
NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length = 0;
NeatUpload_LastEventSourceId = null;
NeatUpload_LastEventType = null;

NeatUpload_AddSubmitHandler('" + formControl.ClientID + "'," + (Inline ? "false" : "true") + @", function () {
	if (NeatUpload_IsFilesToUpload('" + formControl.ClientID + @"'))
	{
		" + displayStatement + @"
	}
});
						
NeatUpload_EventsThatCouldTriggerPostBack = ['click', 'keypress', 'change', 'drop', 'mousedown', 'keydown'];
						
for (var i = 0; i < NeatUpload_EventsThatCouldTriggerPostBack.length; i++)
{
	var eventName = NeatUpload_EventsThatCouldTriggerPostBack[i];
	NeatUpload_AddHandler('" + formControl.ClientID + @"', eventName, function (ev) {
		ev = ev || window.event;
		if (!ev)
		{
			return true;
		}
		var src = ev.srcElement || ev.target;
		if (!src)
		{
			return true;
		}
		NeatUpload_LastEventType = ev.type;
		NeatUpload_LastEventSourceId = src.id;
		if (ev.type != 'click' && ev.type != 'keypress')
		{
			return true;
		}
		if (NeatUpload_TriggerIDs_" + this.ClientID + @"[src.id]
		      || NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length == 0)
		{
			return true;
		}
		var tagName = src.tagName;
		if (!tagName)
		{
			return true;
		}
		tagName = tagName.toLowerCase();
		if (tagName == 'input' || tagName == 'button')
		{
			var inputType = src.getAttribute('type');
			if (inputType) inputType = inputType.toLowerCase();
			if (!inputType || inputType == 'submit' || inputType == 'image')
			{
				var formElem = document.getElementById('" + formControl.ClientID + @"');
				NeatUpload_ClearFileInputs(formElem);
			}
		}
		return true;
	}, true);
}

NeatUpload_AddSubmittingHandler('" + formControl.ClientID + @"', function () {
	if (!NeatUpload_LastEventSourceId)
	{
		return;
	}
	if (NeatUpload_TriggerIDs_" + this.ClientID + @"[NeatUpload_LastEventSourceId])
	{
		return;
	}
	var formElem = document.getElementById('" + formControl.ClientID + @"');
	if (NeatUpload_NonUploadIDs_" + this.ClientID + @"[NeatUpload_LastEventSourceId]
	     || NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length)
	{
		NeatUpload_ClearFileInputs(formElem);
	}
});

");
		}

		
		private void AddNonUploadButtonScripts(StringBuilder scriptBuilder, Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			scriptBuilder.Append(@"NeatUpload_NonUploadIDs_" + this.ClientID + @"['" + control.ClientID + @"'] 
	= ++NeatUpload_NonUploadIDs_" + this.ClientID + @".NeatUpload_length;
");			
		}

		private void AddTriggerScripts(StringBuilder scriptBuilder, Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			scriptBuilder.Append(@"NeatUpload_TriggerIDs_" + this.ClientID + @"['" + control.ClientID + @"'] 
	= ++NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length;
");			
		}

		private string clientScript = @"
<script language=""javascript"">
<!--
function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { if (origHandler(e) == false) return false; return newHandler(e); };
};
function NeatUpload_AddHandler(id, eventName, handler, useCapture)
{
	if (typeof(useCapture) == 'undefined')
		useCapture = false;
	var elem = document.getElementById(id);
	if (elem.addEventListener)
	{
		elem.addEventListener(eventName, handler, useCapture);
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
			elem.NeatUpload_OnSubmitting();
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

function NeatUpload_AddSubmittingHandler(formID, handler)
{
	var elem = document.getElementById(formID);
	if (!elem.NeatUpload_OnSubmittingHandlers) 
	{
		elem.NeatUpload_OnSubmittingHandlers = new Array();
		elem.NeatUpload_OnSubmitting = NeatUpload_OnSubmitting;
	}
	elem.NeatUpload_OnSubmittingHandlers.push(handler);
}

function NeatUpload_OnSubmitting()
{
	for (var i=0; i < this.NeatUpload_OnSubmittingHandlers.length; i++)
	{
		this.NeatUpload_OnSubmittingHandlers[i].call(this);
	}
	return true;
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
