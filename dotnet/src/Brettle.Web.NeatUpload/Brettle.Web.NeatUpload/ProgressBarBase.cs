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
using Brettle.Web.NeatUpload.Internal;
using Brettle.Web.NeatUpload.Internal.UI;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Base class for controls that display the progress and status of an upload.</summary>
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
	[Designer(typeof(ProgressBarBaseDesigner))]
	public abstract class ProgressBarBase : System.Web.UI.WebControls.WebControl
	{
		protected string UploadProgressPath;
		private ArrayList otherTriggers = new ArrayList(); // Controls passed to AddTrigger()

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
		/// JavaScript expression which is evaluated when the form is submitted to determine whether to start the 
		/// progress display.</summary>
		/// <remarks>
		/// Defaults to "IsFilesToUpload()" which will cause the progress display to only start when there
		/// are files to be uploaded.</remarks>
		public string AutoStartCondition
		{
			get { return (string)ViewState["AutoStartCondition"]; }
			set { ViewState["AutoStartCondition"] = value; }
		}
		
		/// <summary>
		/// URL of an aspx page that displays the upload progress.</summary>
		/// <remarks>
		/// The specified page should inherits from the <see cref="Brettle.Web.NeatUpload.Progress"/> code behind class.
		/// You may use an absolute or relative URL that refers to a page in the same web application.  If the URL
		/// starts with "~", the "~" will be replaced with the web application root as returned by
		/// <see cref="HttpRequest.ApplicationPath"/>.  By default, "~/NeatUpload/Progress.aspx" will be used.</remarks>
		[Editor(typeof(UrlEditor), typeof(UITypeEditor)), Bindable(true), DefaultValue("~/NeatUpload/Progress.aspx")]
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

		private string AppPath
		{
			get 
			{
				string appPath = Context.Request.ApplicationPath;
				if (appPath == "/")
				{
					appPath = "";
				}
				return appPath;
			}
		}
		
		protected override void OnPreRender (EventArgs e)
		{
			if (UploadModule.IsEnabled)
			{
                // If we don't have a session yet and the session mode is not "Off", we need to create a
                // session so that it can be used to pass information between the progress display and the 
                // upload request.  Apparently the only way to force the session to be created is to put
                // something in it.
                if (HttpContext.Current.Session != null
                    && Page.Session.Mode != System.Web.SessionState.SessionStateMode.Off
                    && Page.Session.Count == 0
                    && !Page.Session.IsReadOnly)
                {
                    Page.Session["NeatUpload_value"] = "ignored";
                }

				InitializeVars();
				if (!Page.IsClientScriptBlockRegistered("NeatUploadJs"))
				{
					Page.RegisterClientScriptBlock("NeatUploadJs", @"
	<script type='text/javascript' language='javascript' src='" + AppPath + @"/NeatUpload/NeatUpload.js?guid=" 
		+ CacheBustingGuid + @"'></script>");
				}
				if (!Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
				{
					Page.RegisterClientScriptBlock("NeatUploadProgressBar", @"
	<script type='text/javascript' language='javascript'>
	NeatUploadPB.prototype.ClearFileNamesAlert = '" +  ResourceManagerSingleton.GetResourceString("ClearFileNamesAlert") + @"';
	// -->
	</script>
	");
				}
				this.Page.RegisterStartupScript("NeatUploadProgressBarBase-" + this.UniqueID, GetStartupScript());
            }
			base.OnPreRender(e);
		}
		
		/// <summary>
		/// This method does nothing.  It is only provided to ensure API compatibility with earlier NeatUpload
		/// versions.  In those earlier versions, this method registered an OnSubmit statement to
		/// ensure that this ProgressBar will start when the page is submitted.</summary>
		/// <param name="source">Ignored</param>
		/// <param name="e">Ignored</param>
		[Obsolete("No longer necessary")]
		public void RegisterOnSubmitStatement(object source, EventArgs e)
		{
		}
				
		private void InitializeComponent()
		{
		}
		
		internal static string ApplyAppPathModifier(string url)
		{
			string appPath = HttpContext.Current.Request.ApplicationPath;
			if (appPath == "/")
			{
				appPath = "";
			}
			string requestUrl = HttpContext.Current.Request.RawUrl;
			string result = HttpContext.Current.Response.ApplyAppPathModifier(url);
			
			// Workaround Mono XSP bug where ApplyAppPathModifier() doesn't add the session id
			if (requestUrl.StartsWith(appPath + "/(") && !result.StartsWith(appPath + "/("))
			{
				if (url.StartsWith("/") && url.StartsWith(appPath))
				{
					url = "~" + url.Remove(0, appPath.Length);
				}
				if (url.StartsWith("~/"))
				{
					string[] compsOfPathWithinApp = requestUrl.Substring(appPath.Length).Split('/');
					url = appPath + "/" + compsOfPathWithinApp[1] + "/" + url.Substring(2);
				}
				result = url;
			}
			return result;
		}
		
		private void InitializeVars()
		{
			if (!UploadModule.IsEnabled)
				return;

			if (AutoStartCondition == null)
			{
				AutoStartCondition = "IsFilesToUpload()";
			}
			
			UploadProgressPath = Url;
			if (UploadProgressPath == null)
			{
				UploadProgressPath = "~/NeatUpload/Progress.aspx";
			}
			UploadProgressPath = ApplyAppPathModifier(UploadProgressPath);

			if (Attributes["class"] == null)
			{
				Attributes["class"] = "ProgressBar";
			}

			UploadProgressPath += "?barID=" + this.ClientID;
		}
					
		private string GetPopupDimension(string name, Unit length, int min)
		{
			if (length.Type == UnitType.Pixel && length.Value >= min)
			{
				return length.ToString();
			}
			else
			{
				return min.ToString();
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
            Page.RegisterClientScriptBlock(String.Format("NeatUploadProgressBar-{0}-{1}", ClientID, control.ClientID),
                String.Format(@"<script type='text/javascript' language='javascript'>
	if (typeof(NeatUploadPB_{0}_Triggers) == 'undefined') NeatUploadPB_{0}_Triggers = [];
    NeatUploadPB_{0}_Triggers.push('{1}');
	// -->
	</script>
	", ClientID, control.ClientID));
        }

		// This is used to ensure that the browser gets the latest NeatUpload.js each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when NeatUpload.js changes,
		// but computing a hash on that file everytime this assembly is loaded strikes me as overkill.
		private static Guid CacheBustingGuid = System.Guid.NewGuid();
		
		protected virtual string GetStartupScript()
		{
			string allTriggerClientIDs = "[]";
			if (UploadModule.IsEnabled)
			{
				allTriggerClientIDs = GetClientIDsAsJSArray(Triggers);
			}
						
			return String.Format(@"
<script type='text/javascript' language='javascript'>
<!--
if (typeof(NeatUploadPB_{0}_Triggers) == 'undefined') NeatUploadPB_{0}_Triggers = [];
NeatUploadPB_{0}_Triggers = NeatUploadPB_{0}_Triggers.concat(" + allTriggerClientIDs + @");
NeatUploadPB.prototype.Bars['{0}'] 
	= new NeatUploadPB('{0}','" 
						+ FormContext.Current.PostBackID + @"','"
	                    + this.UploadProgressPath + @"','"
	                    + this.GetPopupDimension("Width", Width, 500) + @"','"
                        + this.GetPopupDimension("Height", Height, 100) 
                        + @"',NeatUploadPB_{0}_Triggers, '"
	                    + AutoStartCondition.Replace(@"'", @"\'") + @"');
if (!NeatUploadPB.prototype.FirstBarID)
	NeatUploadPB.prototype.FirstBarID = '{0}';
// -->
</script>", ClientID);
                                                                               
		}
		
		private string GetClientIDsAsJSArray(string idsString)
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
				if (c != null)
				{
					clientIDs.Add(c.ClientID);
				}
				else
				{
					// If we couldn't find a control with that ID, it might be a client-side-only element.
					// In that case assume that the ID that was specified is already the Client ID.
					clientIDs.Add(id);
				}
				
			}
			if (clientIDs.Count == 0)
			{
				return "[]";
			}
			return "['" + String.Join("','", (string[])clientIDs.ToArray(typeof(string))) + "']";
		}
		
		public override void RenderBeginTag(HtmlTextWriter writer)
		{
			if (!UploadModule.IsEnabled)
			{
				return;
			}
			
			// Add an empty <span> element with an ID, so that the JS will know where to find this ProgressBar
			// so it know where to start looking for the containing form.
			writer.Write("<span id='" + ClientID + @"_NeatUpload_dummyspan'/>");
			// Enclose the pop-up fallback div in a <noscript> tag to ensure that it is not visible, even during
			// page load.  Note: we can't put the above dummy ID on the noscript element because Safari doesn't
			// include the noscript element in the DOM.
			writer.Write("<noscript>");
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(HtmlTextWriterTag.Div);
		}
		
		protected override void RenderContents(HtmlTextWriter writer)
		{
			if (!UploadModule.IsEnabled)
			{
				return;
			}

			EnsureChildControls();
		    
			writer.AddAttribute("id", ClientID + "_fallback_link");
			writer.AddAttribute("href", UploadProgressPath 
			                    + "&postBackID=" + FormContext.Current.PostBackID
			                    + "&refresher=server&canScript=false&canCancel=false");
			writer.AddAttribute("target", FormContext.Current.PostBackID);
			writer.RenderBeginTag(HtmlTextWriterTag.A);
			if (!HasControls())
			{
				writer.Write("Check Upload Progress");
			}
			base.RenderChildren(writer);
			writer.RenderEndTag();
		}
		
		internal void RenderChildControls(HtmlTextWriter writer)
		{
			RenderChildren(writer);
		}

		public override void RenderEndTag(HtmlTextWriter writer)
		{
			if (!UploadModule.IsEnabled)
			{
				return;
			}
			writer.RenderEndTag();
			writer.Write("</noscript>");
		}
		
		private ProgressInfo _ProcessingProgress;
		public ProgressInfo ProcessingProgress
		{
			get { return _ProcessingProgress; }
			set 
			{
				value.ControlID = UniqueID;
				_ProcessingProgress = value;
				value.UpdateProcessingState();
			}
		}
		
		[Browsable(false)]
		public string PostBackID
		{
			get { return FormContext.Current.PostBackID; }
		}
	}
}
