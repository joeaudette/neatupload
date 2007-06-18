<%@ Page language="c#" EnableSessionState="false" AutoEventWireup="false" Inherits="Brettle.Web.NeatUpload.AsyncUploadPage" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<%--
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005-2007  Dean Brettle

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
--%>
<%-- 
This is the default page that the MultiFile control uploads to when using Flash.  The logic is in 
AsyncUploadPage.cs 
--%>
<html>
	<head runat="server">
		<title>Async Upload</title>
	</head>
	<body>
		<form id="dummyForm" runat="server">
		<input id="inputFile" type="file" runat="server"/>
		</form>
	</body>
</html>
