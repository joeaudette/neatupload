<%@ Page language="c#" AutoEventWireup="true" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<script runat="server">

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			submitButton.Click += new System.EventHandler(this.submitButton_Click);
		}


		private void submitButton_Click(object sender, EventArgs e)
		{
			if (this.IsValid)
			{
				bodyPre.InnerText = "";
				if (inputFile.HasFile)
				{
					bodyPre.InnerText += "InputFile\n";
					bodyPre.InnerText += "Name: " + inputFile.FileName + "\n";
					bodyPre.InnerText += "Size: " + inputFile.ContentLength + "\n";
					bodyPre.InnerText += "Content type: " + inputFile.ContentType + "\n";
					string destPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), inputFile.FileName); 
					inputFile.MoveTo(destPath, Brettle.Web.NeatUpload.MoveToOptions.Overwrite);
					if (inputFile.ContentType.StartsWith("text/"))
					{
						System.IO.StreamReader r = new System.IO.StreamReader(inputFile.FileContent);
						bodyPre.InnerText += r.ReadToEnd();
						r.Close();
					}
				}
				if (htmlInputFile.PostedFile != null 
					&& htmlInputFile.PostedFile.FileName != null
					&& htmlInputFile.PostedFile.FileName.Length > 0)
				{
					bodyPre.InnerText += "HtmlInputFile\n";
					bodyPre.InnerText += "Name: " + htmlInputFile.PostedFile.FileName + "\n";
					bodyPre.InnerText += "Size: " + htmlInputFile.PostedFile.ContentLength + "\n";
					bodyPre.InnerText += "Content type: " + htmlInputFile.PostedFile.ContentType + "\n";
					string destPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), htmlInputFile.PostedFile.FileName); 
					htmlInputFile.PostedFile.SaveAs(destPath);
					if (htmlInputFile.PostedFile.ContentType.StartsWith("text/"))
					{
						System.IO.StreamReader r = new System.IO.StreamReader(htmlInputFile.PostedFile.InputStream);
						bodyPre.InnerText += r.ReadToEnd();
						r.Close();
					}
				}
			}
		}
</script>
<Html>
	<Head runat="server">
		<Title>NeatUpload Demo</Title>
	</Head>
	<Body>
		<form id="uploadForm" runat="server">
			<Upload:InputFile id="inputFile" runat="server" /><br/>
			<input id="htmlInputFile" type="file" runat="server" /><br/>
			<asp:Button id="submitButton" runat="server" Text="Submit"/><br/>
			<pre id="bodyPre" runat="server">
			</pre>
		</form>
	</Body>
</Html>
