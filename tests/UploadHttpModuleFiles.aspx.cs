/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005,2006  Dean Brettle

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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.IO;

namespace Brettle.Web.NeatUpload
{
	public class UploadHttpModuleFiles : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected HiddenPostBackID hiddenPostBackID;
		protected HtmlAnchor toggleHiddenPostBackIDLink;
		protected InputFile inputFile2;
		protected Button submitButton;
		protected Button cancelButton;
		protected HtmlGenericControl uploadHttpModuleFilesPre;
		protected HtmlGenericControl requestFilesPre;
		protected HtmlGenericControl uploadedFilesDiv;
		protected ProgressBar inlineProgressBar;
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			this.Load += new System.EventHandler(this.Page_Load);
		}
		
		private void Page_Load(object sender, EventArgs e)
		{
			submitButton.Click += new System.EventHandler(this.Button_Clicked);
			toggleHiddenPostBackIDLink.ServerClick += new System.EventHandler(this.ToggleHiddenPostBackID_Clicked);
			uploadedFilesDiv.Visible = IsPostBack;
		}

		private void ToggleHiddenPostBackID_Clicked(object sender, EventArgs e)
		{
			hiddenPostBackID.Visible = !hiddenPostBackID.Visible;
		}
		
		private void Button_Clicked(object sender, EventArgs e)
		{
			uploadHttpModuleFilesPre.InnerText = "";
			for (int i=0; i < UploadHttpModule.Files.Count; i++)
			{
				UploadedFile file = UploadHttpModule.Files[i];
				uploadHttpModuleFilesPre.InnerText += "UploadHttpModule.Files[" + i + "]:\n"; 
				uploadHttpModuleFilesPre.InnerText += "  Name: " + file.FileName + "\n";
				uploadHttpModuleFilesPre.InnerText += "  Size: " + file.ContentLength + "\n";
				uploadHttpModuleFilesPre.InnerText += "  Content type: " + file.ContentType + "\n";
			}
			requestFilesPre.InnerText = "";
			for (int i=0; i < Request.Files.Count; i++)
			{
				HttpPostedFile file = Request.Files[i];
				requestFilesPre.InnerText += "Request.Files[" + i + "]:\n"; 
				requestFilesPre.InnerText += "  Name: " + file.FileName + "\n";
				requestFilesPre.InnerText += "  Size: " + file.ContentLength + "\n";
				requestFilesPre.InnerText += "  Content type: " + file.ContentType + "\n";
			}
		}
	}
}
