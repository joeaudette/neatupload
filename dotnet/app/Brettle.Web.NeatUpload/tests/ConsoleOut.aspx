<%@ Page language="c#" AutoEventWireup="false" %>
<html>
	<head>
		<title>Console.Out Log</title>
	</head>
	<body>
		<pre>
<%= ((System.IO.StringWriter)Application["NeatUpload_AppStateLogger"]).ToString() %>
		</pre>
	</body>
</html>
