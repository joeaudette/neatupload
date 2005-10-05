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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.IO;

namespace Brettle.Web.NeatUpload
{
	public class Demo : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected InputFile inputFile;
		protected InputFile inputFile2;
		protected Button submitButton;
		protected LinkButton linkButton;
		protected Button cancelButton;
		protected LinkButton cancelLinkButton;
		protected HtmlGenericControl bodyPre;
		protected ProgressBar progressBar;
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
			linkButton.Click += new System.EventHandler(this.Button_Clicked);

/*
			// Instead of setting the NonUploadButtons attribute of the 
			// ProgressBar element in the aspx file, you can put lines like
			// the following in your code-behind:
			progressBar.AddNonUploadButton(cancelButton);
			inlineProgressBar.AddNonUploadButton(cancelLinkButton);

			// For compatibility with previous versions of NeatUpload, the
			// following method still works but is deprecated:
			progressBar.AddTrigger(submitButton);
			progressBar.AddTrigger(linkButton);
			inlineProgressBar.AddTrigger(submitButton);
			inlineProgressBar.AddTrigger(linkButton);
*/

		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			if (!this.IsValid)
			{
				bodyPre.InnerText = "Page is not valid!";
				return;
			}
			bodyPre.InnerText = "";
			if (inputFile.TmpFile != null)
			{
				/* 
					In a real app, you'd do something like:
						inputFile.TmpFile.MoveTo(inputFile.FileName);
				*/
				bodyPre.InnerText += "File #1:\n"; 
				bodyPre.InnerText += "  Name: " + inputFile.FileName + "\n";
				bodyPre.InnerText += "  Size: " + inputFile.TmpFile.Length + "\n";
				bodyPre.InnerText += "  Content type: " + inputFile.ContentType + "\n";
			}
			if (inputFile2.TmpFile != null)
			{
				/* 
					In a real app, you'd do something like:
						inputFile.TmpFile.MoveTo(inputFile.FileName);
				*/
				bodyPre.InnerText += "File #2:\n"; 
				bodyPre.InnerText += "  Name: " + inputFile2.FileName + "\n";
				bodyPre.InnerText += "  Size: " + inputFile2.TmpFile.Length + "\n";
				bodyPre.InnerText += "  Content type: " + inputFile2.ContentType + "\n";
			}
		}

	}
}
