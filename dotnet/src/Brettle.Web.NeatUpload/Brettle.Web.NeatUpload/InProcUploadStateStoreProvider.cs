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
	/// <summary>
	/// Retrieves or creates <see cref="IUploadState"/> objects associated
	/// with particular post-back IDs.
	/// </summary>
	/// <remarks>The <see cref="IUploadState"/> objects are stored in the
	/// <see cref="ApplicationState"/> so they are not shared across processes.
	/// As a result, this class will not work with web gardens/farms.
	/// </remarks>
	public class InProcUploadStateStoreProvider : UploadStateStoreProvider
	{

		private static string KeyPrefix = "NeatUpload_InProcUploadState_";
		/// <summary>
		/// Returns an <see cref="IUploadState"/> for a given post-back ID.  
		/// If one does not exist yet, a new one is created and returned.
		/// </summary>
		/// <param name="postBackID">
		/// A post-back ID identifying the <see cref="IUploadState"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IUploadState"/> corresponding to 
		/// <paramref name="postBackID"/>
		/// </returns>
		public override UploadState Load(string postBackID)
		{
			HttpContext ctx = HttpContext.Current;
			string key = KeyPrefix + postBackID;
			return ctx.Application[key] as UploadState;
		}

		public override void MergeAndSave(UploadState uploadState)
		{
			HttpContext ctx = HttpContext.Current;
			string key = KeyPrefix + uploadState.PostBackID;
			ctx.Application.Lock();
			try
			{
				UploadState storedUploadState = Load(uploadState.PostBackID);
				Merge(uploadState, storedUploadState);
				ctx.Application[key] = uploadState;
			}
			finally
			{
				ctx.Application.UnLock();
			}
		}

		public override void DeleteIfStale (string postBackID)
		{
			UploadState uploadState = Load(postBackID);
			if (IsStale(uploadState))
			{
				HttpContext ctx = HttpContext.Current;
				string key = KeyPrefix + uploadState.PostBackID;
				ctx.Application.Remove(key);
			}
		}
	}
}
