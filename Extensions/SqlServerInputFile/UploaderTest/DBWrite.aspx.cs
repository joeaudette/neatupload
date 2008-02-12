using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using Brettle.Web.NeatUpload;

namespace UploaderTest
{
    public class DBWrite : System.Web.UI.Page
    {
        protected System.Web.UI.HtmlControls.HtmlForm uploadForm;
        protected System.Web.UI.WebControls.DropDownList progressBarLocationDropDown;
        protected System.Web.UI.WebControls.DropDownList buttonTypeDropDown;
        protected Hitone.Web.SqlServerUploader.SqlServerInputFile inputFile;
        protected System.Web.UI.WebControls.RegularExpressionValidator RegularExpressionValidator1;
        protected Hitone.Web.SqlServerUploader.SqlServerInputFile inputFile2;
        protected System.Web.UI.HtmlControls.HtmlGenericControl submitButtonSpan;
        protected System.Web.UI.WebControls.Button submitButton;
        protected System.Web.UI.WebControls.Button cancelButton;
        protected System.Web.UI.HtmlControls.HtmlGenericControl commandButtonSpan;
        protected System.Web.UI.WebControls.Button commandButton;
        protected System.Web.UI.WebControls.Button cancelCommandButton;
        protected System.Web.UI.HtmlControls.HtmlGenericControl linkButtonSpan;
        protected System.Web.UI.WebControls.LinkButton linkButton;
        protected System.Web.UI.WebControls.LinkButton cancelLinkButton;
        protected System.Web.UI.HtmlControls.HtmlGenericControl bodyPre;
        protected System.Web.UI.HtmlControls.HtmlGenericControl inlineProgressBarDiv;
        protected Brettle.Web.NeatUpload.ProgressBar inlineProgressBar;
        protected System.Web.UI.HtmlControls.HtmlGenericControl popupProgressBarDiv;
        protected Brettle.Web.NeatUpload.ProgressBar progressBar;
        protected System.Web.UI.WebControls.Label label;

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
            submitButtonSpan.Visible = (buttonTypeDropDown.SelectedValue == "Button");
            linkButtonSpan.Visible = (buttonTypeDropDown.SelectedValue == "LinkButton");
            commandButtonSpan.Visible = (buttonTypeDropDown.SelectedValue == "CommandButton");

            inlineProgressBarDiv.Visible = (progressBarLocationDropDown.SelectedValue == "Inline");
            popupProgressBarDiv.Visible = (progressBarLocationDropDown.SelectedValue == "Popup");

            submitButton.Click += new System.EventHandler(this.Button_Clicked);
            linkButton.Click += new System.EventHandler(this.Button_Clicked);

            /*
                        // Instead of setting the Triggers property of the 
                        // ProgressBar element in the aspx file, you can put lines like
                        // the following in your code-behind:
                        progressBar.AddTrigger(submitButton);
                        progressBar.AddTrigger(linkButton);
                        inlineProgressBar.AddTrigger(submitButton);
                        inlineProgressBar.AddTrigger(linkButton);
            */

            /*
                        // The temp directory used by the default FilesystemUploadStorageProvider can be configured on a
                        // per-control basis like this (see documentation for details)
                        if (!IsPostBack)
                        {
                            inputFile.StorageConfig["tempDirectory"] = "file1temp";
                            inputFile2.StorageConfig["tempDirectory"] = "file2temp";
                        }
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
            if (inputFile.HasFile)
            {
                /* 
                    In a real app, you'd do something like:
                    inputFile.MoveTo(Path.Combine(Request.PhysicalApplicationPath, inputFile.FileName), 
                                     MoveToOptions.Overwrite);
                string filename = System.IO.Path.Combine("c:\\temp", inputFile.FileName);
                inputFile.MoveTo(filename, MoveToOptions.Overwrite);
                */
                
                // Test FileContent property by reading (but ignoring) the whole stream.
                System.IO.Stream content = inputFile.FileContent;
                int bytesToRead = (int)inputFile.ContentLength;
                byte[] buf = new byte[4096];
                while (bytesToRead > 0)
                {
                	bytesToRead 
                		-= content.Read(buf, inputFile.ContentLength - bytesToRead, 
                		                Math.Min(bytesToRead, buf.Length));
                }
                content.Close();
                
                inputFile.Verify();

                bodyPre.InnerHtml += "File #1:\n";
                bodyPre.InnerHtml += "  Name: " + inputFile.FileName + "<br />";
                bodyPre.InnerHtml += "  Size: " + inputFile.ContentLength + "<br />";
                bodyPre.InnerHtml += "  Content type: " + inputFile.ContentType + "<br />";
                bodyPre.InnerHtml += "  DB Identity: " + inputFile.Identity + "<br />";
                bodyPre.InnerHtml += "  Hash: " + ToHex(inputFile.Hash) + "<br />";
                bodyPre.InnerHtml += "  HashSize: " + inputFile.HashSize + "<br />";
                bodyPre.InnerHtml += "  HashName: " + inputFile.HashName + "<br />";
                bodyPre.InnerHtml += "  Download: <a href='DBRead.aspx?id=" + inputFile.Identity + "'>" + inputFile.FileName + "</a><br /><br />";
            }
            if (inputFile2.HasFile)
            {
                /* 
                    In a real app, you'd do something like:
                    inputFile2.MoveTo(Path.Combine(Request.PhysicalApplicationPath, inputFile2.FileName), 
                                      MoveToOptions.Overwrite);
                inputFile2.MoveTo(System.IO.Path.Combine("c:\\temp", inputFile2.FileName), MoveToOptions.Overwrite);
                */

                inputFile2.MoveTo("newname.txt", null);

                bodyPre.InnerHtml += "File #2:\n";
                bodyPre.InnerHtml += "  Name: " + inputFile2.FileName + "<br />";
                bodyPre.InnerHtml += "  Size: " + inputFile2.ContentLength + "<br />";
                bodyPre.InnerHtml += "  Content type: " + inputFile2.ContentType + "<br />";
                bodyPre.InnerHtml += "  DB Identity: " + inputFile2.Identity + "<br />";
                bodyPre.InnerHtml += "  Hash: " + ToHex(inputFile2.Hash) + "<br />";
                bodyPre.InnerHtml += "  HashSize: " + inputFile2.HashSize + "<br />";
                bodyPre.InnerHtml += "  HashName: " + inputFile2.HashName + "<br />";
                bodyPre.InnerHtml += "  Download: <a href='DBRead.aspx?id=" + inputFile2.Identity + "'>" + inputFile2.FileName + "</a>";
            }
        }

        /// <summary>
        /// Converts a byte array to a hexadecimal string
        /// </summary>
        /// <param name="bytes">The byte array to convert</param>
        /// <returns>A string containing the input arra in hexadecimal format</returns>
        /// <remarks>Mimics the System.BitConverter.ToString behaviour but without the dashes</remarks>
        public static string ToHex(byte[] bytes)
        {
        	if (bytes == null || bytes.Length == 0) return string.Empty;
        	return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
