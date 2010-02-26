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
	/// Base class for NeatUpload's file upload controls.
	/// </summary>
	[AspNetHostingPermissionAttribute (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermissionAttribute (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    public abstract class FileControl : System.Web.UI.WebControls.WebControl, System.Web.UI.IPostBackDataHandler
	{

#pragma warning disable 0169
		// Create a logger for use in this class
		private static readonly log4net.ILog log
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore 0169
		
		internal bool IsDesignTime = (HttpContext.Current == null);

		/// <summary>
		/// The array of <see cref="UploadedFile"/> objects corresponding to the files uploaded to this control. </summary>
		/// <remarks>
		/// Derived classes can use this to access the <see cref="UploadedFile"/> objects that were created by the
		/// UploadStorageProvider.</remarks>
		private UploadedFile[] _files = new UploadedFile[0];
	
		internal UploadedFile[] Files
		{
			get 
			{
                InitializeFiles();
				return _files;
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
					if (!IsDesignTime && Files != null  && Files.Length > 0 && HttpContext.Current != null)
					{
						string secureStorageConfig = HttpContext.Current.Request.Form[FormContext.Current.GenerateStorageConfigID(UniqueID)];
						if (secureStorageConfig != null)
						{
							_StorageConfig = UploadModule.CreateUploadStorageConfig();
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
				if (!IsDesignTime && _NewStorageConfig == null)
				{
					_NewStorageConfig = UploadStorage.CreateUploadStorageConfig();
				}
				return _NewStorageConfig;
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

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
        }

		protected override void OnLoad(EventArgs e)
		{
			InitializeForm();
			base.OnLoad(e);
		}

        private bool IsFilesInitialized = false;
		private void InitializeFiles()
		{
			if (IsDesignTime || IsFilesInitialized)
				return;
			// Initialize the Files property
			ArrayList fileArrayList = new ArrayList();
			if (UploadModule.IsEnabled)
			{
				UploadedFileCollection allFiles = UploadModule.Files;
				// Get only the files that were uploaded from this control
				for (int i = 0; allFiles != null && i < allFiles.Count; i++)
				{
					if (allFiles.GetKey(i) == this.UniqueID)
					{
						UploadedFile uploadedFile = allFiles[i];
						if (uploadedFile.IsUploaded)
							fileArrayList.Add(allFiles[i]);
					}
				}
			}
			else
			{
				HttpFileCollection allFiles = HttpContext.Current.Request.Files;
				// Get only the files that were uploaded from this control
				for (int i = 0; allFiles != null && i < allFiles.Count; i++)
				{
					if (allFiles.GetKey(i) == this.UniqueID)
					{
						UploadedFile uploadedFile 
								= UploadModule.ConvertToUploadedFile(this.UniqueID, (HttpPostedFile)allFiles[i]);
						if (uploadedFile == null)
							continue;
						if (uploadedFile.IsUploaded)
							fileArrayList.Add(uploadedFile);
						else
							uploadedFile.Dispose();
					}
				}
			}
			_files = new UploadedFile[fileArrayList.Count];
			Array.Copy(fileArrayList.ToArray(), _files, _files.Length);
            IsFilesInitialized = true;
		}

		private void InitializeForm()
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

        /// <summary>
        /// Called by ASP.NET so that controls can find and process their post back data</summary>
        /// <returns>the true if a file was uploaded with this control</returns>
        public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
        {
            InitializeFiles();
            return Files.Length > 0;
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


        protected override void OnUnload(EventArgs e)
		{
			if (Files != null)
				foreach (UploadedFile f in Files)
					f.Dispose();
			base.OnUnload(e);
		}	
	}
}
