<%@ Control language="c#" Src="TestControl.ascx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.TestControl" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
Pick one file: <Upload:InputFile id="inputFile" runat="server" /><br />
Pick multiple: <Upload:MultiFile id="multiFile" runat="server" UseFlashIfAvailable="true" /><br />
Standard HtmlInputFile: <input type="file" id="htmlInputFile" runat="server" /><br />
<asp:Button id="submitButton" runat="server" Text="Submit" />
<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="false" />
