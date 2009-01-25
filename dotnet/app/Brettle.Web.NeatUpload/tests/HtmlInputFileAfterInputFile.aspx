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
				}
				if (htmlInputFile.PostedFile != null 
					&& htmlInputFile.PostedFile.FileName != null
					&& htmlInputFile.PostedFile.FileName.Length > 0)
				{
					bodyPre.InnerText += "HtmlInputFile\n";
					bodyPre.InnerText += "Name: " + htmlInputFile.PostedFile.FileName + "\n";
					bodyPre.InnerText += "Size: " + htmlInputFile.PostedFile.ContentLength + "\n";
					bodyPre.InnerText += "Content type: " + htmlInputFile.PostedFile.ContentType + "\n";
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
            <Upload:ProgressBar ID="ProgressBar1" runat='server' Inline="False" Triggers="submitButton">
            </Upload:ProgressBar>
			<asp:Button id="submitButton" runat="server" Text="Submit"/><br/>
			<pre id="bodyPre" runat="server">
			</pre>
		</form>
	</Body>
</Html>
