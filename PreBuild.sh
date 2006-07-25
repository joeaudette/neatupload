#!/bin/sh
cd `dirname "$0"`
find . -name "*.resources" -exec /bin/rm -f {} \;
resgen /compile `find . -name "*.resx"`

for f in `find . -name "*.resources" -print`; do
	mv -f $f NeatUpload.`echo $f | sed -e 's?^./??' -e 's?/?.?g'`
done

cp public.snk bin/Release/
cp public.snk bin/Debug/

