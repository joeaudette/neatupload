<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#"  AutoEventWireup="false" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<html>
	<head runat="server">
		<title>NeatUpload Test of Inline ProgressBars in Opera</title>
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
	</head>
	<body>
		<form id="uploadForm" runat="server">
			<h1>Opera Inline ProgressBar Test</h1>
			<ul>
				<li>When using a browser other than Opera, the ProgressBar should appear inline and be 100% wide by 90px high.</li>
				<li>When using Opera, the ProgressBar should appear as a popup and be 500x100 pixels</li>
			</ul>
			<p>
			File: <Upload:InputFile id="inputFile1" runat="server" />
			<asp:Button id="submitButton1" runat="server" Text="Submit" />
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" width="100%" height="90px" Triggers="submitButton1"/>
			</p>
			<p>
			Note: For simplicity, this page doesn't display any info about the uploaded file.
			</p>
		</form>
	</body>
</html>
