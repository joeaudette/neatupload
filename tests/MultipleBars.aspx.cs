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
	public class MultipleBars : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected InputFile inputFile1;
		protected Button submitButton1;
		protected Button cancelButton1;
		protected ProgressBar inlineProgressBar1;
		protected InputFile inputFile2;
		protected Button submitButton2;
		protected Button cancelButton2;
		protected ProgressBar inlineProgressBar2;
		protected Button submitButton3;
		protected HtmlGenericControl bodyPre;
		
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
			submitButton1.Click += new System.EventHandler(this.Button_Clicked);
			submitButton2.Click += new System.EventHandler(this.Button_Clicked);
			submitButton3.Click += new System.EventHandler(this.Button_Clicked);
		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			if (!this.IsValid)
			{
				bodyPre.InnerText = "Page is not valid!";
				return;
			}
			bodyPre.InnerText = "";
			if (inputFile1.HasFile)
			{
				/* 
					In a real app, you'd do something like:
					inputFile.MoveTo(Path.Combine(Request.PhysicalApplicationPath, inputFile.FileName), 
									 MoveToOptions.Overwrite);
				*/
				bodyPre.InnerText += "File #1:\n"; 
				bodyPre.InnerText += "  Name: " + inputFile1.FileName + "\n";
				bodyPre.InnerText += "  Size: " + inputFile1.ContentLength + "\n";
				bodyPre.InnerText += "  Content type: " + inputFile1.ContentType + "\n";
			}
			if (inputFile2.HasFile)
			{
				/* 
					In a real app, you'd do something like:
					inputFile2.MoveTo(Path.Combine(Request.PhysicalApplicationPath, inputFile2.FileName), 
									  MoveToOptions.Overwrite);
				*/
				bodyPre.InnerText += "File #2:\n"; 
				bodyPre.InnerText += "  Name: " + inputFile2.FileName + "\n";
				bodyPre.InnerText += "  Size: " + inputFile2.ContentLength + "\n";
				bodyPre.InnerText += "  Content type: " + inputFile2.ContentType + "\n";
			}
		}

	}
}
