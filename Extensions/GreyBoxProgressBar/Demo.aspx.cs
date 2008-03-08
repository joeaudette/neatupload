/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005-2008  Dean Brettle

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
	public class GreyBoxProgressBarDemo : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected InputFile inputFile;
		protected Button submitButton;
		protected Button cancelButton;
		protected HtmlGenericControl bodyPre;
		protected GreyBoxProgressBar progressBar;
		
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
		}

		private void Button_Clicked(object sender, EventArgs e)
		{
			if (!this.IsValid)
			{
				bodyPre.InnerText = "Page is not valid!";
				return;
			}
			bodyPre.InnerText = "";

			if (inputFile.HasFile)
			{
				/* 
					In a real app, you'd do something like:
					inputFile.MoveTo(Path.Combine(Request.PhysicalApplicationPath, inputFile.FileName), 
									 MoveToOptions.Overwrite);
				*/
				bodyPre.InnerText += "File #1:\n"; 
				bodyPre.InnerText += "  Name: " + inputFile.FileName + "\n";
				bodyPre.InnerText += "  Size: " + inputFile.ContentLength + "\n";
				bodyPre.InnerText += "  Content type: " + inputFile.ContentType + "\n";
			}
		}
	}
}
