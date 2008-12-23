<%@ Page language="c#" %>
<Html>
	<Head runat="server">
		<Title>Without NeatUpload Test</Title>
	</Head>
	<Body>
		<form id="uploadForm" runat="server" enctype="multipart/form-data" method="POST">
			<h1>Without NeatUpload Test</h1>
			<p>
			This page tests the performance of ASP.NET when NeatUpload is not being used.
			</p>
			<p>
			Now a file click Submit: <input id="htmlInputFile" type="file" runat="server"/><br />
			<asp:Button id="submitButton" runat="server" Text="Submit" />
			<pre id="bodyPre" runat="server">
			
			</pre>
		</form>
	</Body>
</Html>
<script runat="server" language="c#">
	void Page_Load(object src, EventArgs ev)
	{
		DateTime endTime = DateTime.Now;
		object startTimeObj = HttpContext.Current.Items["WithoutNeatUpload_StartTime"];
		if (startTimeObj != null)
		{
			DateTime startTime = (DateTime)startTimeObj;
			bodyPre.InnerText = "Upload time = " + (endTime - startTime);
		}
		
	}
</script>
