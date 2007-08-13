#!/bin/sh
ooffice -invisible 'macro:///Standard.Module1.SaveAsHTML("'`pwd`'/Manual.odt")' || (echo "ERROR: Unable to build Manual.html" ; exit 1)
