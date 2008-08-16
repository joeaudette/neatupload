#!/bin/sh
echo "Converting Manual.flatxml to Manual.html via OpenOffice..."
ooffice -invisible 'macro:///Standard.Module1.SaveAsHTML("'`pwd`'/Manual.flatxml")' || (echo "ERROR: Unable to build Manual.html" ; exit 1)
