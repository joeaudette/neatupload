#!/bin/sh
cd `dirname "$0"`
find . -name "*.resources" -exec /bin/rm -f {} \;

RESGEN=resgen
if which resgen1 >/dev/null 2>&1; then
	RESGEN=resgen1
fi

$RESGEN /compile `find . -name "*.resx"`

for f in `find . -name "*.resources" -print`; do
	mv -f $f NeatUpload.`echo $f | sed -e 's?^./??' -e 's?/?.?g'`
done

