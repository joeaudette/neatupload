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
using System.Web.Caching;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Retrieves or creates <see cref="IUploadState"/> objects associated
	/// with particular post-back IDs.
	/// </summary>
	public class UploadStateStore
	{
		private UploadStateStore()
		{
		}

		/// <summary>
		/// Returns an <see cref="IUploadState"/> for a given post-back ID  
		/// </summary>
		/// <param name="postBackID">
		/// A post-back ID identifying the <see cref="IUploadState"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IUploadState"/> corresponding to 
		/// <paramref name="postBackID"/>
		/// </returns>
		public static UploadState OpenReadOnly(string postBackID)
		{
			UploadState uploadState = Provider.Load(postBackID);
			return uploadState;
		}

		/// <summary>
		/// Returns an <see cref="UploadState"/> for a given post-back ID.
		/// or null if none exists.
		/// </summary>
		/// <param name="postBackID">
		/// A post-back ID identifying the <see cref="UploadState"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IUploadState"/> corresponding to 
		/// <paramref name="postBackID"/>, or null if none-exists.
		/// </returns>
		public static UploadState OpenReadWrite(string postBackID)
		{			
			UploadState uploadState = Provider.Load(postBackID);
			if (uploadState != null && uploadState.DeleteAfterDelayWhenNotOpenReadWrite)
			{
				CancelDeleteAfterDelay(uploadState.PostBackID);
			}
			return uploadState;
		}

		/// <summary>
		/// Returns an <see cref="IUploadState"/> for a given post-back ID.
		/// or creates one if none exists.
		/// </summary>
		/// <param name="postBackID">
		/// A post-back ID identifying the <see cref="IUploadState"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IUploadState"/> corresponding to 
		/// <paramref name="postBackID"/>
		/// </returns>
		public static UploadState OpenReadWriteOrCreate(string postBackID)
		{
			UploadState uploadState = OpenReadWrite(postBackID);
			if (uploadState == null)
			{
				uploadState = new UploadState(postBackID);
				uploadState.Changed += new EventHandler(UploadState_Changed);
				uploadState.DeleteAfterDelayWhenNotOpenReadWrite = true;
			}
			return uploadState;
		}

		/// <summary>
		/// Called to indicate that no additional calls will be made to members
		/// of the uploadState object.  If the implementation is supposed to
		/// share the information in the object across servers, it might need to 
		/// take some action to ensure that final changes are propagated to other
		/// servers.
		/// </summary>
		public static void Close(UploadState uploadState)
		{
			Provider.MergeAndSave(uploadState);
			if (uploadState.DeleteAfterDelayWhenNotOpenReadWrite)
				DeleteAfterDelay(uploadState);
		}

		private static void DeleteAfterDelay(UploadState uploadState)
		{
			HttpContext ctx = HttpContext.Current;
			ctx.Cache.Insert(uploadState.PostBackID, uploadState.PostBackID, null, 
			                 Cache.NoAbsoluteExpiration,
			                 TimeSpan.FromSeconds(Config.Current.StateStaleAfterSeconds),
			                 CacheItemPriority.High,
			                 new CacheItemRemovedCallback(CacheItem_Remove));
		}

		private static void CancelDeleteAfterDelay(string postBackID)
		{
			HttpContext ctx = HttpContext.Current;
			ctx.Cache.Remove(postBackID);			
		}
			

		private static void CacheItem_Remove(string postBackID, object val, CacheItemRemovedReason reason)
		{
			if (reason == CacheItemRemovedReason.Removed)
				return;
			Provider.DeleteIfStale(postBackID);			
		}


		private static UploadStateStoreProvider Provider = new SessionBasedUploadStateStoreProvider();

		private static void UploadState_Changed(object sender, EventArgs args)
		{
			UploadState uploadState = sender as UploadState;
			DateTime now = DateTime.Now;
			if (uploadState.TimeOfLastMerge == DateTime.MinValue)
				uploadState.TimeOfLastMerge = now;
			else if (uploadState.TimeOfLastMerge.AddSeconds(Config.Current.MergeIntervalSeconds) < now)
			{
				Provider.MergeAndSave(uploadState);
				uploadState.TimeOfLastMerge = now;
			}
		}
	}
}
