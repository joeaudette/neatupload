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
	public class InputFile : System.Web.UI.WebControls.WebControl, System.Web.UI.IPostBackDataHandler
	{
		private UploadedFile file;
		
		public FileInfo TmpFile {
			get { return (file != null && file.IsUploaded) ? file.TmpFile : null; }
		}
		
		public string FileName {
			get { return (file != null && file.IsUploaded) ? file.FileName : null; }
		}

		public string ContentType {
			get { return (file != null && file.IsUploaded) ? file.ContentType : null; }
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
			
			// This really belongs in LoadPostData, but MS ASP.NET only calls LoadPostData if the request
			// contains a name matching the name of our control.  Since we filter out that part of the request
			// ASP.NET doesn't see it and doesnt' call LoadPostData.
			if (this.Page.IsPostBack)
			{
				if (UploadHttpModule.IsEnabled)
				{
					file = UploadContext.Current.GetUploadedFile(this.UniqueID);
				}
				else
				{
					HttpPostedFile postedFile = Context.Request.Files[this.UniqueID];
					if (postedFile != null)
					{
						file = new UploadedFile(postedFile.FileName, postedFile.ContentType);
						postedFile.SaveAs(file.TmpFile.FullName);
					}
				}
			}
		}
				
		protected override void Render(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "file");
			string name; 
			if (UploadHttpModule.IsEnabled)
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
/*
			writer.AddAttribute(HtmlTextWriterAttribute.Type, "hidden");
			writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
			writer.AddAttribute(HtmlTextWriterAttribute.Value, "dummy");
			writer.RenderBeginTag(HtmlTextWriterTag.Input);
			writer.RenderEndTag();
*/
		}

		public virtual bool LoadPostData(string postDataKey, NameValueCollection postCollection)
		{		
			return true;
		}
		
		public virtual void RaisePostDataChangedEvent()
		{
			/*  See comment in Control_Load for why this is commented out
			if (FileUploaded != null)
			{
				FileUploaded(this, EventArgs.Empty);
			}
			*/
		}
		
		// public event System.EventHandler FileUploaded;
	}
}
