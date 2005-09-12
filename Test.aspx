<%@ Page language="c#" Codebehind="Test.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.Test" %>
<%@ Register TagPrefix="Upload" TagName="TestControl" Src="~/TestControl.ascx" %>

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
		<Upload:TestControl id="testControl" runat="server" />
			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</Body>
</Html>
