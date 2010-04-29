#!/bin/sh
branch=$1
shift
point_release_number=$1
shift
if [ -z "$branch" -o -z "$point_release_number" ]; then
	echo "Usage: make_unbranded_archives.sh BRANCH POINT_RELEASE_NUMBER"
	echo "For example make_unbranded_archives.sh NeatUpload-1.3 10"
	exit 1
fi

tag="${branch}.${point_release_number}"
pushd "../../neatupload-release"
tar czvf "${tag}.unbranded.tar.gz" $tag
zip -r "${tag}.unbranded.zip" $tag
popd
echo "Now upload ../../neatupload-release/${tag}.* and announce the release."
echo "Also, don't forget to call 'git push' if you haven't already."
