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
	/// displayed.  If you use the <see cref="Triggers"/> property (or the <see cref="AddTrigger"/> method)
	/// to specify which controls should cause files to be uploaded, then other controls will not start the progress
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

		private string UploadProgressPath;
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
			if (!IsDesignTime && Config.Current.UseHttpModule)
			{
                // If we don't have a session yet and the session mode is not "Off", we need to create a
                // session so that it can be used to pass information between the progress display and the 
                // upload request.  Apparently the only way to force the session to be created is to put
                // something in it.
                if (Page.Session != null
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
	NeatUploadPB.prototype.ClearFileNamesAlert = '" +  Config.Current.GetResourceString("ClearFileNamesAlert") + @"';
	// -->
	</script>
	");
				}
            }
			base.OnPreRender(e);
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
			if (IsDesignTime || !Config.Current.UseHttpModule)
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

			if (UploadContext.Current != null)
			{
				UploadProgressPath += "&lastPostBackID=" + UploadContext.Current.PostBackID;
			}
						
			if (Inline)
			{
				Tag = HtmlTextWriterTag.Iframe;
				Attributes["src"] = UploadProgressPath + "&canScript=false&canCancel=false&postBackID=" + FormContext.Current.PostBackID;
				Attributes["frameborder"] = "0";
				Attributes["scrolling"] = "no";
				Attributes["name"] = this.ClientID;
			}
			else
			{
				Tag = HtmlTextWriterTag.Div;
			}
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

		// This is used to ensure that the browser gets the latest NeatUpload.js each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when NeatUpload.js changes,
		// but computing a hash on that file everytime this assembly is loaded strikes me as overkill.
		private static Guid CacheBustingGuid = System.Guid.NewGuid();
		
		private void RegisterScripts()
		{
			string allTriggerClientIDs = "[]";
			if (Config.Current.UseHttpModule)
			{
				allTriggerClientIDs = GetClientIDsAsJSArray(Triggers, otherTriggers);
			}
						
			this.Page.RegisterStartupScript("NeatUploadProgressBar-" + this.UniqueID, @"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadPB.prototype.Bars['" + this.ClientID + @"'] 
	= new NeatUploadPB('" + this.ClientID + @"','" 
						+ FormContext.Current.PostBackID + @"','"
	                    + this.UploadProgressPath + @"',"
	                    + (Inline ? "true" : "false") + @",'"
	                    + this.GetPopupDimension("Width", Width, 500) + @"','"
	                    + this.GetPopupDimension("Height", Height, 100) + @"',"
	                    + allTriggerClientIDs + @", '"
	                    + AutoStartCondition.Replace(@"'", @"\'") + @"');
if (!NeatUploadPB.prototype.FirstBarID)
	NeatUploadPB.prototype.FirstBarID = '" + this.ClientID + @"';
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
			foreach (Control c in controls)
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
			
			if (!IsDesignTime)
			{
				// We can't register these scripts during PreRender because if we do there will be no way to
				// programmatically add triggers that are in data-bound controls that occur after the ProgressBar.
				RegisterScripts();
			}
			
			if (!Inline && !IsDesignTime)
			{
				// Add an empty <span> element with an ID, so that the JS will know where to find this ProgressBar
				// so it know where to start looking for the containing form.
				writer.Write("<span id='" + ClientID + @"_NeatUpload_dummyspan'/>");
				// Enclose the pop-up fallback div in a <noscript> tag to ensure that it is not visible, even during
				// page load.  Note: we can't put the above dummy ID on the noscript element because Safari doesn't
				// include the noscript element in the DOM.
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
				writer.AddAttribute("href", UploadProgressPath + "&postBackID=" + FormContext.Current.PostBackID + "&refresher=server&canScript=false&canCancel=false");
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
		
		private ProgressInfo _ProcessingProgress;
		public ProgressInfo ProcessingProgress
		{
			get { return _ProcessingProgress; }
			set 
			{ 
				_ProcessingProgress = value;
				UploadContext ctx = UploadContext.Current;
				if (ctx != null)
				{
					ctx.ProgressInfoByID[UniqueID] = _ProcessingProgress; 
					UploadHttpModule.AccessSession(new SessionAccessCallback(ctx.SyncWithSession));
				}
			}
		}
		
		public string PostBackID
		{
			get { return FormContext.Current.PostBackID; }
		}
	}
}
