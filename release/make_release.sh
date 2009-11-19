#!/bin/sh
branch=$1
shift
point_release_number=$1
shift
if [ -z "$branch_name" -o -z "$point_release_number" ]; then
	echo "Usage: make_release.sh BRANCH POINT_RELEASE_NUMBER"
	echo "For example make_release.sh NeatUpload-1.0 3"
	exit 1
fi
git checkout $branch
tag="${branch}.${point_release_number}"
git tag $tag
exportdir=../../neatupload-release/"$tag"/
git checkout-index -a --prefix="$exportdir"
pushd "$exportdir" 

# Set informational version to the name of the current directory
informational_version=`basename "$PWD"`
find . -name "AssemblyInfo.cs" -exec sed -i -e "/assembly: *AssemblyInformationalVersion(.*)/ c [assembly: AssemblyInformationalVersion(\"$informational_version\")]" {} \;
popd
echo "Now use MonoDevelop to build Debug and Release dlls and run make_archives.sh" 
