#!/bin/sh
cd `dirname "$0"`
ndoc `ls -t bin/*/Brettle.Web.NeatUpload.dll | head -1` \
	-Documenter=MSDN \
	-SkipCompile=true \
	-OutputDirectory=docs/api \
	-SdkLinksOnWeb=true \
	-OutputTarget=Web \
	-CleanIntermediates=false \
	-Title="NeatUpload Documentation"

