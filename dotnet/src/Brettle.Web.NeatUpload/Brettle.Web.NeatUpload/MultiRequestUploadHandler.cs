/*
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2008  Dean Brettle

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
using System.Web;
using System.Web.UI;

namespace Brettle.Web.NeatUpload
{	
	public class MultiRequestUploadHandler : System.Web.IHttpHandler
	{
        // Create a logger for use in this class
        private static readonly log4net.ILog log
            = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public virtual void ProcessRequest(HttpContext context)
        {
            ProcessMultiRequestUploadRequest(context, UploadHttpModule.CurrentUploadState);
        }

        protected virtual void ProcessMultiRequestUploadRequest(HttpContext context, UploadState uploadState)
        {
            string controlID = context.Request.QueryString[MultiRequestUploadModule.ControlIDQueryParam];
            if (log.IsDebugEnabled) log.DebugFormat("controlID={0}", controlID);
            string postBackID = context.Request.QueryString[UploadModule.PostBackIDQueryParam];
            if (log.IsDebugEnabled) log.DebugFormat("postBackID={0}", postBackID);
            string secureStorageConfigString = context.Request.Form[UploadModule.ConfigFieldNamePrefix + controlID];
            if (log.IsDebugEnabled) log.DebugFormat("secureStorageConfigString={0}", secureStorageConfigString);
            string fileSizesString = context.Request.Form[MultiRequestUploadModule.FileSizesFieldName];
            if (log.IsDebugEnabled) log.DebugFormat("fileSizesString={0}", fileSizesString);

            if (postBackID != null && fileSizesString != null && fileSizesString.Length > 0)
            {
                string[] fileSizeStrings = fileSizesString.Split(' ');
                long totalSize = 0;
                for (int i = 0; i < fileSizeStrings.Length; i++)
                {
                    long size = Int64.Parse(fileSizeStrings[i]);
                    // fileSizesString contains a -1 for each non-Flash upload
                    // associated with the request.  Ignore those so that the 
                    // totalSize is not off by one.
                    if (size > 0)
                        totalSize += size;
                }
                uploadState.MultiRequestObject = secureStorageConfigString;
                uploadState.BytesTotal = totalSize;
            }
            // MacOSX Flash player won't fire FileReference.onComplete unless something is returned.
            context.Response.Clear();
            context.Response.Write(" ");
            context.Response.End();
        }

        public bool IsReusable { get { return true; } }
	}
}
