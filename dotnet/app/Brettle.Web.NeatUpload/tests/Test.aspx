<%@ Reference Control="TestControl.ascx" %>
<%@ Page language="c#" AutoEventWireup="true" %>
<%@ Register TagPrefix="Upload" TagName="TestControl" Src="./TestControl.ascx" %>
<script runat="server">

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			testControl.submitButton.Click += new System.EventHandler(this.submitButton_Click);
		}

		private string myInputFileConfig = null;
		private string myMultiFileConfig = null;
		private void Page_Load(object sender, EventArgs e)
		{
			// Make sure changing the ID of the user control (or master page) from Page_Load()
			// doesn't cause files to be lost.
			testControl.ID = "myControl";
			bodyPreFromPageLoad.InnerText = GetUploadResultText();

			// StorageConfig should be maintained as well.
			myInputFileConfig = testControl.inputFile.StorageConfig["myConfig"];
			testControl.inputFile.StorageConfig["myConfig"] = "inputFile-" + DateTime.Now.ToString();
			myMultiFileConfig = testControl.multiFile.StorageConfig["myConfig"];
			testControl.multiFile.StorageConfig["myConfig"] = "multiFile-" + DateTime.Now.ToString();
		}

		private void submitButton_Click(object sender, EventArgs e)
		{
			if (this.IsValid)
			{
				bodyPreFromSubmitButtonClick.InnerText = GetUploadResultText();
			}
		}
	
		private string GetUploadResultText()
		{
			string text = "";
			if (testControl.inputFile.HasFile)
			{
				text += "--InputFile--\n";
				text += "StorageConfig: " + myInputFileConfig +"\n";
				text += "Name: " + testControl.inputFile.FileName + "\n";
				text += "Size: " + testControl.inputFile.ContentLength + "\n";
				text += "Content type: " + testControl.inputFile.ContentType + "\n";
			}
			if (testControl.multiFile.Files.Length > 0)
			{
				text += "--MultiFile--\n";
				text += "StorageConfig: " + myMultiFileConfig + "\n";
				for (int i = 0; i < testControl.multiFile.Files.Length; i++)
				{
					Brettle.Web.NeatUpload.UploadedFile file = testControl.multiFile.Files[i];
					text += "Number: " + i + "\n";
					text += "Name: " + file.FileName + "\n";
					text += "Size: " + file.ContentLength + "\n";
					text += "Content type: " + file.ContentType + "\n";
				}
			}
			if (testControl.htmlInputFile.PostedFile != null && testControl.htmlInputFile.PostedFile.FileName != null
				&& testControl.htmlInputFile.PostedFile.FileName.Length > 0)
			{
				text += "--HtmlInputFile--\n";
				text += "Name: " + testControl.htmlInputFile.PostedFile.FileName + "\n";
				text += "Size: " + testControl.htmlInputFile.PostedFile.ContentLength + "\n";
				text += "Content type: " + testControl.htmlInputFile.PostedFile.ContentType + "\n";
			}
			return text;
		}
</script>
<Html>
	<Head runat="server">
		<Title>NeatUpload Demo</Title>
		<style type="text/css">
<!--
		.ProgressBar {
			margin: 0px;
			border: 0px;
			padding: 0px;
			width: 100%;
			height: 2em;
		}
-->
		</style>
	</Head>
	<Body>
		<form id="uploadForm" runat="server">
		<Upload:TestControl id="testControl" runat="server" /><br />
			<h3>From Page_Load (before StorageConfig retrieved):</h3>
			<pre id="bodyPreFromPageLoad" runat="server">
			
			</pre>
			<h3>From submitButton_Click:</h3>
			<pre id="bodyPreFromSubmitButtonClick" runat="server">
			
			</pre>
		</form>
	</Body>
</Html>
