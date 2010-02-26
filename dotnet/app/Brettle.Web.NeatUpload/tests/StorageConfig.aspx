<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#" %>
<%@ Import namespace="System.IO" %>
<%@ Import namespace="Brettle.Web.NeatUpload" %>
<script runat="server">
	void Page_Load(object src, EventArgs ev)
	{
		InputFile inputFile = new InputFile();
		AssertEquals(null, inputFile.StorageConfig["tempDirectory"]);
		inputFile.StorageConfig["tempDirectory"] = "overrideValue";
		AssertEquals("overrideValue", inputFile.StorageConfig["tempDirectory"]);
		repeater.Controls.Add(inputFile);
		AssertEquals("overrideValue", inputFile.StorageConfig["tempDirectory"]);
		inputFile.StorageConfig["tempDirectory"] = "valueToPostback";

		InputFile inputFile2 = new InputFile();
		AssertEquals(null, inputFile2.StorageConfig["tempDirectory"]);
		repeater.Controls.Add(inputFile2);
		if (IsPostBack)
			AssertEquals("valueToPostback", inputFile2.StorageConfig["tempDirectory"]);
		else
			AssertEquals(null, inputFile2.StorageConfig["tempDirectory"]);
		inputFile2.StorageConfig["tempDirectory"] = "valueToPostback";
	}
	
	void AssertEquals(string expected, string actual)
	{
		if (expected != actual)
			throw new Exception(String.Format("{0} != {1}", expected, actual));
	}
</script>
<html>
  <head>
    <title>Test Page for NeatUpload StorageConfig Bug</title>
  </head>
  <body>
  	<form runat="server">
 		<div>
 		If you can submit this form and don't get an error, then this test works.
 		You don't need to upload any files.</div>
  		<asp:Repeater id="repeater" runat="server">
  			<ItemTemplate>
  			</ItemTemplate>
  		</asp:Repeater>
  		<br/>
  		<input type="submit" />
  	</form>  	
  </body>
</html>