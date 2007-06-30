<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#" Src="TriggerChildren.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.TriggerChildren" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<html>
	<head runat="server">
		<title>NeatUpload Test of Trigger Children</title>
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
			<h1>Trigger Children Test</h1>
			<p>
			This is a test of whether children of triggers start the progress display.  This situation happens 
			when a link button contains child elements (e.g. for additional formatting).  To test, confirm that the 
			"Start Upload Now" link works no matter which of the 3 words you click on.
			</p>
			<p>
			File: <Upload:InputFile id="inputFile" runat="server" />
			<asp:LinkButton id="submitButton" runat="server">Start <i>Upload <b>Now</b></i></asp:LinkButton>
			<asp:LinkButton id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/>
			</p>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" Triggers="submitButton"/>
			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</body>
</html>
