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
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.IO;

namespace Brettle.Web.NeatUpload
{
	public class Processing : System.Web.UI.Page
	{	
		protected HtmlForm form;
		protected InputFile inputFile;
		protected Button submitButton;
		protected Button cancelButton;
		protected ProgressBar inlineProgressBar;
		protected HtmlGenericControl uploadedFilePre;
		protected HtmlGenericControl sessionPre;
		
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
			submitButton.Click += new System.EventHandler(this.Button_Clicked);
			if (Request.Params["processing"] == "true")
			{
				DoProcessing();
			}
		}
		
		private void Page_PreRender(object sender, EventArgs e)
		{
			UploadHttpModule.AccessSession(new SessionAccessCallback(InitializeSessionPre));
		}

		private void DoProcessing()
		{
			ProgressInfo progress = inlineProgressBar.ProcessingProgress = new ProgressInfo(5000, "Units");
			for (int i = 1; i <= 5000; i++)
			{
				System.Threading.Thread.Sleep(1);
				progress.Value = i;
			}
			progress.Text = "Processing Complete";
		}
			
		private void Button_Clicked(object sender, EventArgs e)
		{
			DoProcessing();
			uploadedFilePre.InnerText = "";
			if (inputFile.HasFile)
			{
				uploadedFilePre.InnerText += "  Name: " + inputFile.FileName + "\n";
				uploadedFilePre.InnerText += "  Size: " + inputFile.ContentLength + "\n";
				uploadedFilePre.InnerText += "  Content type: " + inputFile.ContentType + "\n";
				UploadHttpModule.AccessSession(new SessionAccessCallback(AddFileNameToSession));
			}
		}
		
		private void AddFileNameToSession(HttpSessionState session)
		{
			session["myUploadedFiles"] += inputFile.FileName + "\n";
		}

		private void InitializeSessionPre(HttpSessionState session)
		{
			sessionPre.InnerText = session["myUploadedFiles"] as string;
		}
		
	}
}
