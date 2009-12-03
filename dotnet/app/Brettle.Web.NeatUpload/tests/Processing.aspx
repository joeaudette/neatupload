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
				<li><a id="link1" onclick="UpdateUrlsAndDisplayProgress()" href="Processing.aspx?NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %>&processing=true">Processing.aspx?NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %>&processing=true</a></li>
				<li><a id="link2" onclick="UpdateUrlsAndDisplayProgress()" href="Processing.aspx?processing=true&NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %>">Processing.aspx?processing=true&NeatUpload_PostBackID=<%= inlineProgressBar.PostBackID %></a></li>
			</ul>
		The following link should not work because there is no postback ID query param:
			<ul>
				<li><a id="link3" onclick="UpdateUrlsAndDisplayProgress()" href="Processing.aspx?processing=true">Processing.aspx?processing=true</a></li>
			</ul>
		
		</p>
		<script type="text/javascript">
		function UpdateUrlsAndDisplayProgress()
		{
			var inlineProgressBar = document.getElementById("inlineProgressBar");
			var nuf = NeatUploadForm.prototype.GetFor(inlineProgressBar, '<%= inlineProgressBar.PostBackID %>');
			var link1 = document.getElementById("link1");
			var link2 = document.getElementById("link2");
			var link3 = document.getElementById("link3");
			link1.href = nuf.ChangePostBackIDInUrl(link1.href, "NeatUpload_PostBackID");
			link2.href = nuf.ChangePostBackIDInUrl(link2.href, "NeatUpload_PostBackID");
			link3.href = nuf.ChangePostBackIDInUrl(link3.href, "NeatUpload_PostBackID");
			NeatUploadPB.prototype.Bars['inlineProgressBar'].Display();
		}
		</script>
		<form id="uploadForm" runat="server">
			<a href="javascript:NeatUploadConsole.open('Console opened')">Show NeatUpload Console (for debugging)</a>
			<p>
			You can optionally select a file to upload:
			</p>
			<p>
			Pick a file:
				<Upload:InputFile id="inputFile" runat="server" /><br />
			</p>
			<p>
			This page has EnableSessionState="false" so that the session isn't locked by this page while the
			ProgressBar is trying to use the session to update the display.  <b>Note: That means that if you are using
			a web garden or web farm or the SessionBasedUploadStateStoreProvider, you will need to establish a
			session by visiting a different page in order for the ProgressBar below to work.</b><br />
			
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</p>

			<p>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton" AutoStartCondition="true" />
			</p>

			Files just uploaded:
			<pre id="uploadedFilePre" runat="server">
			</pre>
		</form>
	</Body>
</Html>
