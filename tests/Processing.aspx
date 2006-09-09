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
		<form id="uploadForm" runat="server">
			<Upload:HiddenPostBackID />
			<h1>Test of the Processing UploadStatus</h1>
			<p>
			This page tests the "ProcessingInProgress" UploadStatus which is the status used while the page handler is 
			being executed.  This page simulates doing some processing and updates the progress bar periodically.
			</p>
			<p>
			You can (optionally) select some files to upload as well.
			</p>
			<p>
			Pick a file:
				<Upload:InputFile id="inputFile" runat="server" /><br />

			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</p>

			<p>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton" />
			</p>

			<pre id="uploadedFilePre" runat="server">
			</pre>
		</form>
	</Body>
</Html>
