<%@ Reference Control="TrickyTestControl.ascx" %>
<%@ Page language="c#" AutoEventWireup="true" %>
<%@ Register TagPrefix="Upload" TagName="TestControl" Src="./TrickyTestControl.ascx" %>
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


		private void submitButton_Click(object sender, EventArgs e)
		{
			if (this.IsValid)
			{
			    bodyPre.InnerText = "";
				foreach (Brettle.Web.NeatUpload.UploadedFile file in testControl.multiFile.Files)
				{
					bodyPre.InnerText += String.Format("{0}: {1}, {2} bytes\n", file.FileName, file.ContentType, file.ContentLength);
				}
			}
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
		<a href="javascript:NeatUploadConsole.open('Console opened')">Show NeatUpload Console (for debugging)</a>
		<Upload:TestControl id="testControl" runat="server" />
			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</Body>
</Html>
