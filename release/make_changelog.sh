#!/bin/sh
export PAGER=cat
branch=$1
shift
point_release_number=$1
shift
if [ -z "$branch" -o -z "$point_release_number" ]; then
	echo "Usage: make_changelog.sh BRANCH POINT_RELEASE_NUMBER"
	echo "For example make_changelog.sh NeatUpload-1.0 3"
	exit 1
fi
while [ "${point_release_number}" -gt 0 ]; do
	tag="${branch}.${point_release_number}"
	prev_point_release=`expr ${point_release_number} - 1`
	prev_tag="${branch}.${prev_point_release}"
	echo "=============================================================="
	echo "Changes in ${tag}"
	echo "=============================================================="
	git log --no-merges --pretty=format:%s%n%b ${prev_tag}..${tag}
	echo ""
	echo "Changed paths:"
	git diff --name-status ${prev_tag}..${tag}
	echo ""
	point_release_number=${prev_point_release}
done

product=`echo "${branch}" | sed -e "s/[-.]/ /g" | cut -f1 -d' '`
major=`echo "${branch}" | sed -e "s/[-.]/ /g" | cut -f2 -d' '`
minor=`echo "${branch}" | sed -e "s/[-.]/ /g" | cut -f3 -d' '`
prev_major=${major}
while [ "${minor}" -gt 0 ]; do
	prev_minor=`expr "${minor}" - 1`
	tag="${product}-${major}.${minor}.0"
	prev_tag="${product}-${major}.${prev_minor}.0"
	echo "=============================================================="
	echo "Changes in ${product}-${major}.${minor}"
	echo "=============================================================="
	git log --no-merges --pretty=format:%s%n%b ${prev_tag}..${tag}
	echo ""
	echo "Changed paths:"
	git diff --name-status ${prev_tag}..${tag}
	echo ""
	minor=${prev_minor}
done
