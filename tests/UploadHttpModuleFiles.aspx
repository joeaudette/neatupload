<%@ Page language="c#" Src="UploadHttpModuleFiles.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.UploadHttpModuleFiles" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>

<Html>
	<Head runat="server">
		<Title>Test of UploadHttpModule.Files Property</Title>
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
			<Upload:HiddenPostBackID id="hiddenPostBackID" runat="server" />
			<h1>Test of UploadHttpModule.Files Property</h1>
			<p>
			This page tests the UploadHttpModule.Files property.  It should return an UploadedFileCollection of 
			UploadedFile objects, which corresponds to all the files that were uploaded, not just the ones that 
			correspond to NeatUpload's InputFile control.  Using UploadHttpModule.Files, the developer can 
			access files uploaded via standard &lt;input type="file"&gt; elements, even if those elements
			are dynamically generated by via client-side JavaScript.
			</p>
			<p>
			Files that NeatUpload streams to storage are not available via the standard ASP.NET Request.Files
			property.  Instead, they can be accessed via the UploadHttpModule.Files property.
			By default, NeatUpload will only stream to storage only those files associated with NeatUpload controls.
			Other files will be processed by ASP.NET and will only cause any ProgressBars to update if they
			happen to occur after a file that NeatUpload has streamed.  To tell NeatUpload to
			stream the other files, you can put a HiddenPostBackID control in your form before any of the
			&lt;input type="file"&gt; elements.
			</p>
			<p>
			This form is currently 
			<%= hiddenPostBackID.Visible ? "using" : "not using" %>
			a HiddenPostBackID control.  You can 
			<a id="toggleHiddenPostBackIDLink" runat="server">
				switch to a version of the form which
				<%= hiddenPostBackID.Visible ? "does not" : "does" %>
			</a>.
			</p>
			<p>
			Now select some files and click Submit.
			</p>
			<p>
			Pick a file using a standard &lt;input type="file"&gt; element:
				<input type="file" name="inputFile"/><br />
			Pick a file using using NeatUpload's InputFile control:
				<Upload:InputFile id="inputFile2" runat="server" /><br />
			Pick a file using a standard &lt;input type="file"&gt; element:
				<input type="file" name="inputFile3"/><br />
			Pick a file using using NeatUpload's InputFile control:
				<Upload:InputFile id="inputFile4" runat="server" /><br />

			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</p>

			<p>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton" />
			</p>

			<div runat="server" id="uploadedFilesDiv">
			<h2>UploadHttpModule.Files</h2>
			<pre id="uploadHttpModuleFilesPre" runat="server">
			</pre>
			<h2>Request.Files</h2>
			<pre id="requestFilesPre" runat="server">
			</pre>
			</div>
		</form>
	</Body>
</Html>