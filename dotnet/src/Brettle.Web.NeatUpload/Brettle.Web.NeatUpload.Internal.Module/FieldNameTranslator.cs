/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2006  Dean Brettle

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
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Security.Cryptography;
using System.IO;
using System.Web.UI;
using System.Web;
using System.Text.RegularExpressions;

namespace Brettle.Web.NeatUpload.Internal.Module
{
	internal class FieldNameTranslator
	{
		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
		= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/
		internal FieldNameTranslator()
		{
			if (!Config.Current.UseHttpModule)
				return;
			HttpWorkerRequest worker = UploadHttpModule.GetCurrentWorkerRequest();
            if (worker != null)
            {
            	string qs = worker.GetQueryString();
				PostBackID = UploadHttpModule.GetPostBackIDFromQueryString(qs);
				MultiRequestControlID = UploadHttpModule.GetMultiRequestControlIDFromQueryString(qs);
			}
		}
		
		internal string PostBackID = null;
		internal string MultiRequestControlID = null;
		
		internal virtual string FileFieldNameToControlID(string name)
		{
			// If an MultiRequestControlID was specified in the query string, use it instead, 
			// because Flash doesn't provide any control over the field name used to upload files.
			if (MultiRequestControlID != null)
			{
				return MultiRequestControlID;
			}
			
			// If this is a the name of a file field that we created, return the associated control ID
			if (name != null && name.StartsWith(Constants.NamePrefix))
			{
				int dashIndex = name.IndexOf('-');
				return name.Substring(dashIndex + 1);
			}
				
			// Otherwise, if a PostBackID was specified in the query string just use the field name as is.
			if (PostBackID != null)
			{
				return name;
			}
			
			return null;
		}
		
		
		internal virtual string FileFieldNameToPostBackID(string name)
		{
			if (PostBackID != null)
				return PostBackID;
			
			if (name == null || !name.StartsWith(Constants.NamePrefix))
				return null;
			int dashIndex = name.IndexOf('-');
			return name.Substring(Constants.NamePrefix.Length, dashIndex-Constants.NamePrefix.Length);
		}
		
		internal virtual string ConfigFieldNameToControlID(string name)
		{
			if (name == null || !name.StartsWith(Constants.ConfigNamePrefix))
				return null;
			return name;
		}
		
		internal virtual string FileIDToConfigID(string fileID)
		{
			return Constants.ConfigNamePrefix + fileID;
		}
	}
}
