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
using System.Collections;
using System.Collections.Specialized;
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
            if (uploadState == null)
                return null;
            UploadState uploadStateCopy = new UploadState();
            uploadStateCopy.CopyFrom(uploadState);
            uploadStateCopy.IsWritable = false;
			return uploadStateCopy;
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
                uploadState.IsWritable = true;
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
            uploadState.IsWritable = true;
            return uploadState;
		}

		/// <summary>
		/// Called to indicate that no additional calls will be made to members
		/// of the uploadState object during this request.  If the implementation
        /// is supposed to share the information in the object across servers, it 
        /// might need to take some action to ensure that final changes are 
        /// propagated to other servers.
		/// </summary>
		public static void Close(UploadState uploadState)
		{
            if (uploadState.IsWritable)
			    MergeAndSave(uploadState);
			if (uploadState.DeleteAfterDelayWhenNotOpenReadWrite)
				DeleteAfterDelay(uploadState);
		}

		private static void DeleteAfterDelay(UploadState uploadState)
		{
			HttpContext ctx = HttpContext.Current;
            UploadStateStoreProvider.CleanUpIfStaleCallback cleanUpIfStaleCallback
                = Provider.GetCleanUpIfStaleCallback();
            ctx.Cache.Insert(uploadState.PostBackID, cleanUpIfStaleCallback, null,
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
            UploadStateStoreProvider.CleanUpIfStaleCallback cleanUpIfStaleCallback 
                = (UploadStateStoreProvider.CleanUpIfStaleCallback)val;
			if (reason == CacheItemRemovedReason.Removed)
				return;
            cleanUpIfStaleCallback(postBackID);
		}

		public static void UploadState_Changed(object sender, EventArgs args)
		{
			UploadState uploadState = sender as UploadState;
			DateTime now = DateTime.Now;
			if (uploadState.TimeOfLastMerge == DateTime.MinValue)
				uploadState.TimeOfLastMerge = now;
			else if (uploadState.TimeOfLastMerge.AddSeconds(Config.Current.MergeIntervalSeconds) < now)
			{
				MergeAndSave(uploadState);
				uploadState.TimeOfLastMerge = now;
			}
		}

        private static void MergeAndSave(UploadState uploadState)
        {
            Provider.MergeAndSave(uploadState);
        }

        public static UploadStateStoreProvider Provider
        {
            get
            {
                Config config = Config.Current;
                if (config.DefaultStateStoreProviderName == null)
                {
                    return LastResortProvider;
                }
                return config.StateStoreProviders[config.DefaultStateStoreProviderName];
            }
        }

        public static UploadStateStoreProviderCollection Providers
        {
            get
            {
                return Config.Current.StateStoreProviders;
            }
        }

        private static AdaptiveUploadStateStoreProvider _lastResortProvider = null;
        private static object _lock = new object();
        internal static AdaptiveUploadStateStoreProvider LastResortProvider
        {
            get
            {
                lock (_lock)
                {
                    if (_lastResortProvider == null)
                    {
                        _lastResortProvider = new AdaptiveUploadStateStoreProvider();
                        _lastResortProvider.Initialize("AdaptiveUploadStateStoreProvider", new NameValueCollection());
                    }
                }
                return _lastResortProvider;
            }
        }
    }
}
