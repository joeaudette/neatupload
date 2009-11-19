#!/bin/sh
cd `dirname "$0"`
cp ../bin/Release/*.xml ./bin/Release
ndoc ./bin/Release/*.dll \
	-Documenter=MSDN \
	-SkipCompile=true \
	-OutputDirectory=../docs/api \
	-SdkLinksOnWeb=true \
	-OutputTarget=Web \
	-CleanIntermediates=false \
	-Title="NeatUpload Documentation"

