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
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace Brettle.Web.NeatUpload
{
	[ValidationProperty("ValidationFileName")]
	public class InputFile : System.Web.UI.WebControls.WebControl, System.Web.UI.IPostBackDataHandler
	{

		private UploadedFile _file = null;
		private UploadedFile file
		{
			get 
			{
				if (_file == null)
				{
					if (Config.Current.UseHttpModule)
					{
						_file = UploadContext.Current.GetUploadedFile(this.UniqueID);
					}
					else
					{
						HttpPostedFile postedFile = Context.Request.Files[this.UniqueID];
						if (postedFile != null)
						{
							_file = new UploadedFile(this.UniqueID, postedFile.FileName, postedFile.ContentType);
							postedFile.SaveAs(file.TmpFile.FullName);
						}
					}
				}
				return _file;
			}
		}
		
		[Obsolete("This property is obsolete and will be removed in a future version.  Instead use other members like InputFile.MoveTo(), InputFile.HasFile, and InputFile.ContentLength.")]
		public FileInfo TmpFile {
			get { return (file != null && file.IsUploaded) ? file.TmpFile : null; }
		}
		
		public bool HasFile {
			get { return (file != null && file.IsUploaded); }
		}

		public string FileName {
			get { return (file != null && file.IsUploaded) ? file.FileName : null; }
		}

		public string ValidationFileName {
			get { return (FileName != null ? FileName : ""); }
		}

		public string ContentType {
			get { return (file != null && file.IsUploaded) ? file.ContentType : null; }
		}

		public long ContentLength {
			get { return (HasFile ? file.TmpFile.Length : 0); }
		}

		public Stream FileContent
		{
			get { return (HasFile ? file.TmpFile.OpenRead() : null); }
		}

		[Flags]
		public enum MoveToFlags
		{
			None = 0,
			Overwrite = 1
		}

		public virtual void MoveTo(string path, MoveToFlags flags)
		{
			if ((flags & MoveToFlags.Overwrite) != 0 && File.Exists(path))
			{
				File.Delete(path);
			}
			file.TmpFile.MoveTo(path);
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
			// Find the containing <form> tag and set enctype="multipart/form-data" method="Post"
			Control c = Parent;
			while (c != null && !(c is HtmlForm))
			{
				c = c.Parent;
			}
			HtmlForm form = c as HtmlForm;
			form.Enctype = "multipart/form-data";
			form.Method = "Post";
		}
				
		protected override void Render(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "file");
			string name; 
			if (Config.Current.UseHttpModule)
			{
				name = FormContext.Current.GenerateFileID(this.UniqueID);
			}
			else
			{
				name = this.UniqueID;
			}
			writer.AddAttribute(HtmlTextWriterAttribute.Name, name);
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();
		}

		public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
		{		
			return true;
		}
		
		public virtual void RaisePostDataChangedEvent()
		{
			if (FileUploaded != null)
			{
				FileUploaded(this, EventArgs.Empty);
			}
		}
		
		public event System.EventHandler FileUploaded;
	}
}
