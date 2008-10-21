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

namespace Brettle.Web.NeatUpload
{	
	public abstract class SessionBasedUploadStateStoreProviderBase : UploadStateStoreProvider
	{
        public override string Description { get { return "Stores UploadState objects in the HttpSessionState of the current request."; } }
        
        private static string KeyPrefix = "NeatUpload_SessionBasedUploadState_";		

		public override UploadState Load(string postBackID)
		{
			HttpContext ctx = HttpContext.Current;
			string key = KeyPrefix + postBackID;
			return ctx.Session[key] as UploadState;
		}

		public override void MergeAndSave(UploadState uploadState)
		{
			HttpContext ctx = HttpContext.Current;
			string key = KeyPrefix + uploadState.PostBackID;
			UploadState storedUploadState = Load(uploadState.PostBackID);
			Merge(uploadState, storedUploadState);
			ctx.Session[key] = uploadState;
		}

		protected override void Delete(string postBackID)
		{
		    HttpContext ctx = HttpContext.Current;
		    string key = KeyPrefix + postBackID;
		    ctx.Session.Remove(key);
		}
	}
}
