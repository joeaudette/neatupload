<%@ Control language="c#" Codebehind="TestControl.ascx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.TestControl" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<asp:Button id="submitButton" runat="server" Text="Submit" />
Pick a file: <Upload:InputFile id="inputFile" runat="server" />
<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" />
