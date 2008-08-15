#!/bin/sh
mtasc -v -swf ../../NeatUpload/SWFUpload.swf -version 8 -main -frame 1 -header 1:1:12 classes/*.as && \
jsmin < swfupload.js > ../../NeatUpload/SWFUpload.js
