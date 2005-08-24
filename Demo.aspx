<%@ Page language="c#" Codebehind="Demo.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.Demo" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="NeatUpload" %>

<Html>
	<Head>
		<Title>NeatUpload Demo</Title>
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
			Pick a file: <Upload:InputFile id="inputFile" runat="server" />
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" />
<!--
			<input id="submitButton" runat="server" type="submit" />
-->
			<br />
			<Upload:ProgressBar id="progressBar" runat="server" ><asp:Label id="label" runat="server" Text="Check Progress"/></Upload:ProgressBar>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" />
			<pre id="bodyPre" runat="server">
				Nothing uploaded yet.
			</pre>
		</form>
	</Body>
</Html>
