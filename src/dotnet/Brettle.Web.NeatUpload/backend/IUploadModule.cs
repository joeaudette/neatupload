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
		IUploadedFileCollection Files { get; }
		string PostBackID { get; }
		void SetProcessingState(string postBackID, string controlID, object state);
		void BindProgressState(string postBackID, string controlID, IUploadProgressState progressState);
		void CancelPostBack(string postBackID);
	}
}
