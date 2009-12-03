<%@ Page Language="vb" AutoEventWireup="false" Inherits="System.Web.UI.Page" %>
<% Server.Transfer("Bugs.aspx?test=transfer") %>
<html>
<head>
        <title>this page should never be reached</title>
        <meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1">
</head>
<body>
        <h1>this page should never be reached</h1>

</body>
</html>


