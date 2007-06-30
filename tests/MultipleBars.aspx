<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#" Src="MultipleBars.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.MultipleBars" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<html>
	<head runat="server">
		<title>NeatUpload Test of Multiple Progress Bars</title>
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
			<h1>Multiple Progress Bars Test</h1>
			<p>
			This is a test of having multiple ProgressBars in the same form but with different triggers. 
			</p>
			<ul>
				<li>Each Submit button should start it's associated ProgressBar and the Common ProgressBar
				and the and all files should be uploaded.</li>
				<li>Each Cancel button should clear all filenames and no ProgressBars should start.</li>
				<li>The Trigger All button should start all ProgressBars and all files should be uploaded</li>
			</ul>
			<p>
			File #1: <Upload:InputFile id="inputFile1" runat="server" />
			<asp:Button id="submitButton1" runat="server" Text="Submit" />
			<asp:Button id="cancelButton1" runat="server" Text="Cancel" CausesValidation="False"/>
			<Upload:ProgressBar id="inlineProgressBar1" runat="server" inline="true" Triggers="submitButton1 submitButton3"/>
			<br />
			File #2: <Upload:InputFile id="inputFile2" runat="server" />
			<asp:Button id="submitButton2" runat="server" Text="Submit" />
			<asp:Button id="cancelButton2" runat="server" Text="Cancel" CausesValidation="False"/>
			<Upload:ProgressBar id="inlineProgressBar2" runat="server" inline="true" Triggers="submitButton2 submitButton3" />
			<br />
			<asp:Button id="submitButton3" runat="server" Text="Trigger All" />
			<br/>
			Common ProgressBar:	<Upload:ProgressBar id="inlineProgressNoTriggers" runat="server" inline="true" />
			<br />
			</p>

			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</body>
</html>
