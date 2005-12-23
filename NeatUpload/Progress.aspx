<%@ Page language="c#" Codebehind="Progress.aspx.cs" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.Progress" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<html>
	<head>
		<title>Upload Progress</title>
		<script src="Progress.js"></script>
		<link rel="stylesheet" type="text/css" title="default" href="default.css" />		
		<style type="text/css">
<!--
		body, form, table {
			margin: 0px;
			border: 0px none;
			padding: 0px;
		}

		table {
			vertical-align: middle;
			width: 100%;
			height: 100%;
		}
		
		#barTd {
			width: 100%;
		}
		
		#statusDiv {
			border-width: 1px;
			border-style: solid;
			padding: 0px;
			position: relative;
			width: 100%;
			text-align: center;
			z-index: 1; 
		}
		
		#barDiv,#barDetailsDiv {
			border: 0px none ; 
			margin: 0px; 
			padding: 0px; 
			position: absolute; 
			top: 0pt; 
			left: 0pt; 
			z-index: -1; 
			height: 100%;
			width: 75%;
		}
-->
		</style>
	</head>
	<body>
		<form id="dummyForm" runat="server">
		<table class="ProgressDisplay">
		<tr>
		<td>
			<span id="label" runat="server" class="Label">Upload&#160;Status:</span>
		</td>
		<td id="barTd" >
			<div id="statusDiv" runat="server" class="StatusMessage">&#160;
				<Upload:DetailsSpan id="normalInProgress" runat="server" WhenStatus="NormalInProgress" style="font-weight: normal; white-space: nowrap;">
					<%# FormatCount(BytesRead) %>/<%# FormatCount(BytesTotal) %> <%# CountUnits %>
					(<%# String.Format("{0:0%}", FractionComplete) %>) at <%# FormatRate(BytesPerSec) %>
					- <%# FormatTimeSpan(TimeRemaining) %> left
				</Upload:DetailsSpan>
				<Upload:DetailsSpan id="chunkedInProgress" runat="server" WhenStatus="ChunkedInProgress" style="font-weight: normal; white-space: nowrap;">
					<%# FormatCount(BytesRead) %> <%# CountUnits %>
					at <%# FormatRate(BytesPerSec) %>
					- <%# FormatTimeSpan(TimeElapsed) %> elapsed
				</Upload:DetailsSpan>
				<Upload:DetailsSpan id="completed" runat="server" WhenStatus="Completed">
					Complete: <%# FormatCount(BytesRead) %> <%# CountUnits %>
					at <%# FormatRate(BytesPerSec) %>
					took <%# FormatTimeSpan(TimeElapsed) %>
				</Upload:DetailsSpan>
				<Upload:DetailsSpan id="cancelled" runat="server" WhenStatus="Cancelled">
					Cancelled!
				</Upload:DetailsSpan>
				<Upload:DetailsSpan id="rejected" runat="server" WhenStatus="Rejected">
					Rejected: <%# Rejection != null ? Rejection.Message : "" %>
				</Upload:DetailsSpan>
				<Upload:DetailsSpan id="error" runat="server" WhenStatus="Failed">
					Error: <%# Failure != null ? Failure.Message : "" %>
				</Upload:DetailsSpan>
				<Upload:DetailsDiv id="barDetailsDiv" runat="server" 
					style='<%# String.Format(@"width: {0:0}%;", 100*FractionComplete) %>' class="ProgressBar"></Upload:DetailsDiv>	
			</div>
		</td>
		<td>
			<a id="cancelLink" runat="server" title="Cancel Upload" class="ImageButton"><img id="cancelImage" runat="server" src="cancel.png" alt="Cancel Upload" /></a>
			<a id="refreshLink" runat="server" title="Refresh" class="ImageButton"><img id="refreshImage" runat="server" src="refresh.png" alt="Refresh" /></a>
			<a id="stopRefreshLink" runat="server" title="Stop Refreshing" class="ImageButton"><img id="stopRefreshImage" runat="server" src="stop_refresh.png" alt="Stop Refreshing" /></a>
		</td>
		</tr>
		</table>
		</form>
	</body>
</html>
