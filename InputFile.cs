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
using System.Web.UI.Design;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

[assembly: TagPrefix("Brettle.Web.NeatUpload", "Upload")]
namespace Brettle.Web.NeatUpload
{
	[ToolboxData("<{0}:InputFile runat='server'/>"),
	 ValidationProperty("ValidationFileName")]
	public class InputFile : System.Web.UI.WebControls.WebControl, System.Web.UI.IPostBackDataHandler
	{

		private bool IsDesignTime = (HttpContext.Current == null);

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
							// Copy the posted file to an UploadedFile.
							// We use a temporary UploadContext so that we have something we can pass to the
							// UploadStorageProvider.  Note that unlike when the UploadHttpModule is used,
							// this temporary context is not shared between uploaded files.
							UploadContext ctx = new UploadContext();
							ctx.SetContentLength(this.Page.Request.ContentLength);
							ctx.Status = UploadStatus.InProgress;
							_file = UploadStorage.CreateUploadedFile(ctx, this.UniqueID, postedFile.FileName, postedFile.ContentType);
							Stream outStream = null, inStream = null;
 							try
							{
								outStream = _file.CreateStream();
								inStream = postedFile.InputStream;
								byte[] buf = new byte[4096];
								int bytesRead = -1;
								while (outStream.CanWrite && inStream.CanRead 
									   && (bytesRead = inStream.Read(buf, 0, buf.Length)) > 0)
								{
									outStream.Write(buf, 0, bytesRead);
									ctx.BytesRead += bytesRead;
								}
								ctx.BytesRead = ctx.ContentLength;
								ctx.Status = UploadStatus.Completed;
							}
							finally
							{
								if (inStream != null) inStream.Close();
								if (outStream != null) outStream.Close();
							}
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
			get { return (HasFile ? file.ContentLength : 0); }
		}

		public Stream FileContent
		{
			get { return (HasFile ? file.OpenRead() : null); }
		}

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
			if (!IsDesignTime && Config.Current.UseHttpModule)
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
