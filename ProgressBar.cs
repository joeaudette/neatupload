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

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Displays progress and status of an upload.</summary>
	/// <remarks>
	/// For the progress bar to be displayed, the <see cref="UploadHttpModule"/> must be in use.
	/// For the progress display to be started, the form being submitted must include an <see cref="InputFile"/>
	/// control that is not empty.  Use the <see cref="Inline"/> property to control how the progress bar is
	/// displayed.  Use the <see cref="NonUploadButtons"/> property (or the <see cref="AddNonUploadButton"/> method)
	/// to specify any buttons which should not cause files to be uploaded and should not start the progress
	/// display (e.g. "Cancel" buttons).
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[DefaultProperty("Inline")]
	[ParseChildren(false)]
	[PersistChildren(true)]
	public class ProgressBar : System.Web.UI.WebControls.WebControl
	{
		private bool IsDesignTime = (HttpContext.Current == null);

		private string uploadProgressUrl;
		private string displayStatement;
		private ArrayList otherNonUploadButtons = new ArrayList(); // Controls passed to AddNonUploadButton()
		private ArrayList otherTriggers = new ArrayList(); // Controls passed to AddTrigger()
		private	HtmlTextWriterTag Tag;

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
				if (!IsDesignTime)
				{
					string userAgent = HttpContext.Current.Request.UserAgent;
					if (userAgent != null && userAgent.ToLower().IndexOf("opera") != -1)
						return false;
				}
				return InlineRequested;
			}
			set { InlineRequested = value; }
		}
		
		/// <summary>
		/// Space-separated list of the IDs of controls which should not upload files and start the progress 
		/// display. </summary>
		/// <remarks>
		/// When a user clicks on a non-upload control, Javascript clears all <see cref="InputFile" /> controls. 
		/// As a result, the progress display does not start and no files are uploaded when the form is submitted.
		/// If no triggers are listed in <see cref="Triggers"/> or added via <see cref="AddTrigger"/> then any control
		/// other than those listed in <see cref="NonUploadButtons"/> or added via <see cref="AddNonUploadButton"/>
		/// will be considered a trigger and will upload files and start the progress display.  If you do specify
		/// one or more triggers, then all links and submit buttons <i>other</i> than those triggers will be considered
		/// non-upload controls (in addition to any controls listed in <see cref="NonUploadButtons"/> or added via
		/// <see cref="AddNonUploadButton"/>).  This means that in most cases you can simply specify one or more
		/// triggers and not worry about specifying non-upload controls unless you have controls other than links and
		/// submit buttons that cause the form to submit.</remarks>  
		private string NonUploadButtons
		{
			get { return (string)ViewState["NonUploadButtons"]; }
			set { ViewState["NonUploadButtons"] = value; }
		}

		/// <summary>
		/// Space-separated list of the IDs of controls which should upload files and start the progress 
		/// display. </summary>
		/// <remarks>
		/// If no triggers are listed in <see cref="Triggers"/> or added via <see cref="AddTrigger"/> then whenever
		/// the form is submitted with any files selected the progress display will start.  If you do specify
		/// one or more triggers, then any form submissions initiated via any other controls that you didn't specify
		/// as triggers will <i>not</i> include any files and will <i>not</i> start the progress display.</remarks>  
		public string Triggers
		{
			get { return (string)ViewState["Triggers"]; }
			set { ViewState["Triggers"] = value; }
		}
		
		/// <summary>
		/// URL of an aspx page that displays the upload progress.</summary>
		/// <remarks>
		/// The specified page should inherits from the <see cref="Brettle.Web.NeatUpload.Progress"/> code behind class.
		/// You may use an absolute or relative URL that refers to a page in the same web application.  If the URL
		/// starts with "~", the "~" will be replaced with the web application root as returned by
		/// <see cref="HttpRequest.ApplicationPath"/>.  By default, "~/NeatUpload/Progress.aspx" will be used.</remarks>
		public string Url
		{
			get { return (string)ViewState["Url"]; }
			set { ViewState["Url"] = value; }
		}
				
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}

        protected override void OnPreRender(EventArgs e)
		{
			if (!IsDesignTime && Config.Current.UseHttpModule)
			{
				InitializeVars();
				if (!Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
				{
					Page.RegisterClientScriptBlock("NeatUploadProgressBar", clientScript);
				}
            }
			base.OnPreRender(e);
		}

		protected override object SaveViewState()
		{
			// We register the on submit statement here in hopes that it will be the last on submit statement.
			// Other on submit statements will generally be added during PreRender.
			HtmlControl formControl = GetFormControl(this);
			this.Page.RegisterOnSubmitStatement(formControl.UniqueID + "-OnSubmitStatement", "NeatUpload_OnSubmitForm_" + formControl.ClientID + @"();");
			return base.SaveViewState();
		}
		
		private void InitializeComponent()
		{
		}
		
		private void InitializeVars()
		{
			if (IsDesignTime || !Config.Current.UseHttpModule)
				return;
			string appPath = Context.Request.ApplicationPath;
			if (appPath == "/")
			{
				appPath = "";
			}
			uploadProgressUrl = Url;
			if (uploadProgressUrl == null)
			{
				uploadProgressUrl = appPath + "/NeatUpload/Progress.aspx";
			}
			else if (uploadProgressUrl.StartsWith("~"))
			{
				uploadProgressUrl = appPath + uploadProgressUrl.Substring(1);
			}
			
			uploadProgressUrl += "?postBackID=" + FormContext.Current.PostBackID;

			if (Attributes["class"] == null)
			{
				Attributes["class"] = "ProgressBar";
			}

			if (UploadContext.Current != null)
			{
				uploadProgressUrl += "&lastPostBackID=" + UploadContext.Current.PostBackID;
			}
			
			if (Inline)
			{
				Tag = HtmlTextWriterTag.Iframe;
				Attributes["src"] = uploadProgressUrl + "&canScript=false&canCancel=false";
				Attributes["frameborder"] = "0";
				Attributes["scrolling"] = "no";
				Attributes["name"] = this.ClientID;
				displayStatement = @"
setTimeout(""frames['" + this.ClientID + @"'].location.href = '" + uploadProgressUrl + @"&refresher=client&canScript=true&canCancel=' + NeatUploadCanCancel();"", 0);
";
				this.Page.RegisterStartupScript(this.UniqueID + "UpdateIFrameSrc", @"
<script type=""text/javascript"" language=""javascript"">
<!--
if (frames['" + this.ClientID + @"'])
	frames['" + this.ClientID + @"'].location.replace('" + uploadProgressUrl + @"&canScript=true&canCancel=' + NeatUploadCanCancel());
// -->
</script>
");
			}
			else
			{
				Tag = HtmlTextWriterTag.Div;
				displayStatement = GetPopupDisplayStatement();
				this.Page.RegisterStartupScript(this.UniqueID + "RemoveDiv", @"
<script type=""text/javascript"" language=""javascript"">
<!--
NeatUpload_DivNode = document.getElementById ? document.getElementById('" + this.ClientID + @"') : null; 
if (NeatUpload_DivNode)
	NeatUpload_DivNode.parentNode.removeChild(NeatUpload_DivNode);
// -->
</script>
");
			}
		}
		
		private string GetPopupDisplayStatement()
		{
			string width = GetPopupDimension("Width", Width, 500);
			string height = GetPopupDimension("Height", Height, 100);
			return @"window.open('" + uploadProgressUrl + "&refresher=client&canScript=true&canCancel=' + NeatUploadCanCancel(), '"
			                        + FormContext.Current.PostBackID + @"','width=" + width + @",height=" + height
			                        + @",directories=no,location=no,menubar=no,resizable=yes,scrollbars=auto,status=no,toolbar=no');";
		}
			
		private string GetPopupDimension(string name, Unit length, int min)
		{
			if (length.Type == UnitType.Pixel && length.Value >= min)
			{
				return length.ToString();
			}
			else if (InlineRequested || length == Unit.Empty)
			{
				return min.ToString();
			}
			else
			{
				throw new System.ArgumentOutOfRangeException(name, "must be at least " + min + " pixels and must use pixel(px) units when using a popup ProgressBar.");
			}
		}
		
		/// <summary>
		/// Adds a control (typically a button) to a list trigger controls.</summary>
		/// <param name="control">the control to add to the list</param>
		/// <remarks>
		/// See the <see cref="Triggers"/> property for information on what triggers are.  This method is
		/// primarily for situations where the see cref="Triggers"/> property can't be used because the ID of the
		/// trigger control is not known until runtime (e.g. for
		/// controls in Repeaters).  Controls added via this method are maintained in a separate list from those
		/// listed in the <see cref="Triggers"/> property, and said list is not maintained as part of this
		/// control's <see cref="ViewState"/>.  That means that if you use this method, you will need to call it
		/// for each request, not just non-postback requests.  Also, you can use both this method and the
		/// <see cref="Triggers"/> property for the same control.
		/// </remarks>
		public void AddTrigger(Control control)
		{
			otherTriggers.Add(control);
		}

		/// <summary>
		/// Adds a control (typically a button) to a list non-upload controls.</summary>
		/// <param name="control">the control to add to the list</param>
		/// <remarks>
		/// See the <see cref="NonUploadButtons"/> property for information on what non-upload buttons are.
		/// This method is primarily for situations where the see cref="NonUploadButtons"/> property can't be used
		/// because the ID of the non-upload control is not known until runtime (e.g. for
		/// controls in Repeaters).  Controls added via this method are maintained in a separate list from those
		/// listed in the <see cref="NonUploadButtons"/> property, and said list is not maintained as part of this
		/// control's <see cref="ViewState"/>.  That means that if you use this method, you will need to call it
		/// for each request, not just non-postback requests.  Also, you can use both this method and the
		/// <see cref="NonUploadButtons"/> property for the same control.
		/// </remarks>
		private void AddNonUploadButton(Control control)
		{
			otherNonUploadButtons.Add(control);
		}

		private void RegisterScripts()
		{
			HtmlControl formControl = GetFormControl(this);
			this.Page.RegisterStartupScript(formControl.UniqueID + "-OnSubmit", @"
<script type=""text/javascript"" language=""javascript"">
<!--
function NeatUpload_OnSubmitForm_" + formControl.ClientID + @"()
{
	var elem = document.getElementById('" + formControl.ClientID + @"');
	return elem.NeatUpload_OnSubmit();
}
// -->
</script>
");

			StringBuilder scriptBuilder = new StringBuilder();
			scriptBuilder.Append(@"
<script type=""text/javascript"" language=""javascript"">
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
			{
				return;
			}
			
			if (!IsDesignTime)
			{
				// We can't register these scripts during PreRender because if we do there will be no way to
				// programmatically add triggers that are in data-bound controls that occur after the ProgressBar.
				RegisterScripts();
			}
			
			// Enclose the pop-up fallback div in a <noscript> tag to ensure that it is not visible, even during
			// page load.
			if (!Inline && !IsDesignTime)
			{
				writer.Write("<noscript>");
			}
			EnsureChildControls();
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(Tag);
			
			bool renderFallback = true;
			// Don't render the no-iframe fallback if the browser supports frames because there have been reports
			// that the fallback is briefly visible in some browsers.
			if (Inline && !IsDesignTime && Context != null && Context.Request != null && Context.Request.Browser != null && Context.Request.Browser.Frames)
			{
		    	renderFallback = false;
		    }
		    if (renderFallback)
		    {
		    	
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
			    
				writer.AddAttribute("id", ClientID + "_fallback_link");
				writer.AddAttribute("href", uploadProgressUrl + "&refresher=server&canScript=false&canCancel=false");
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
			}
			writer.RenderEndTag();
			if (!Inline && !IsDesignTime)
			{
				writer.Write("</noscript>");
			}
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
NeatUpload_FallbackLink = document.getElementById ? document.getElementById('" + this.ClientID + @"_fallback_link') : null;
if (NeatUpload_FallbackLink)
	NeatUpload_FallbackLink.setAttribute('href', ""javascript:" + GetPopupDisplayStatement() + @""");
						
if (typeof(NeatUpload_NonUploadIDs) == 'undefined')
{
	NeatUpload_NonUploadIDs = new Object();
	NeatUpload_NonUploadIDs.NeatUpload_length = 0;
}
if (typeof(NeatUpload_TriggerIDs) == 'undefined')
{
	NeatUpload_TriggerIDs = new Object();
	NeatUpload_TriggerIDs.NeatUpload_length = 0;
}
NeatUpload_NonUploadIDs_" + this.ClientID + @" = new Object();
NeatUpload_NonUploadIDs_" + this.ClientID + @".NeatUpload_length = 0;
NeatUpload_TriggerIDs_" + this.ClientID + @" = new Object();
NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length = 0;

NeatUpload_LastEventSource = null;
NeatUpload_LastEventType = null;
NeatUpload_AlertShown = true;

NeatUpload_AddSubmitHandler('" + formControl.ClientID + "'," + (Inline ? "false" : "true") + @", function () {
	var formElem = document.getElementById('" + formControl.ClientID + @"');
	// If trigger controls were specified for this progress bar and the trigger is not 
	// specified for *any* progress bar, then clear the filenames.
	if (NeatUpload_LastEventSource
	    && (NeatUpload_IsElemWithin(NeatUpload_LastEventSource, NeatUpload_NonUploadIDs_" + this.ClientID + @")
	      	|| NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length)
	    && !NeatUpload_IsElemWithin(NeatUpload_LastEventSource, NeatUpload_TriggerIDs))
	{
		return NeatUpload_ClearFileInputs(formElem);
	}
	// If there are files to upload and either no trigger controls were specified for this progress bar or
	// a specified trigger control was triggered, then start the progress display.
	if (NeatUpload_IsFilesToUpload('" + formControl.ClientID + @"')
		&& (!NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length
		    || NeatUpload_IsElemWithin(NeatUpload_LastEventSource, NeatUpload_TriggerIDs_" + this.ClientID + @")))
	{
		" + displayStatement + @"
	}
	return true;
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
		NeatUpload_LastEventSource = src;
		NeatUpload_AlertShown = false;
		if (ev.type != 'click' && ev.type != 'keypress')
		{
			return true;
		}
		if (NeatUpload_IsElemWithin(src, NeatUpload_TriggerIDs)
		      || NeatUpload_TriggerIDs.NeatUpload_length == 0)
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
			if (document.getElementById && (!inputType || inputType == 'submit' || inputType == 'image'))
			{
				var formElem = document.getElementById('" + formControl.ClientID + @"');
				NeatUpload_ClearFileInputs(formElem);
			}
		}
		return true;
	}, true);
}

NeatUpload_AddSubmittingHandler('" + formControl.ClientID + @"', function () {
	if (!NeatUpload_LastEventSource)
	{
		return;
	}
	if (NeatUpload_IsElemWithin(NeatUpload_LastEventSource, NeatUpload_TriggerIDs))
	{
		return;
	}
	var formElem = document.getElementById('" + formControl.ClientID + @"');
	if (NeatUpload_IsElemWithin(NeatUpload_LastEventSource, NeatUpload_NonUploadIDs)
	     || NeatUpload_TriggerIDs.NeatUpload_length)
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
			
			scriptBuilder.Append(@"NeatUpload_NonUploadIDs['" + control.ClientID + @"'] 
	= ++NeatUpload_NonUploadIDs.NeatUpload_length;
");			
			scriptBuilder.Append(@"NeatUpload_NonUploadIDs_" + this.ClientID + @"['" + control.ClientID + @"'] 
	= ++NeatUpload_NonUploadIDs_" + this.ClientID + @".NeatUpload_length;
");			
		}

		private void AddTriggerScripts(StringBuilder scriptBuilder, Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			scriptBuilder.Append(@"NeatUpload_TriggerIDs['" + control.ClientID + @"'] 
	= ++NeatUpload_TriggerIDs.NeatUpload_length;
");			
			scriptBuilder.Append(@"NeatUpload_TriggerIDs_" + this.ClientID + @"['" + control.ClientID + @"'] 
	= ++NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length;
");			
		}

		private string clientScript = @"
<script type=""text/javascript"" language=""javascript"">
<!--

if (!Array.prototype.push)
{
	Array.prototype.push = function() {
		for (var i = 0; i < arguments.length; i++)
			this[this.length] = arguments[i];
		return this.length;
	};
}

if (!Array.prototype.unshift)
{
	Array.prototype.unshift = function() {
		this.reverse();
		for (var i = 0; i < arguments.length; i++)
			this[this.length] = arguments[i];
		this.reverse();
		return this.length;
	};
}

if (!Function.prototype.call)
{
	Function.prototype.call = function() {
		var obj = arguments[0];
		obj._NeatUpload_tmpFunc = this;
		var argList = '';
		for (var i = 1; i < arguments.length; i++)
		{
			argList += 'arguments[' + i + ']';
			if (i < arguments.length - 1)
				argList += ',';
		}
		var result = eval('obj._NeatUpload_tmpFunc(' + argList + ')');
		obj._NeatUpload_tmpFunc = null;
		return result;
	};
}

function NeatUpload_IsElemWithin(elem, assocArray)
{
	while (elem)
	{
		if (elem.id && assocArray[elem.id])
		{
			return true;
		}
		elem = elem.parentNode;
	}
}

function NeatUploadCanCancel()
{
	try
	{
		if (window.stop || window.document.execCommand)
			return true;
		else
			return false;
	}
	catch (ex)
	{
		return false;
	}
}
function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { if (origHandler(e) == false) return false; return newHandler(e); };
};
function NeatUpload_AddHandler(id, eventName, handler, useCapture)
{
	if (typeof(useCapture) == 'undefined')
		useCapture = false;
	if (!document.getElementById)
		return;
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
	if (!document.getElementById)
		return false;
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
				if (navigator && navigator.userAgent)
				{
					var ua = navigator.userAgent.toLowerCase();
					var msiePosition = ua.indexOf('msie');
					if (msiePosition != -1 && typeof(ActiveXObject) != 'undefined' && ua.indexOf('mac') == -1
					    && ua.charAt(msiePosition + 5) < 7)
					{
						var re = new RegExp('^(\\\\\\\\[^\\\\]|([a-zA-Z]:)?\\\\).*');
						var match = re.exec(inputElem.value);
						if (match == null || match[0] == '')
						{
							if (typeof(NeatUpload_HandleIE6InvalidPath) != 'undefined'
							    && NeatUpload_HandleIE6InvalidPath != null)
								NeatUpload_HandleIE6InvalidPath(inputElem);
							return false;
						}
					}
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
		// NOTE: clearing (by removing and recreating) empty file inputs confuses IE6 when the document is
		// in both the top-level window and in an iframe.  ExpertTree uses such an iframe to do AJAX-style
		// callbacks.
		if (inputFile.type == 'file' && inputFile.value && inputFile.value.length > 0)
		{
			try
			{
				var newInputFile = document.createElement('input');
				for (var a=0; a < inputFile.attributes.length; a++)
				{
					var attr = inputFile.attributes.item(a); 
					if (! attr.specified)
						continue;
					var attrName = attr.name.toLowerCase();
					if (attrName != 'type' && attrName != 'value')
					{
						if (attrName == 'style' && newInputFile.style && newInputFile.style.cssText)
							newInputFile.style.cssText = attr.value;
						else if (attrName == 'class') // Needed for IE because 'class' is a JS keyword
							newInputFile.className = attr.value;
						else if (attrName == 'for') // Needed for IE because 'for' is a JS keyword
							newInputFile.htmlFor = attr.value;
						else
							newInputFile.setAttribute(attr.name, attr.value);
					}
				}
				newInputFile.setAttribute('type', 'file');
				inputFile.parentNode.replaceChild(newInputFile, inputFile);
			}
			catch (ex)
			{
				// I don't know of any other way to clear the file inputs, so on browser where we get an error
				// (eg Mac IE), we just give the user a warning.
				if (inputFile.value != null && inputFile.value != '')
				{
					if (!NeatUpload_AlertShown)
					{
						window.alert('" + Config.Current.ResourceManager.GetString("ClearFileNamesAlert") + @"');
						NeatUpload_AlertShown = true;
					}
					return false;
				}
			}
		}
	}
	return true;
}

function NeatUpload_AddSubmitHandler(formID, isPopup, handler)
{
	if (!document.getElementById)
		return;
	var elem = document.getElementById(formID);
	if (!elem.NeatUpload_OnSubmitHandlers) 
	{
		elem.NeatUpload_OnSubmitHandlers = new Array();
		elem.NeatUpload_OrigSubmit = elem.submit;
		elem.NeatUpload_OnSubmit = NeatUpload_OnSubmit;
		try
		{
			elem.submit = function () {
				elem.NeatUpload_OnSubmitting();
				elem.NeatUpload_OrigSubmit();
				elem.NeatUpload_OnSubmit();
			};
		}
		catch (ex)
		{
			// We can't override the submit method.  That means NeatUpload won't work 
			// when the form is submitted programmatically.  This occurs in Mac IE.
		}			
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
	if (!document.getElementById)
		return;
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
		if (!this.NeatUpload_OnSubmitHandlers[i].call(this))
			return false;
	}
	return true;
}
// -->
</script>
";
	}
}
