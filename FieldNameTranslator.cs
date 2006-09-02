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

namespace Brettle.Web.NeatUpload
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
			if (!UploadHttpModule.IsInited)
				return;
			string postBackIDQueryParam = Config.Current.PostBackIDQueryParam;
			HttpWorkerRequest worker = UploadHttpModule.GetCurrentWorkerRequest();
            if (postBackIDQueryParam == null || worker == null)
                return;
			string qs = worker.GetQueryString();
            if (qs == null)
                return;
			Match match = Regex.Match(qs, @"(^|\?|&)" + Regex.Escape(postBackIDQueryParam) + @"=([^&]+)");
			if (match.Success)
			{
				PostBackID = HttpUtility.UrlDecode(match.Groups[2].Value);
			}
		}
		
		internal string PostBackID = null;
		
		internal virtual string FileFieldNameToControlID(string name)
		{
			if (PostBackID != null)
				return name;
			
			if (name == null || !name.StartsWith(UploadContext.NamePrefix))
				return null;
			int dashIndex = name.IndexOf('-');
			return name.Substring(dashIndex + 1);
		}
		
		
		internal virtual string FileFieldNameToPostBackID(string name)
		{
			if (PostBackID != null)
				return PostBackID;
			
			if (name == null || !name.StartsWith(UploadContext.NamePrefix))
				return null;
			int dashIndex = name.IndexOf('-');
			return name.Substring(UploadContext.NamePrefix.Length, dashIndex-UploadContext.NamePrefix.Length);
		}
		
		internal virtual string ConfigFieldNameToControlID(string name)
		{
			if (name == null || !name.StartsWith(UploadContext.ConfigNamePrefix))
				return null;
			return name;
		}
		
		internal virtual string FileIDToConfigID(string fileID)
		{
			return UploadContext.ConfigNamePrefix + fileID;
		}
		
		internal virtual string FormatFileFieldName(string postBackID, string controlID)
		{
			return UploadContext.NamePrefix + postBackID + "-" + controlID;
		}
		
		internal virtual string FormatConfigFieldName(string postBackID, string controlID)
		{
			return UploadContext.ConfigNamePrefix + controlID;
		}
	}
}
