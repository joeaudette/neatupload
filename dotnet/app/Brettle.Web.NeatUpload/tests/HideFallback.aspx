<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#"  AutoEventWireup="false" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<html>
	<head runat="server">
		<title>NeatUpload Test of Fallback Text Hiding</title>
		<style type="text/css">
.ProgressBar {
	margin: 0px;
	border: 0px;
	padding: 0px;
	width: 100%;
	height: 2em;
}
		</style>
	</head>
	<body>
		<form id="uploadForm" runat="server">
			<h1>Hide Fallback Text Test</h1>
			<ul>
				<li>When Javascript is enabled, the fallback text should not be visible even while the page is loading.</li>
				<li>When Javascript is disable, the fallback text should be visible.</li>
			</ul>
			<p>
			File: <Upload:InputFile id="inputFile1" runat="server" />
			<asp:Button id="submitButton1" runat="server" Text="Submit" />
			</p>
			<Upload:ProgressBar id="popProgressBar" runat="server" inline="false" Triggers="submitButton1"/>
			<p>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton1"
				style="display:inline-block;background-color:GhostWhite;height:20px;width:165px;"/>
			<script language="javascript" type="text/javascript">
			
<!--
window.alert('Page loading - fallback text should not be visible');
// -->
			</script>
			<br />
			</p>
			<p>
			Note: For simplicity, this page doesn't display any info about the uploaded file.
			</p>
			<p>
			Page finished loading.
			</p>
		</form>
	</body>
</html>
