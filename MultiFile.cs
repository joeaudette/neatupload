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

namespace Brettle.Web.NeatUpload
{	
	/// <summary>
	/// Multiple file upload control that can be used with the <see cref="UploadHttpModule"/> and <see cref="ProgressBar"/>.
	/// </summary>
	/// <remarks>
	/// On post back, you can use <see cref="Files"/> to access the <see cref="UploadedFileCollection"/>.
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
	public class MultiFile : System.Web.UI.WebControls.WebControl, System.Web.UI.IPostBackDataHandler
	{

		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/
		
		private bool IsDesignTime = (HttpContext.Current == null);

		private UploadedFileCollection _files = null;
		private UploadedFileCollection files
		{
			get 
			{
				if (_files == null)
				{
					_files = new UploadedFileCollection();
					if (IsDesignTime) return _files;
					
					// Get only the files that were uploaded from this control
					UploadedFileCollection allFiles = UploadHttpModule.Files;
					for (int i = 0; i < allFiles.Count; i++)
					{
						if (allFiles.GetKey(i) == this.UniqueID && allFiles[i].IsUploaded)
						{
							_files.Add(_files.Count.ToString(), allFiles[i]);
						}
					}
				}
				return _files;
			}
		}
		
		/// <summary>
		/// The <see cref="UploadedFileCollection"/> corresponding to the files uploaded to this control. </summary>
		/// <remarks>
		/// Derived classes can use this to access the <see cref="UploadedFile"/> objects that were created by the
		/// UploadStorageProvider.</remarks>
		public UploadedFileCollection Files
		{
			get 
			{
				return files;
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
					for (int i = 0; i < Files.Count; i++)
					{
						sb.Append(Files[i].FileName + ";");
					}
					_validationFileNames = sb.ToString();
				}
				return _validationFileNames;
			}
		}

		public string Accept
		{
			get
			{
				string val = Attributes["accept"];
				if (val == null)
					return String.Empty;
				else
					return val;
			}
			set
			{
				if (value == null || value == String.Empty)
					Attributes.Remove("accept");
				else
					Attributes["accept"] = value;
			}
		}
		
		public int MaxLength
		{
			get
			{
				string val = Attributes["maxlength"];
				if (val == null)
					return -1;
				else
					return Convert.ToInt32(val);
			}
			set
			{
				if (value == -1)
					Attributes.Remove("maxlength");
				else
					Attributes["maxlength"] = value.ToString();
			}
		}
				
		public int Size
		{
			get
			{
				string val = Attributes["size"];
				if (val == null)
					return -1;
				else
					return Convert.ToInt32(val);
			}
			set
			{
				if (value == -1)
					Attributes.Remove("size");
				else
					Attributes["size"] = value.ToString();
			}
		}
				
#warning TODO
		private UploadStorageConfig _StorageConfig;
		public UploadStorageConfig StorageConfig
		{
			get
			{
				if (_StorageConfig == null)
				{
					// Keep the storage config associated with the previous upload, if any
					if (Files.Count > 0 && !IsDesignTime && HttpContext.Current != null)
					{
						string secureStorageConfig = HttpContext.Current.Request.Form[FormContext.Current.Translator.FileIDToConfigID(UniqueID)];
						if (secureStorageConfig != null)
						{
							_StorageConfig = UploadStorage.CreateUploadStorageConfig();
							_StorageConfig.Unprotect(secureStorageConfig);
						}
					}
					else
					{
						_StorageConfig = UploadStorage.CreateUploadStorageConfig();
					}
				}
				return _StorageConfig;
			}
		}

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			this.Load += new System.EventHandler(this.Control_Load);
		}
				
		private void Control_Load(object sender, EventArgs e)
		{
			if (IsDesignTime)
				return;
			
			// If we can find the containing HtmlForm control, set enctype="multipart/form-data" method="Post".
			// If we can't find it, the page might be using some other form control or not using runat="server",
			// so we assume the developer has already set the enctype and method attributes correctly.
			Control c = Parent;
			while (c != null && !(c is HtmlForm))
			{
				c = c.Parent;
			}
			HtmlForm form = c as HtmlForm;
			if (form != null)
			{
				form.Enctype = "multipart/form-data";
				form.Method = "post";
			}
		}

		// This is used to ensure that the browser gets the latest SWFUpload.js each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when SWFUpload.js changes,
		// but computing a hash on that file everytime this assembly is loaded strikes me as overkill.
		private static Guid CacheBustingGuid = System.Guid.NewGuid();

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
			if (!IsDesignTime && Config.Current.UseHttpModule)
			{
				// Generate a special name recognized by the UploadHttpModule
				name = FormContext.Current.GenerateFileID(this.UniqueID);
				storageConfigName = FormContext.Current.GenerateStorageConfigID(this.UniqueID);

#warning TODO: Do not use Flash on Linux Firefox because it is currently unstable (i.e. crashes FF).
				this.Page.RegisterStartupScript("NeatUploadMultiFile-" + this.UniqueID, @"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadMultiFileCreate('" + this.ClientID + @"', 
		'" + FormContext.Current.PostBackID + @"',
		'" + AppPath + @"',
		'" + AppPath + @"/NeatUpload/AsyncUpload.aspx',
		'" + Config.Current.PostBackIDQueryParam + @"',
		{" + Config.Current.PostBackIDQueryParam + @" : '" + FormContext.Current.PostBackID + @"',
		 NeatUpload_AsyncControlID : '" + this.ClientID + @"'});
// -->
</script>");
			}
			else
			{
				name = this.UniqueID;
				storageConfigName = UploadContext.ConfigNamePrefix + "-" + this.UniqueID;
			}

			// Store the StorageConfig in a hidden form field with a related name
			if (StorageConfig != null && StorageConfig.Count > 0)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
				writer.AddAttribute(HtmlTextWriterAttribute.Name, storageConfigName);
				
				writer.AddAttribute(HtmlTextWriterAttribute.Value, StorageConfig.Protect());				
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

			if (Config.Current.UseHttpModule)
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
			return (Files.Count > 0);
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
		/// Fired when a file is uploaded to this control.</summary>
		public event System.EventHandler FileUploaded;
	}
}
