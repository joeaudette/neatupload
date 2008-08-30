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
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Retrieves or creates <see cref="IUploadState"/> objects associated
	/// with particular post-back IDs.
	/// </summary>
	public abstract class UploadStateStoreProvider
	{
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
		public abstract UploadState Load(string postBackID);

		public abstract void MergeAndSave(UploadState uploadState);

		public abstract void DeleteIfStale(string postBackID);

		public static void Merge(UploadState uploadState, UploadState storedUploadState)
		{
			if (storedUploadState == null || uploadState == storedUploadState)
				return;

			UploadState uploadStateAtLastMerge = uploadState.UploadStateAtLastMerge;
			if (uploadStateAtLastMerge == null)
				uploadStateAtLastMerge = new UploadState(uploadState.PostBackID);

			uploadState.IsMerging = true;
			uploadState.BytesRead 
				= storedUploadState.BytesRead + (uploadState.BytesRead - uploadStateAtLastMerge.BytesRead);

			uploadState.BytesTotal 
				= storedUploadState.BytesTotal + (uploadState.BytesTotal - uploadStateAtLastMerge.BytesTotal);

			uploadState.FileBytesRead 
				= storedUploadState.FileBytesRead + (uploadState.FileBytesRead 
				                                     - uploadStateAtLastMerge.FileBytesRead);

			if (uploadState.Failure == null)
				uploadState.Failure = storedUploadState.Failure;

			if (uploadState.Rejection == null)
				uploadState.Rejection = storedUploadState.Rejection;

			if (uploadState.Files.Count < storedUploadState.Files.Count)
			{
				uploadState._Files = storedUploadState._Files;
			}

			if (uploadState.MultiRequestObject == null)
				uploadState.MultiRequestObject = storedUploadState.MultiRequestObject;

			if (uploadState.ProcessingStateDict == null ||  uploadState.ProcessingStateDict.Count == 0)
				uploadState._ProcessingStateDict = storedUploadState._ProcessingStateDict;

			if (uploadState.Status < storedUploadState.Status)
				uploadState.Status = storedUploadState.Status;

			uploadState.OnMerged();
			uploadState.IsMerging = false;
		}

		protected bool IsStale(UploadState uploadState)
		{
			return (uploadState.TimeOfLastMerge.AddSeconds(Config.Current.StateStaleAfterSeconds) > DateTime.Now);				
		}

	}
}
