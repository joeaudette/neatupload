/*
NeatUpload - an HttpModule and User Control for uploading large files
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
using System.Collections.Specialized;
using System.Web;

namespace Brettle.Web.NeatUpload
{
	public interface IUploadModule
	{
		string PostBackIDQueryParam { get; }
		string FileFieldNamePrefix { get; }
		string ConfigFieldNamePrefix { get; }
		bool IsEnabled { get; }
		NameValueCollection Unprotect(string armoredString);
		string Protect(NameValueCollection nvc);
		UploadedFileCollection Files { get; }
		string PostBackID { get; }
		void SetProcessingState(string postBackID, string controlUniqueID, object state);
		void BindProgressState(string postBackID, string controlUniqueID, IUploadProgressState progressState);
		void CancelPostBack(string postBackID);
		UploadedFile ConvertToUploadedFile(string controlUniqueID, HttpPostedFile file);
		string FileSizesFieldName { get; }
		string AsyncUploadPath { get; }
		string AsyncControlIDQueryParam { get; }
		string ArmoredCookiesQueryParam { get; }		
	}
}
