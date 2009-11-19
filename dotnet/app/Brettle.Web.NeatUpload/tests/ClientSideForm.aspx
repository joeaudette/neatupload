<%@ Page language="c#"  AutoEventWireup="false" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>

<Html>
	<Head>
		<Title>NeatUpload Test of Client Side Forms</Title>
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
		<form enctype="multipart/form-data" method="Post">
			<h1>Client Side Form Test</h1>
			<p>
			The following InputFile and ProgressBar controls are in a &lt;form&gt; element that does not contain 
			runat="server" and does not contain an id attribute.
			Nonetheless, the submit button should still start the progress display and the cancel button should clear
			the filename and not start the progress display.
			</p>
			<p>
			File: <Upload:InputFile id="inputFile1" runat="server" />
			<input type="submit" id="submitButton1" value="Submit" />
			<input type="submit" id="cancelButton1" value="Cancel" />
			<Upload:ProgressBar id="popProgressBar" runat="server" inline="false" Triggers="submitButton1"/>
			</p>
			<p>
			Note: For simplicity, this page doesn't display any info about the uploaded file.
			</p>
		</form>

		<p>
		Also note: For the progress bar to display, the page must also contain a &lt;form&gt; element with 
		runat="server" after the the client-side form element.  This is needed so that ASP.NET will render the 
		JavaScript that	the ProgressBar control generates.
		</p>

		<form runat="server">
		</form>
	</Body>
</Html>
