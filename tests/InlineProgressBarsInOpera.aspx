<%@ Page language="c#"  AutoEventWireup="false" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>

<Html>
	<Head runat="server">
		<Title>NeatUpload Test of Inline ProgressBars in Opera</Title>
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
			<h1>Opera Inline ProgressBar Test</h1>
			<p>
			<ul>
				<li>When using a browser other than Opera, the ProgressBar should appear inline and be 100% wide by 90px high.</li>
				<li>When using Opera, the ProgressBar should appear as a popup and be 500x100 pixels</li>
			</ul>
			</p>
			<p>
			File: <Upload:InputFile id="inputFile1" runat="server" />
			<asp:Button id="submitButton1" runat="server" Text="Submit" />
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" width="100%" height="90px" Triggers="submitButton1"/>
			</p>
			<p>
			Note: For simplicity, this page doesn't display any info about the uploaded file.
			</p>
		</form>
	</Body>
</Html>
