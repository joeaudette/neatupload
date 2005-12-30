#!/bin/sh
topdir=`dirname "$0"`
cd $topdir

# Set informational version to the name of the current directory
informational_version=`basename $topdir`
sed -i -e "/assembly: *AssemblyInformationalVersion(.*)/ c [assembly: AssemblyInformationalVersion(\"$informational_version\")]" AssemblyInfo.cs

# Compile resources
rm -f *.resources */*.resources
resgen /compile *.resx */*.resx
for f in *.resources */*.resources; do
	mv -f $f NeatUpload.`echo $f | sed -e 's?/?.?g'`
done

