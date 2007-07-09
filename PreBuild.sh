#!/bin/sh
cd `dirname "$0"`
find . -name "*.resources" -exec /bin/rm -f {} \;
resgen /compile `find . -name "*.resx"`

for f in `find . -name "*.resources" -print`; do
	mv -f $f NeatUpload.`echo $f | sed -e 's?^./??' -e 's?/?.?g'`
done

cp public.snk bin/Release/
cp public.snk bin/Debug/

cd SWFUpload/FlashDevelop
./Build.sh || echo "WARNING: Unable to build SWFUpload from source.  Either install mtasc and jsmin or copy the latest SWFUpload.js and SWFUpload.swf files into the NeatUpload subfolder."
