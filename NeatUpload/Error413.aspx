<%@ Page language="c#" AutoEventWireup="false" %>
<Html>
	<Head>
		<Title>Upload Too Large</Title>
	</Head>
	<Body>
		<h1>Upload Too Large</h1>
		<p>
		You are attempting to upload more than <%= Brettle.Web.NeatUpload.UploadHttpModule.MaxRequestLength / 1024 %> 
		Kbytes.  Please	use your browser's Back button to go back and try a smaller upload.
		</p>
	</Body>
</Html>
