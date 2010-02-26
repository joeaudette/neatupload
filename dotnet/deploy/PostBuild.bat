del "%1\bin\SWFUpload.*"
del "%1\bin\js.*"
del "%1\bin\deploy.*"
md "%2\NeatUpload"
md "%2\bin"

copy "%1\..\..\js\install\*.js" "%2\NeatUpload\"
copy "%1\..\..\flash\src\SWFUpload\install\*.*" "%2\NeatUpload\"
copy "%1\..\src\Brettle.Web.NeatUpload\install\NeatUpload\*.*" "%2\NeatUpload\"
copy "%1\bin\*.pdb" "%2\bin"
copy "%1\bin\*.dll" "%2\bin"
xcopy "%1\..\src\Brettle.Web.NeatUpload\install\bin" "%2\bin\" /s /y
