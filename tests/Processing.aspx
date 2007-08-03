<%@ Page language="c#" EnableSessionState="false" Src="Processing.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.Processing" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>

<Html>
	<Head runat="server">
		<Title>Test of the ProcessingInProgress UploadStatus</Title>
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
		<h1>Test of the ProcessingInProgress UploadStatus</h1>
		<p>
		This page tests the "ProcessingInProgress" UploadStatus which is the status used while the page handler is 
		being executed.  This page simulates doing some processing and updates the progress bar periodically.
		</p>
		<p>
		The following links should demonstrate this without requiring a POST:
			<ul>
				<li><a onclick="NeatUploadPB.prototype.Bars['inlineProgressBar'].Display()" href="Processing.aspx?NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %>&processing=true">Processing.aspx?NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %>&processing=true</a></li>
				<li><a onclick="NeatUploadPB.prototype.Bars['inlineProgressBar'].Display()" href="Processing.aspx?processing=true&NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %>">Processing.aspx?processing=true&NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %></a></li>
			</ul>
		The following link should not work because there is no postback ID query param:
			<ul>
				<li><a onclick="NeatUploadPB.prototype.Bars['inlineProgressBar'].Display()" href="Processing.aspx?processing=true">Processing.aspx?processing=true</a></li>
			</ul>
		
		</p>
		<form id="uploadForm" runat="server">
			<Upload:HiddenPostBackID />
			<p>
			You can optionally select a file to upload:
			</p>
			<p>
			Pick a file:
				<Upload:InputFile id="inputFile" runat="server" /><br />
			</p>
			<p>
			This page has EnableSessionState="false" so that the session isn't locked by this page while the
			ProgressBar is trying to use the session to update the display.  To demonstrate that you can still
			access the session through UploadHttpModule.AccessSession(), the names of the file you upload will 
			be appended to a session variable which is displayed at the bottom of this page.<br />
			
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</p>

			<p>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton" AutoStartCondition="true" />
			</p>

			Files just uploaded:
			<pre id="uploadedFilePre" runat="server">
			</pre>
			
			Files uploaded this session:
			<pre id="sessionPre" runat="server">
			</pre>
		</form>
	</Body>
</Html>