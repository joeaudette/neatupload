/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005-2007  Dean Brettle

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
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.Design;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Web.Security;
using Brettle.Web.NeatUpload.Internal.UI;

namespace Brettle.Web.NeatUpload
{	
	/// <summary>
	/// Multiple file upload control that can be used with the <see cref="UploadHttpModule"/> and <see cref="ProgressBar"/>.
	/// </summary>
	/// <remarks>
	/// On post back, you can use <see cref="Files"/> to access the array of <see cref="UploadedFile"/>s.
	/// For each <see cref="UploadedFile"/> in the collection, use <see cref="UploadedFile.FileName"/>, 
	//// <see cref="UploadedFile.ContentType"/>, <see cref="UploadedFile.ContentLength"/>, and
	/// <see cref="UploadedFile.InputStream"/>
	/// to access the file's name, MIME type, length, and contents.  If you want to save the file for use after
	/// the current request, use the <see cref="UploadedFile.MoveTo"/> method.
	/// This control will function even if the <see cref="UploadHttpModule"/> is not being used.  In that case,
	/// its methods/properties act on the file in the standard ASP.NET <see cref="HttpRequest.Files"/> collection.
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[ValidationProperty("ValidationFileNames")]
	[ParseChildren(false)]
	[PersistChildren(true)]
	public class MultiFile : FileControl, System.Web.UI.IPostBackDataHandler
	{

		// Create a logger for use in this class
		private static readonly log4net.ILog log
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		

		public new UploadedFile[] Files
		{
			get 
			{
				return base.Files;
			}
		}		
		
		private string _validationFileNames;
		
		/// <summary>
		/// Client-side names of the uploaded files for validation purposes, separated by semicolons.</summary>
		[Browsable(false)]
		public string ValidationFileNames {
			get 
			{
				if (_validationFileNames == null)
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();
					for (int i = 0; i < Files.Length; i++)
					{
						sb.Append(Files[i].FileName + ";");
					}
					_validationFileNames = sb.ToString();
				}
				return _validationFileNames;
			}
		}
		
		/// <summary>
		/// The ClientID of the control where the file queue should be displayed.</summary>
		/// <remarks>
		/// Defaults to "" which will cause the file queue to be displayed in a DIV element that is 
		/// automatically inserted immediately before this MultiFile control.</remarks>
		public string FileQueueControlID
		{
			get
			{
				string val = Attributes["FileQueueControlID"];
				if (val == null)
					return "";
				else
					return val;
			}
			set
			{
				Attributes["FileQueueControlID"] = value;
			}
		}
				
		public bool UseFlashIfAvailable
		{
			get
			{
				object val = ViewState["UseFlashIfAvailable"];
				if (val == null)
					return false;
				else
					return (bool)val;
			}
			set
			{
				ViewState["UseFlashIfAvailable"] = value;
			}
		}

		public string FlashFilterExtensions
		{
			get
			{
				string val = (string)ViewState["FlashFilterExtensions"];
				if (val == null || val.Length == 0)
					return "";
				return val;
			}
			set
			{
				ViewState["FlashFilterExtensions"] = value;
			}
		}

		public string FlashFilterDescription
		{
			get
			{
				string val = (string)ViewState["FlashFilterDescription"];
				if (val == null || val.Length == 0)
					return "";
				return val;
			}
			set
			{
				ViewState["FlashFilterDescription"] = value;
			}
		}

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
		}
				
		protected override void OnPreRender (EventArgs e)
		{
			if (!IsDesignTime && UploadModule.IsEnabled)
			{
				if (!Page.IsClientScriptBlockRegistered("NeatUploadMultiFile"))
				{
					Page.RegisterClientScriptBlock("NeatUploadMultiFile", @"
	<script type='text/javascript' language='javascript' src='" + AppPath + @"/NeatUpload/SWFUpload.js?guid=" 
		+ CacheBustingGuid + @"'></script>");
				}
				if (!Page.IsClientScriptBlockRegistered("NeatUploadJs"))
				{
					Page.RegisterClientScriptBlock("NeatUploadJs", @"
	<script type='text/javascript' language='javascript' src='" + AppPath + @"/NeatUpload/NeatUpload.js?guid=" 
		+ CacheBustingGuid + @"'></script>");
				}
			}
			base.OnPreRender(e);
		}

		protected override void Render(HtmlTextWriter writer)
		{
			string targetDivID = "NeatUploadDiv_" + this.ClientID;
			string name;
			string storageConfigName;
			if (!IsDesignTime && UploadModule.IsEnabled)
			{
				// Generate a special name recognized by the UploadHttpModule
				name = FormContext.Current.GenerateFileID(this.UniqueID);
				storageConfigName = FormContext.Current.GenerateStorageConfigID(this.UniqueID);
				ArmoredNameValueCollection armoredCookies = new ArmoredNameValueCollection();
				if (HttpContext.Current != null)
				{
					HttpCookieCollection cookies = HttpContext.Current.Request.Cookies;
					string[] cookieNames 
						= new string[] {"ASP.NET_SESSIONID", "ASPSESSION", FormsAuthentication.FormsCookieName};
					foreach (string cookieName in cookieNames)
					{
						HttpCookie cookie = cookies[cookieName];
						if (cookie != null)
							armoredCookies[cookieName] = cookie.Value;
					}
				}
					
				this.Page.RegisterStartupScript("NeatUploadMultiFile-" + this.UniqueID, @"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadMultiFileCreate('" + this.ClientID + @"', 
		'" + FormContext.Current.PostBackID + @"',
		'" + AppPath + @"',
		'" + AppPath + UploadModule.AsyncUploadPath +@"',
		'" + UploadModule.PostBackIDQueryParam + @"',
		{" + UploadModule.PostBackIDQueryParam + @" : '" + FormContext.Current.PostBackID + @"',
		 " + UploadModule.AsyncControlIDQueryParam + @": '" + this.ClientID + @"',
		 " + UploadModule.ArmoredCookiesQueryParam + @": '" + UploadModule.Protect(armoredCookies) + @"'
		},
		 " + (UseFlashIfAvailable ? "true" : "false") + @",
		 '" + FileQueueControlID + @"',
		 '" + FlashFilterExtensions + @"',
		 '" + FlashFilterDescription + @"'
);
// -->
</script>");
			}
			else
			{
				name = this.UniqueID;
				storageConfigName = UploadModule.ConfigFieldNamePrefix + "-" + this.UniqueID;
			}

			// Store the StorageConfig in a hidden form field with a related name
			if (StorageConfig != null && StorageConfig.Count > 0)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
				writer.AddAttribute(HtmlTextWriterAttribute.Name, storageConfigName);
				
				writer.AddAttribute(HtmlTextWriterAttribute.Value, UploadModule.Protect(StorageConfig));				
				writer.RenderBeginTag(HtmlTextWriterTag.Input);
				writer.RenderEndTag();
			}
			
			writer.AddAttribute(HtmlTextWriterAttribute.Id, targetDivID);
 			writer.RenderBeginTag(HtmlTextWriterTag.Div);

			base.AddAttributesToRender(writer);
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "file");
			writer.AddAttribute(HtmlTextWriterAttribute.Name, name);
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag(); // input type="file"

			if (UploadModule.IsEnabled && HasControls())
			{
				writer.Write("<div style='position: relative; display: none;'>");
				RenderChildren(writer);
				writer.Write("</div>");
			}

			if (UploadModule.IsEnabled)
			{
				// The constant strings below are broken apart so that you couldn't just search for the text and
				// remove it.  To find this code, you probably had to understand enough about custom web controls
				// to know where to look.  People who can't find this code are generally less experienced, harder
				// to support, and less likely to submit patches.  So they contribute in another way when they
				// use NeatUpload - they contribute by advertising it.  If they don't want to do that, they can
				// always have someone more capable find and remove the code for them (probably for a fee).
				// For more information, see the "Branding, Licensing, and the Trademark" section in 
				// docs/Manual.html.
				writer.AddStyleAttribute(HtmlTextWriterStyle.FontSize, "smal" + "ler");
				writer.RenderBeginTag(HtmlTextWriterTag.Span);
				writer.Write("&nbsp;(Po" + "wer" +"ed&nb" + "sp;by&nb" + "sp;");
				writer.AddAttribute(HtmlTextWriterAttribute.Target, "_bla" + "nk");
				writer.AddAttribute(HtmlTextWriterAttribute.Href, 
					"htt" +"p://ww" + "w.bre"+ "ttle." + "com/" + "neat" + "upload");
				writer.RenderBeginTag(HtmlTextWriterTag.A);
				writer.Write("Neat" + "Upload");
				writer.RenderEndTag(); // a
				writer.Write(")");
				writer.RenderEndTag(); // span
			}

 			writer.RenderEndTag(); // div
		}
		/// <summary>
		/// Called by ASP.NET so that controls can find and process their post back data</summary>
		/// <returns>true if a file was uploaded with this control</returns>
		public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
		{		
			return (Files.Length > 0);
		}		

		/// <summary>
		/// Called by ASP.NET if <see cref="LoadPostData"/> returns true (i.e. if a file was uploaded to this 
		/// control).  Fires the <see cref="FileUploaded"/> event.</summary>
		public virtual void RaisePostDataChangedEvent()
		{
			if (FileUploaded != null)
			{
				FileUploaded(this, EventArgs.Empty);
			}
		}
		
		/// <summary>
		/// Fired when at least one file is uploaded to this control.</summary>
		public event System.EventHandler FileUploaded;
	}
}