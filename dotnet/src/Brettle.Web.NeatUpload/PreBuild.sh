#!/bin/sh

cd SWFUpload/FlashDevelop
./Build.sh || echo "WARNING: Unable to build SWFUpload from source.  Either install mtasc and jsmin or copy the latest SWFUpload.js and SWFUpload.swf files into the NeatUpload subfolder."
