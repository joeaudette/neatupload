<%@ Control language="c#" Src="TrickyTestControl.ascx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.TrickyTestControl" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<table width="100%">
    <thead>
        <tr>
            <td colspan="2" align="center">A Flash-enabled MultiFile control in a table</td>
        </tr>
    </thead>
    <tr>
        <td><Upload:MultiFile id="multiFile" runat="server" useFlashIfAvailable="true"/></td>
        <td><asp:Button id="submitButton" runat="server" Text="Submit" /></td>
    </tr>
    <tr>
        <td colspan="2" align="center"><Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Url="~/Brettle.Web.NeatUpload/tests/tricky/Progress.aspx" /></td>
    </tr>
</table>



