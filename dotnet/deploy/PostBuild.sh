#!/bin/sh
rm "$1"/bin/SWFUpload.*
rm "$1"/bin/js.*
rm "$1"/bin/deploy.*
rm -rf "$1"/bin/Debug
rm -rf "$1"/bin/Release
mkdir "$1"/NeatUpload

# Return an error if any of these fail
set -e
# Display the commands as they are executed
set -x
cp "$1"/../../js/install/*.js "$2"/NeatUpload/
cp "$1"/../../flash/src/SWFUpload/install/* "$2"/NeatUpload/
cp "$1"/../src/Brettle.Web.NeatUpload/install/NeatUpload/* "$2"/NeatUpload/
cp "$1"/bin/* "$2"/bin