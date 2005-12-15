#!/bin/sh
cd `dirname "$0"`
rm -f *.resources */*.resources
resgen /compile *.resx */*.resx
for f in *.resources */*.resources; do
	mv -f $f NeatUpload.`echo $f | sed -e 's?/?.?g'`
done

