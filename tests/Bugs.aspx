<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#" %>
<%@ Import namespace="System.IO" %>
<script runat="server">
	void Page_Load(object src, EventArgs ev)
	{
		string test = Request.Params["test"];
		if (test == "download")
		{
			TestDownload();
		}
		else if (test == "redirect")
		{
			TestRedirect();
		}
		else if (test == "trace")
		{
			TestTrace();
		}
		else if (test == "flushandthrow")
		{
			TestFlushAndThrow();
		}
		else if (test == "throwtocustomerror")
		{
			TestThrowToCustomError();
		}
	}
	
	void TestDownload()
	{
		
		string strFileName = "Web.config";
		string strPath = Path.Combine(Request.PhysicalApplicationPath, strFileName);
        int intChunkSize = 10000;
        byte[] buffer = new byte[intChunkSize];

        Response.Clear();

        using (FileStream iStream = File.OpenRead(strPath))
        {
            long lngLengthToRead = iStream.Length;
            string strValue = "attachment;filename=" + strFileName;
            Response.AddHeader("content-disposition", strValue);

            while(lngLengthToRead > 0)
            {
                int intLengthRead = iStream.Read(buffer, 0, intChunkSize);
                Response.OutputStream.Write(buffer, 0, intLengthRead);

                Response.Flush();

                lngLengthToRead -= intLengthRead;
            }
        }

        Response.Close();
	}
	
	void TestRedirect()
	{
		Response.Redirect("http://www.google.com/");
	}
	
	void TestTrace()
	{
		Trace.IsEnabled = true;
		Trace.Write("Hello world from TestTrace() in Bugs.aspx!");
	}
	
	void TestFlushAndThrow()
	{
		Response.Flush();
		throw new HttpException(413, "Can't display the Error413.aspx page because response was flushed.");
	}

	void TestThrowToCustomError()
	{
		throw new HttpException(413, "Should display the Error413.aspx page.");
	}
</script>
<html>
  <head>
    <title>Test Page for NeatUpload Bugs</title>
  </head>
  <body>
    <h1>Test Page for NeatUpload Bugs</h1>
    <p>
    This page can be used to verify that various NeatUpload bugs or potential bugs have been fixed.  
    You should be able to:
    </p>
    <ul>
      <li><a href="Bugs.aspx?test=download">download the Web.config file</a>.</li>
      <li><a href="Bugs.aspx?test=redirect">get redirected to Google</a>.</li>
      <li><a href="Bugs.aspx?test=trace">generate some trace output</a>.</li>
      <li><a href="Bugs.aspx?test=throwtocustomerror">throw a 413 HttpException to display a custom error page</a>.</li>
      <li><a href="Bugs.aspx?test=flushandthrow">flush the response and throw an exception that shouldn't display a custom error page.</a>.</li>
    </ul>
  </body>
</html>