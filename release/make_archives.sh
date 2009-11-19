#!/bin/sh
branch=$1
shift
point_release_number=$1
shift
if [ -z "$branch" -o -z "$point_release_number" ]; then
	echo "Usage: make_archives.sh BRANCH POINT_RELEASE_NUMBER"
	echo "For example make_archives.sh NeatUpload-1.0 3"
	exit 1
fi

tag="${branch}.${point_release_number}"
pushd "../../neatupload-release/$tag"
if [ -e bin ]; then
	cd bin
	if [ -e ../HashedInputFile/bin/Debug/Brettle.Web.NeatUpload.HashedInputFile.dll ]; then
		ln -s ../HashedInputFile/bin/Debug/Brettle.Web.NeatUpload.HashedInputFile* .
	fi
	if [ -e ../Extensions/SqlServerInputFile/SqlServerUploader/bin/Debug/Hitone.Web.SqlServerUploader.dll ]; then
		ln -s ../Extensions/SqlServerInputFile/SqlServerUploader/bin/Debug/Hitone.Web.SqlServerUploader* .
	fi
	ln -s Debug/Brettle* .
	if [ -e Brettle.Web.NeatUpload.dll ]; then
		sn -R Brettle.Web.NeatUpload.dll ../../keypair.snk
	fi
	cd ..
fi
cd .. 
tar czvf "${tag}.tar.gz" $tag
zip -r "${tag}.zip" $tag
cd $tag
patch -p0 < ../unbrand.patch
popd
echo "Now use MonoDevelop to rebuild the solution in Debug and Release configs and run make_unbranded_archives.sh"
