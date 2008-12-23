/*
 
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005  Dean Brettle

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.IO;
using System.Web;
using System.Web.SessionState;
using System.Collections;
using System.Collections.Specialized;
using System.Net;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload.Internal.Module
{
	internal class Constants
	{
		// No instances.
		private Constants() { }
		
		internal const string ContextItemKeyPrefix = "NeatUpload.UploadedFile-";
				
		// All NeatUpload InputFile controls will have name attributes starting with the following prefix
		// This will appear in form field values, query strings, and window names.
		// Note: Must not include a "." because it confuses IE's javascript) and must not include a "-" 
		// because we use that separate the postback id from the control id.
		internal const string NamePrefix = "NeatUpload_";
		
		// The hidden form fields that contain per-control StorageConfig info have names which start with:
		internal const string ConfigNamePrefix = "NeatUploadConfig_";
		
		// The hidden form field that contain the sizes files to expect
		internal const string FileSizesName = "NeatUploadFileSizes";
	}
}
