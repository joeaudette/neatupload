<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#" Src="Demo.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.Demo" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<html>
	<head runat="server">
		<title>NeatUpload Demo</title>
		<style type="text/css">
		.ProgressBar {
			margin: 0px;
			border: 0px;
			padding: 0px;
			width: 100%;
			height: 2em;
		}
		</style>
	</head>
	<body>
		<form id="uploadForm" runat="server">
		<Upload:UnloadConfirmer runat="server"/>
			<h1>NeatUpload Demo</h1>
			<p>
			This page demonstrates the basic functionality of <a href="http://www.brettle.com/neatupload">NeatUpload</a>.
			Start by selecting the progress bar location and submit button type you'd like to see demostrated.
			</p>
			<p>
			Progress bar location: 
			<asp:DropDownList id="progressBarLocationDropDown" runat="server" AutoPostBack="True" CausesValidation="False">
				<asp:ListItem Value="Popup" Selected="True">Popup</asp:ListItem>
				<asp:ListItem Value="Inline">Inline</asp:ListItem>
				<asp:ListItem Value="None">None</asp:ListItem>
			</asp:DropDownList>
			Submit button type:
			<asp:DropDownList id="buttonTypeDropDown" runat="server" AutoPostBack="True" CausesValidation="False">
				<asp:ListItem Value="Button" Selected="True">Button</asp:ListItem>
				<asp:ListItem Value="LinkButton">LinkButton</asp:ListItem>
				<asp:ListItem Value="CommandButton">CommandButton</asp:ListItem>
				<asp:ListItem Value="HtmlInputButtonButton">HtmlInputButton(type=Button)</asp:ListItem>
				<asp:ListItem Value="HtmlInputButtonSubmit">HtmlInputButton(type=Submit)</asp:ListItem>
			</asp:DropDownList>
			<a href="javascript:NeatUploadConsole.open('Console opened')">Show NeatUpload Console (for debugging)</a>
			</p>
			<p>
			Now either select and upload files using the following MultiFile controls:
			</p>
			<p>File(s) to upload: 
			<Upload:MultiFile id="multiFile" runat="server" useFlashIfAvailable="true" flashFilterExtensions="*.jpg;*.gif;*.png">
				<asp:Button id="multiFileButton" Text="Add File..." Enabled="<%# multiFile.Enabled %>" runat="server"/>
			</Upload:MultiFile>
			<asp:RegularExpressionValidator id="RegularExpressionValidator2" 
				ControlToValidate="multiFile"
				ValidationExpression="(([^.;]*[.])+(jpg|gif|png|JPG|GIF|PNG); *)*(([^.;]*[.])+(jpg|gif|png|JPG|GIF|PNG))?$"
				Display="Static"
				ErrorMessage="Only jpg, gif, and png extensions allowed"
				EnableClientScript="True" 
				runat="server"/><br />
			</p>
			<p>Other file(s) to upload (just to demonstrate multiple MultiFileControls on the same page): 
			<Upload:MultiFile id="multiFile2" runat="server" useFlashIfAvailable="true">
				<asp:Button id="multiFileButton2" Text="Add File..." Enabled="<%# multiFile2.Enabled %>" runat="server"/>
			</Upload:MultiFile>
			</p>
			<p>
			Or, select some files using the InputFile controls below and click Submit.
			</p>
			<p>
			Pick file #1: <Upload:InputFile id="inputFile" runat="server" />
			<asp:RegularExpressionValidator id="RegularExpressionValidator1" 
				ControlToValidate="inputFile"
				ValidationExpression=".*([^e]|[^x]e|[^e]xe|[^.]exe)$"
				Display="Static"
				ErrorMessage="No EXEs allowed"
				EnableClientScript="True" 
				runat="server"/><br />
			Pick file #2: <Upload:InputFile id="inputFile2" runat="server" /><br />

			<span id="submitButtonSpan" runat="server">
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</span>

			<span id="commandButtonSpan" runat="server">
			<asp:Button id="commandButton" runat="server" Text="Submit Command" CommandName="Submit" />
			<asp:Button id="cancelCommandButton" runat="server" Text="Cancel Command" CausesValidation="False" CommandName="Cancel" /><br />
			</span>

			<span id="linkButtonSpan" runat="server">
			<asp:LinkButton id="linkButton" runat="server" Text="Submit" />
			<asp:LinkButton id="cancelLinkButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</span>
			
			<span id="htmlInputButtonButtonSpan" runat="server">
			<input type="Button" id="htmlInputButtonButton" runat="server" name="htmlInputButtonButton" value="Submit" />
			<input type="Button" id="cancelhtmlInputButtonButton" runat="server" name="htmlInputButtonButton" value="Cancel" /><br />
			</span>
			<span id="htmlInputButtonSubmitSpan" runat="server">
			<input type="Submit" id="htmlInputButtonSubmit" runat="server" name="htmlInputButtonSubmit" value="Submit" />
			<input type="Submit" id="cancelhtmlInputButtonSubmit" runat="server" name="htmlInputButtonSubmit" value="Cancel" /><br />
			</span>
			</p>
			<p>
			NeatUpload stores uploaded files in temporary storage on the server and
			automatically deletes them when your request completes.  A real 
			application would move or copy the files to their permanent location.  
			This demo just displays the files' name, size, and content type.    
			</p>

			<pre id="bodyPre" runat="server">
			
			</pre>
			<div id="inlineProgressBarDiv" runat="server">
			<h2>Inline Progress Bar</h2>
			<p>
			The inline progress bar will be displayed here:
			</p>
			<div style="display: none;">
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton linkButton commandButton htmlInputButtonButton htmlInputButtonSubmit" />
			</div>
			<script type="text/javascript">
window.onload = function()
{
	var inlineProgressBar = NeatUploadPB.prototype.Bars["inlineProgressBar"];
	var origDisplay = inlineProgressBar.Display;
	inlineProgressBar.Display = function()
	{
		var elem = document.getElementById(this.ClientID);
		elem.parentNode.style.display = "block";
		origDisplay.call(this);
	}
	inlineProgressBar.EvalOnClose = "NeatUploadMainWindow.document.getElementById('" 
		+ inlineProgressBar.ClientID + "').parentNode.style.display = \"none\";";
}
</script>
            <p>
			By default, an inline progress bar is always displayed.  The inline progress bar above has been 
			customized with javascript (see HTML source for this page) to hide the progress bar except when the
			upload is in progress or an error has occurred.
			The inline progress bar is an IFRAME.  If your browser doesn't support IFRAMEs, you'll see a link to
			display the progress bar in a new window.  The text of that link is configurable.
			</p>
			</div>

			<div id="popupProgressBarDiv" runat="server">
			<h2>Popup Progress Bar</h2>
			<p>
			Here's what is generated by a ProgressBar control configured to display a popup:
			</p>
			<Upload:ProgressBar id="progressBar" runat="server" Triggers="submitButton linkButton commandButton htmlInputButtonButton htmlInputButtonSubmit">
			<asp:Label id="label" runat="server" Text="Check Progress"/>
			</Upload:ProgressBar>
			<p>
			<strong>What?  You don't see anything?</strong>  That's because you have JavaScript enabled.  With JavaScript
			enabled, the popup is shown automatically when you submit the form, so there is no need to display anything
			extra on this page. 	If you disable JavaScript, you will see a "Check Progress" link which you can click
			on to display the progress bar in a new window.  The text of that link is configurable.
			</p>
			</div>
			
			<h2>Cancelling Uploads</h2>
			<p>
			You can cancel your upload by either clicking your browser's Stop button or clicking the Cancel button that
			is displayed to the right of the progress bar when the upload is in progress.
			</p>
			
		</form>
	</body>
</html>
