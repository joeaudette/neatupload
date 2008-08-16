mtasc -v -swf "%1"\install\SWFUpload.swf -version 8 -main -frame 1 -header 1:1:12 "%1"\FlashDevelop\classes\*.as
if %errorlevel% == 0 goto jsmin
echo "Could not build SWFUpload.swf using mtasc."
echo "Make sure mtasc from mtasc.org is in your path."
goto error

:jsmin
cmd /c "jsmin < "%1"\FlashDevelop\swfupload.js > "%1"\install\SWFUpload.js"
if %errorlevel% == 0 goto exit
echo "Could not minify SWFUpload.js using jsmin."
echo "Make sure jsmin from crockford.com/javascript/jsmin.html is in your path."
goto error

:error
cmd /c "exit -1"

:exit
