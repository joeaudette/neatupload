This is NeatUpload.

This project is not maintained, it was migrated from codeplex to preserve history since codeplex is shutting down.

The remainder of this file is dedicated to building NeatUpload from source.

To build the code you will need:

1. mtasc from http://mtasc.org.  The mtasc executable needs to be on your PATH. (this is used to compile the ActionScript fot SWFUpload)
2. jsmin from http://crockford.com/javascript/jsmin.html.  The jsmin
   executable needs to be on your PATH.
3. Either VS2008 or MonoDevelop 1.0.  Msbuild or xbuild might work as well.

To build the Manual.html, you will OpenOffice.org Writer and some files in
the dotnet/docs directory.  For details, see dotnet/docs/README.txt.

/** Update 2011-01-13 - changes by Joe Audette **/
Added NeatUpload-VS2010.sln file
Removed the docs project as I don't care to use Open Office plus plugins to maintain the documentation, too complicated and requires installing things I'd rather not install.
I've left the project existing just not included in the VS2010 solution.
I will convert the Open Office doc to Word and just export as Html from Word

To build, open the NeatUpload.sln in your IDE and build it.  If you have met
all of the above requirements, the build should succeed.  If you are using
VS2008, NeatUpload will be built against .NET 2.0.  If you are using
MonoDevelop, it will be build against .NET 1.1.

The deploy project will copy the dlls and support files into the dotnet/app/
folder.  You can either open that folder as a web project in VS2008 or 
configure it as a web application in IIS.  Under mono, you can run xsp in that
folder to access the app at http://localhost:8080/.  

Here is brief tour of the directory structure:
js/ - Javascript source that is specific to .NET.  In theory someone could
      port the dotnet source to Java or PHP and reuse the the Javascript code
      in this folder.  js.csproj is a dummy project that does nothing
      accept provide a convenient way to access the js files from the IDE. 

flash/ - The Flash/ActionScript code.  This is not specific to .NET.  It is  
      just a slightly modified old version of SWFUpload a the moment.
      flash.csproj is a dummy project that runs the script/batch file to build
      the ActionScript files and minify the associated Javascript files.

dotnet/ - Everything that is specific to .NET.

dotnet/src/ - The source for all of the assemblies.

dotnet/src/Brettle.Web.NeatUpload/ - The source for Brettle.Web.NeatUpload.dll
      There is a subfolder for each namespace.  To help organize the code a
      bit, internal classes are in the Brettle.Web.NeatUpload.Internal.*
      namespaces.  Brettle.Web.NeatUpload.csproj builds
      Brettle.Web.NeatUpload.dll.

dotnet/src/Extensions/ - The source for the various extension assemblies.
      There is a subfolder for each extension.  Each subfolder contains a
      project file for building the associated extension.

dotnet/app/ - The test/demo application discussed above.

dotnet/app/Brettle.Web.NeatUpload/ - Pages that only require
       Brettle.Web.NeatUpload.dll.

dotnet/app/Extensions/ - Test and demo pages for the extension assemblies.
       There is a subfolder for each extension.

dotnet/deploy/ - The dummy project that runs the script/batch file to copy
       the various assemblies and support files into the dotnet/app folder.

dotnet/docs/ - The documentation files.  docs.csproj is a dummy project that
       runs the script/batch file to create the HTML documentation.  



