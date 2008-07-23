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
	/// File upload control that can be used with the <see cref="UploadHttpModule"/> and <see cref="ProgressBar"/>.
	/// </summary>
	/// <remarks>
	/// On post back, you can use <see cref="HasFile"/> to determine whether a file has been uploaded and use
	/// <see cref="FileName"/>, <see cref="ContentType"/>, <see cref="ContentLength"/>, <see cref="FileContent"/>
	/// to access the file's name, MIME type, length, and contents.  If you want to save the file for use after
	/// the current request, use the <see cref="MoveTo"/> method.
	/// This control will function even if the <see cref="UploadHttpModule"/> is not being used.  In that case,
	/// its methods/properties act on the file in the standard ASP.NET <see cref="HttpRequest.Files"/> collection.
	/// </remarks>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[ValidationProperty("ValidationFileName")]
	public class InputFile : System.Web.UI.WebControls.WebControl, System.Web.UI.IPostBackDataHandler
	{

		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/
		
		private bool IsDesignTime = (HttpContext.Current == null);

		private UploadedFile _file = null;
		private UploadedFile file
		{
			get 
			{
				if (_file == null && UniqueID != null)
				{
					if (IsDesignTime) return null;
					_file = UploadHttpModule.Files[this.UniqueID];
				}
				return _file;
			}
		}
		
		/// <summary>
		/// The <see cref="UploadedFile"/> corresponding to the file uploaded to this control. </summary>
		/// <remarks>
		/// Derived classes can use this to access the <see cref="UploadedFile"/> object that was created by the
		/// UploadStorageProvider.</remarks>
		protected UploadedFile File { get { return file; } }
		
		[Obsolete("This property is obsolete and will be removed in a future version.  Instead use other members like InputFile.MoveTo(), InputFile.HasFile, and InputFile.ContentLength.")]
		[Browsable(false)]
		public FileInfo TmpFile {
			get { return HasFile ? file.TmpFile : null; }
		}
		
		/// <summary>
		/// Whether a file was uploaded using this control. </summary>
		/// <remarks>
		/// HasFile is true if a file was uploaded during the last postback, otherwise false.</remarks>
		[Browsable(false)]
		public bool HasFile {
			get { return (file != null && file.IsUploaded); }
		}

		/// <summary>
		/// Client-side name of the uploaded file. </summary>
		/// <remarks>
		/// FileName is the name (not the full path) of the uploaded file on the user's machine if a file was
		/// uploaded during the last postback, otherwise it is null.</remarks>
		[Browsable(false)]
		public string FileName {
			get { return HasFile ? file.FileName : null; }
		}

		/// <summary>
		/// Client-side name of the uploaded file for validation purposes. </summary>
		/// <remarks>
		/// ValidationFileName is the same as <see cref="FileName"/> if a file was uploaded during the last postback.
		/// However, if no file was uploaded, ValidationFileName is String.Empty while FileName is null.</remarks>
		[Browsable(false)]
		public string ValidationFileName {
			get { return (FileName != null ? FileName : String.Empty); }
		}

		/// <summary>
		/// Browser-specified MIME type of the uploaded file. </summary>
		/// <remarks>
		/// ContentType is browser-specified MIME type of the uploaded file if a file was
		/// uploaded during the last postback, otherwise it is null.  Note that different browsers determine
		/// the MIME type differently.  They might use the file's extension, the first few bytes of the file, or
		/// something else entirely to determine the MIME type.</remarks>
		[Browsable(false)]
		public string ContentType {
			get { return HasFile ? file.ContentType : null; }
		}

		/// <summary>
		/// Number of bytes in the uploaded file. </summary>
		/// <remarks>
		/// Number of bytes in the uploaded file if a file was uploaded during the last postback, 
		/// otherwise 0.</remarks>
		[Browsable(false)]
		public long ContentLength {
			get { return (HasFile ? file.ContentLength : 0); }
		}
		
		private Stream fileContent;
		
		/// <summary>
		/// A readable <see cref="Stream"/> on the uploaded file. </summary>
		/// <remarks>
		/// A readable <see cref="Stream"/> on the uploaded file if a file was uploaded during the last postback, 
		/// otherwise null.  Note that the <see cref="Stream"/> is opened when this property is first accessed and
		/// that stream becomes the permanent value of this property.  If you use this
		/// property and don't either close the stream or call <see cref="MoveTo"/> before the request ends you
		/// may get an exception when NeatUpload tries to delete the underlying temporary storage at the end of the
		/// request.
		/// </remarks>
		[Browsable(false)]
		public Stream FileContent
		{
			get
			{
				if (fileContent != null)
					return fileContent;
				if (HasFile)
					fileContent = file.OpenRead();
				return fileContent;
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
		
		
		private UploadStorageConfig _NewStorageConfig;
		private UploadStorageConfig _StorageConfig;
		public UploadStorageConfig StorageConfig
		{
			get
			{
				if (_StorageConfig == null)
				{
					// Keep the storage config associated with the previous upload, if any
					if (file != null && !IsDesignTime && HttpContext.Current != null)
					{
						string secureStorageConfig = HttpContext.Current.Request.Form[FormContext.Current.Translator.FileIDToConfigID(UniqueID)];
						if (secureStorageConfig != null)
						{
							_StorageConfig = UploadStorage.CreateUploadStorageConfig();
							_StorageConfig.Unprotect(secureStorageConfig);
							// Replace any values set before this control had a fully qualified name.
							if (_NewStorageConfig != null)
							{
								foreach (string key in _NewStorageConfig.AllKeys)
									_StorageConfig[key] = _NewStorageConfig[key];
							}
						}
					}
				}
				if (_StorageConfig != null)
					return _StorageConfig;
				if (_NewStorageConfig == null)
				{
					_NewStorageConfig = UploadStorage.CreateUploadStorageConfig();
				}
				return _NewStorageConfig;
			}
		}
		
			
			
					
		
		/// <summary>
		/// Moves an uploaded file to a permanent location.</summary>
		/// <param name="path">the permanent location to move the uploaded file to.</param>
		/// <param name="opts">options associated with moving the file (e.g. 
		/// <see cref="MoveToOptions.Overwrite">MoveToOptions.Overwrite</see> or 
		/// <see cref="MoveToOptions.None">MoveToOptions.None</see></param>
		/// <remarks>
		/// <para>
		/// The default <see cref="UploadStorageProvider"/> (a <see cref="FilesystemUploadStorageProvider"/>)
		/// temporarily stores uploaded files on disk.  If you don't call MoveTo() in response to the postback, the temporary file will be 
		/// automatically deleted.</para>
		/// <para>  
		/// The <paramref name="path"/> you pass to MoveTo() is the filesystem path where you want the uploaded file
		/// to be permanently moved.  If you want any existing file at that path to be overwritten, pass 
		/// <see cref="MoveToOptions.Overwrite">MoveToOptions.Overwrite</see> as the <paramref name="opts"/> 
		/// parameter.  Otherwise, pass <see cref="MoveToOptions.None">MoveToOptions.None</see>.
		/// Only the last call to MoveTo() in response to a particular postback will determine the uploaded file's
		/// permanent location.</para>
		/// <para>
		/// If you are using a non-default <see cref="UploadStorageProvider"/>, it might interpret 
		/// <paramref name="path"/> differently.  For example, it might use it as the primary key to identify a row 
		/// in a database table.  A non-default <see cref="UploadStorageProvider"/> might also allow other options
		/// by accepting a subclass of <see cref="MoveToOptions"/> for the <paramref name="opts"/> parameter.</para>
		/// </remarks>
		public virtual void MoveTo(string path, MoveToOptions opts)
		{
			file.MoveTo(path, opts);
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

		// This is used to ensure that the browser gets the latest NeatUpload.js each time this assembly is
		// reloaded.  Strictly speaking the browser only needs to get the latest when NeatUpload.js changes,
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
			string name;
			string storageConfigName;
			if (!IsDesignTime && Config.Current.UseHttpModule)
			{
				// Generate a special name recognized by the UploadHttpModule
				name = FormContext.Current.GenerateFileID(this.UniqueID);
				storageConfigName = FormContext.Current.GenerateStorageConfigID(this.UniqueID);

				this.Page.RegisterStartupScript("NeatUploadInputFile-" + this.UniqueID, @"
<script type='text/javascript' language='javascript'>
<!--
NeatUploadInputFileCreate('" + this.ClientID + @"','"
							 + FormContext.Current.PostBackID + @"');
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
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "file");
			writer.AddAttribute(HtmlTextWriterAttribute.Name, name);
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();

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
		}

		/// <summary>
		/// Called by ASP.NET so that controls can find and process their post back data</summary>
		/// <returns>the true if a file was uploaded with this control</returns>
		public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
		{		
			return HasFile;
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
