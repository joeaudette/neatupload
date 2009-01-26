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


		private void submitButton_Click(object sender, EventArgs e)
		{
			if (this.IsValid)
			{
				if (testControl.inputFile.HasFile)
				{
					bodyPre.InnerText = "Name: " + testControl.inputFile.FileName + "\n";
					bodyPre.InnerText += "Size: " + testControl.inputFile.ContentLength + "\n";
					bodyPre.InnerText += "Content type: " + testControl.inputFile.ContentType + "\n";
/*
                    string destPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), testControl.inputFile.FileName); 
					testControl.inputFile.MoveTo(destPath, Brettle.Web.NeatUpload.MoveToOptions.Overwrite);
					if (testControl.inputFile.ContentType.StartsWith("text/"))
					{
						System.IO.StreamReader r = new System.IO.StreamReader(testControl.inputFile.FileContent);
						bodyPre.InnerText += r.ReadToEnd();
						r.Close();
					}
 */
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
		<Upload:TestControl id="testControl" runat="server" />
			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</Body>
</Html>
