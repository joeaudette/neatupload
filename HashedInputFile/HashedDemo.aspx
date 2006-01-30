<%@ Page language="c#" Src="HashedDemo.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.HashedDemo" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<%@ Register TagPrefix="HashedUpload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload.HashedInputFile" %>

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
			</asp:DropDownList>
			Submit button type:
			<asp:DropDownList id="buttonTypeDropDown" runat="server" AutoPostBack="True" CausesValidation="False">
				<asp:ListItem Value="Button" Selected="True">Button</asp:ListItem>
				<asp:ListItem Value="LinkButton">LinkButton</asp:ListItem>
				<asp:ListItem Value="CommandButton">CommandButton</asp:ListItem>
			</asp:DropDownList>
			</p>
			<p>
			Now select some files and click Submit.
			</p>
			<p>
			Pick file #1: <HashedUpload:HashedInputFile id="inputFile" runat="server" />
			<asp:RegularExpressionValidator id="RegularExpressionValidator1" 
				ControlToValidate="inputFile"
				ValidationExpression="^(?!.*\.exe$).*"
				Display="Static"
				ErrorMessage="No EXEs allowed"
				EnableClientScript="True" 
				runat="server"/><br />
			Pick file #2: <HashedUpload:HashedInputFile id="inputFile2" runat="server" /><br />

			<span id="submitButtonSpan" runat="server">
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<asp:Button id="cancelButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</span>

			<span id="commandButtonSpan" runat="server">
			<asp:Button id="commandButton" runat="server" Text="Submit Command" Command="Submit" />
			<asp:Button id="cancelCommandButton" runat="server" Text="Cancel Command" CausesValidation="False" Command="Cancel" /><br />
			</span>

			<span id="linkButtonSpan" runat="server">
			<asp:LinkButton id="linkButton" runat="server" Text="Submit" />
			<asp:LinkButton id="cancelLinkButton" runat="server" Text="Cancel" CausesValidation="False"/><br />
			</span>
			
			<p>
			NeatUpload stores uploaded files in temporary storage on the server and
			automatically deletes them when your request completes.  A real 
			application would move or copy the files to their permanent location.  
			This demo just displays the files' name, size, and content type.    
			</p>

			<pre id="bodyPre" runat="server">
			
			</pre>
			</p>
			<div id="inlineProgressBarDiv" runat="server">
			<h2>Inline Progress Bar</h2>
			<p>
			Here's the inline progress bar:
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true" Triggers="submitButton linkButton" />
			The inline progress bar is an IFRAME.  If your browser doesn't support IFRAMEs, you'll see a link to
			display the progress bar in a new window.  The text of that link is configurable.
			</p>
			</div>

			<div id="popupProgressBarDiv" runat="server">
			<h2>Popup Progress Bar</h2>
			<p>
			Here's what is generated by a ProgressBar control configured to display a popup:
			</p> 
			<Upload:ProgressBar id="progressBar" runat="server" Triggers="submitButton linkButton">
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
	</Body>
</Html>
