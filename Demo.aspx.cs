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
using log4net;

namespace Brettle.Web.NeatUpload
{
	public class Demo : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected InputFile inputFile;
		protected Button submitButton;
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
			this.PreRender += new System.EventHandler(this.Page_PreRender);
		}
		
		private void Page_Load(object sender, EventArgs e)
		{
			// ProgressBar.AddTrigger adds script to the page which causes the
			// progress bar to start updating when the specified button is 
			// clicked.  AddTrigger should be called no earlier than page
			// Load and no later than page Render.
			progressBar.AddTrigger(submitButton);
			inlineProgressBar.AddTrigger(submitButton);
		}

		private void Page_PreRender(object sender, EventArgs e)
		{
			if (this.IsPostBack)
			{
				if (inputFile.TmpFile != null)
				{
					/* 
						In a real app, you'd do something like:
							inputFile.TmpFile.MoveTo(inputFile.FileName);
					*/
					bodyPre.InnerText = "Name: " + inputFile.FileName + "\n";
					bodyPre.InnerText += "Size: " + inputFile.TmpFile.Length + "\n";
					bodyPre.InnerText += "Content type: " + inputFile.ContentType + "\n"; 
				}
			}
		}

	}
}
