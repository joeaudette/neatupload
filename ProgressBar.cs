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

		/// <summary>
		/// Whether to display the progress bar inline or as a pop-up.</summary>
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
				return (ViewState["inline"] != null && (bool)ViewState["inline"]);
			}
			set { ViewState["inline"] = value; }
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
		
		protected override void OnPreRender (EventArgs e)
		{
			if (!IsDesignTime && Config.Current.UseHttpModule)
			{
				InitializeVars();
				RegisterScripts();
			}
			base.OnPreRender(e);
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
setTimeout(""frames['" + this.ClientID + @"'].location.href = '" + uploadProgressUrl + @"&refresher=client&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel();"", 0);
";
				this.Page.RegisterStartupScript(this.UniqueID + "UpdateIFrameSrc", @"
<script language=""javascript"">
<!--
if (frames['" + this.ClientID + @"'])
	frames['" + this.ClientID + @"'].location.replace('" + uploadProgressUrl + @"&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel());
// -->
</script>
");
			}
			else
			{
				Tag = HtmlTextWriterTag.Div;
				displayStatement = GetPopupDisplayStatement();
			}
		}
		
		private string GetPopupDisplayStatement()
		{
			string width = GetPopupDimension("Width", Width, 500);
			string height = GetPopupDimension("Height", Height, 100);
			return @"window.open('" + uploadProgressUrl + "&refresher=client&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel(), '"
			                        + FormContext.Current.PostBackID + @"','width=" + width + @",height=" + height
			                        + @",directories=no,location=no,menubar=no,resizable=yes,scrollbars=auto,status=no,toolbar=no');";
		}
			
		private string GetPopupDimension(string name, Unit length, int min)
		{
			if (length.Type == UnitType.Pixel && length.Value >= min)
			{
				return length.ToString();
			}
			else if (Inline || length == Unit.Empty)
			{
				return min.ToString();
			}
			else
			{
				throw new System.ArgumentOutOfRangeException(name, "must be at least " + min + " pixels and must use pixel(px) units");
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
			if (!Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
			{
				string appPath = Context.Request.ApplicationPath;
				if (appPath == "/")
				{
					appPath = "";
				}
				Page.RegisterClientScriptBlock("NeatUploadProgressBar", @"
<script src='" + appPath + @"/NeatUpload/ProgressBar.js'></script>
<script language='javascript'>
NeatUploadPB.prototype.ClearFileNamesAlert = '" +  Config.Current.ResourceManager.GetString("ClearFileNamesAlert") + @"';
// -->
</script>
");
			}
			
			string allTriggerClientIDs = "[]";
			string allNonUploadButtonClientIDs = "[]";
			
			if (Config.Current.UseHttpModule)
			{
				allTriggerClientIDs = GetClientIDsAsJSArray(Triggers, otherTriggers);
				allNonUploadButtonClientIDs = GetClientIDsAsJSArray(NonUploadButtons, otherNonUploadButtons);
			}
						
			this.Page.RegisterStartupScript("NeatUploadProgressBar-" + this.UniqueID, @"
<script language=""javascript"">
<!--
NeatUploadPB.prototype.Bars['" + this.ClientID + @"'] 
	= new NeatUploadPB('" + this.ClientID + @"',
	                    """ + this.GetPopupDisplayStatement() + @""",
	                    "  + (Inline ? "true" : "false") + @",
	                    function() { " + displayStatement + @" },
	                    " + allTriggerClientIDs + @",
	                    " + allNonUploadButtonClientIDs + @");
// -->
</script>");
                                                                               
		}
		
		private string GetClientIDsAsJSArray(string idsString, ArrayList controls)
		{
			ArrayList ids = new ArrayList(); // IDs of buttons listed in idsString
			if (idsString != null)
			{
				ids.AddRange(idsString.Split(' '));
			}
			
			ArrayList clientIDs = new ArrayList();
			foreach (string id in ids)
			{
				Control c = NamingContainer.FindControl(id);
				if (c == null)
					continue;
				clientIDs.Add(c.ClientID);
			}
			foreach (Control c in otherNonUploadButtons)
			{
				clientIDs.Add(c.ClientID);
			}
			if (clientIDs.Count == 0)
			{
				return "[]";
			}
			return "['" + String.Join("','", (string[])clientIDs.ToArray(typeof(string))) + "']";
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
			
			// Enclose the pop-up fallback div in a <noscript> tag to ensure that it is not visible, even during
			// page load.
			if (!Inline && !IsDesignTime)
			{
				writer.Write("<noscript id='" + ClientID + @"_noscript'>");
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
	}
}
