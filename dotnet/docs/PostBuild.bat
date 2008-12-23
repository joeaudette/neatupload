del bin\Debug\*.*
del bin\Release\*.*
del bin\docs.*

rem TODO: Use sandcastle or something similar to build API docs for .NET 2.0 assemblies
rem ndocconsole bin\Brettle.Web.NeatUpload.dll bin\Brettle.Web.NeatUpload.HashedInputFile.dll bin\Brettle.Web.NeatUpload.GreyBoxProgressBar.dll bin\Hitone.Web.SqlServerUploader.dll -Documenter=MSDN -SkipCompile=true -OutputDirectory=api -SdkLinksOnWeb=true -OutputTarget=Web -CleanIntermediates=false -Title="NeatUpload Documentation"

rem Converting Manual.flatxml to Manual.html via OpenOffice...
swriter -invisible macro:///SaveAsHtml.Module1.SaveAsHTML("%1Manual.flatxml")
