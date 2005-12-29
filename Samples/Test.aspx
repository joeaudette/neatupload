<%@ Page language="c#" Src="Test.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.Test" %>
<%@ Register TagPrefix="Upload" TagName="TestControl" Src="~/TestControl.ascx" %>

<Html>
	<Head>
		<Title>NeatUpload User Control Test</Title>
	</Head>
	<Body>
		<form id="uploadForm" runat="server">
		<Upload:TestControl id="testControl" runat="server" />
			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</Body>
</Html>
