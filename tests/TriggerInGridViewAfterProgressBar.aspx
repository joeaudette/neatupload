<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"
        "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Page language="c#" %>
<%@ Register TagPrefix="Upload" Namespace="Brettle.Web.NeatUpload" Assembly="Brettle.Web.NeatUpload" %>
<%@ Import namespace="System.Collections" %>
<html>
	<head runat="server">
		<title>Trigger in GridView after ProgressBar</title>
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
	</head>
	<body>
		<form id="uploadForm" runat="server">
			<h1>Trigger in GridView after ProgressBar</h1>
			<p>
			This page tests whether it is possible to have a ProgressBar trigger inside of a GridView that occurs
			after the ProgressBar.
			</p>
			<p>Here's the ProgressBar:<br />
			<Upload:ProgressBar id="inlineProgressBar" runat="server" inline="true"/>
			</p>
			<p>
			Here's the GridView:<br />
			<asp:GridView id="gridView" runat="server" OnPreRender="gridView_PreRender">
				<Columns>
		            <asp:TemplateField ShowHeader="False">
		                <ItemTemplate>
		                    <asp:LinkButton ID="LinkButton3" runat="server" CommandName="Edit" CausesValidation="false">Edit</asp:LinkButton>
		                </ItemTemplate>
		                <EditItemTemplate>
		                    <asp:LinkButton ID="UpdateButton" runat="server" CommandName="Update">Update</asp:LinkButton>
		                    <asp:LinkButton ID="CancelButton" runat="server" CommandName="Cancel">Cancel</asp:LinkButton>                    
		                </EditItemTemplate>
		            </asp:TemplateField>
		            <asp:TemplateField ShowHeader="False">
		                <ItemTemplate>
		                </ItemTemplate>
		                <EditItemTemplate>
		                    <Upload:InputFile ID="UrlPathFileUpload" runat="server" />
		                </EditItemTemplate>
		            </asp:TemplateField>
				</Columns>
			</asp:GridView>
			</p>
		</form>
	</body>
</html>
<script runat="server">
	void Page_Load(object src, EventArgs ev)
	{
		ArrayList list = new ArrayList();
		list.Add("Item1");
		list.Add("Item2");
		list.Add("Item3");
		gridView.DataSource = list;
	}
</script>
