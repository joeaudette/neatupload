#!/bin/sh
cd `dirname "$0"`
rm -f *.resources */*.resources
resgen /compile *.resx */*.resx
for f in *.resources */*.resources; do
	base=`basename $f .resources`
	base=`basename $base .aspx`
	base=`basename $base .asax`
	mv -f $f Brettle.Web.NeatUpload.${base}.resources
done

