<%@ Page language="c#"  AutoEventWireup="false" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>

<Html>
	<Head runat="server">
		<Title>NeatUpload Test of Fallback Text Hiding</Title>
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
			<h1>Hide Fallback Text Test</h1>
			<p>
			<ul>
				<li>When Javascript is enabled, the fallback text should not be visible even while the page is loading.</li>
				<li>When Javascript is disable, the fallback text should be visible.</li>
			</ul>
			</p>
			<p>
			File: <Upload:InputFile id="inputFile1" runat="server" />
			<asp:Button id="submitButton1" runat="server" Text="Submit" />
			<Upload:ProgressBar id="popProgressBar" runat="server" inline="false" Triggers="submitButton1"/>
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton1"
				style="display:inline-block;background-color:GhostWhite;height:20px;width:165px;"/>
			<script language="javascript">
			
<!--
window.alert('Page loading - fallback text should not be visible');
// -->
			</script>
			<br />
			</p>
			<p>
			Note: For simplicity, this page doesn't display any info about the uploaded file.
			</p>
			</p>
			Page finished loading.
			</p>
		</form>
	</Body>
</Html>
