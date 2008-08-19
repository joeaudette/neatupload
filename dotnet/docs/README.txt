The source for the manual is in Manual.flatxml.  To open/edit it:
1. Install OpenOffice 2.3 or later.  You can choose to install only the
   Writer application from the installer.
2. Run OpenOffice Writer.
3. Select Tools > XML Filter Settings > Open Package, and then select 
   FlatXMLFilter.jar from the directory containing this README.txt file.

From then on you should be able to open Manual.flatxml.

To let the build automatically create Manual.html from Manual.flatxml:
1. Do the steps above.
2. Select Tools > Extension Manager > Add, and then select 
   ooSaveAsHtml.oxt from the directory containing this README.txt file.
3. Add the directory containing the ooffice executable (Linux) or 
   starwriter.exe (Windows) to your PATH environment variable.

From then on, if you exit OpenOffice before building docs.csproj, the build 
should create Manual.html.
