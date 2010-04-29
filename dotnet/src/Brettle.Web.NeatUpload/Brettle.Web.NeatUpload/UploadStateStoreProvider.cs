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
using System.Collections;
using System.Web;
using System.Collections.Specialized;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Base class for classes that retrieve or creates <see cref="IUploadState"/> objects associated
	/// with particular post-back IDs.
	/// </summary>
	/// <remarks>Subclasses
	/// are instantiated by NeatUpload if they are added in the &lt;providers&gt; section of the &lt;neatUpload&gt;
	/// section.  Application developers should not instantiate UploadStateStoreProviders directly.
	/// </remarks>
	public abstract class UploadStateStoreProvider
	{
        // Create a logger for use in this class
        private static readonly log4net.ILog log
            = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initialize this object using the name and attributes.
		/// </summary>
		/// <param name="name">
		/// The name of this provider as specified by the name attribute of the &lt;add&gt; element.
		/// Available via the <see cref="Name"/> property after this method is called.
		/// </param>
		/// <param name="attrs">
		/// A <see cref="System.Collections.Specialized.NameValueCollection"/> representing additional
		/// attributes of the &lt;add&gt; element that can be used by subclasses.
		/// </param>
        public virtual void Initialize(string name, NameValueCollection attrs)
        {
            _name = name;
        }

		/// <value>
		/// A human readable description of this provider.
		/// </value>
        public abstract string Description { get; }

        private string _name = null;
		
		/// <value>
		/// The name of this provider as specified by the name attribute of the &lt;add&gt; element.
		/// </value>
		public virtual string Name { get { return _name; } }

        /// <summary>
		/// Returns an <see cref="IUploadState"/> for a given post-back ID.  
		/// </summary>
		/// <param name="postBackID">
		/// A post-back ID identifying the <see cref="IUploadState"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IUploadState"/> corresponding to 
		/// <paramref name="postBackID"/>, or null if none exists.
		/// </returns>
		public abstract UploadState Load(string postBackID);

		/// <summary>
		/// Merges a particular <see cref="UploadState"/> with the stored <see cref="UploadState"/> and store
		/// the merged <see cref="UploadState"/>.
		/// </summary>
		/// <param name="uploadState">
		/// The <see cref="UploadState"/> to be merged.
		/// </param>
		/// <remarks>When this method returns, <paramref name="uploadState"/> and the stored <see cref="UploadState"/>
		/// will be equivalent (though not necessarily identical) and either may have changed as a result
		/// of the merge.</remarks>
		public abstract void MergeAndSave(UploadState uploadState);

		/// <summary>
		/// Returns the <see cref="CleanUpIfStaleCallback"/> that NeatUpload should call when a post-back ID
		/// might be stale.
		/// </summary>
		/// <returns>
		/// The <see cref="CleanUpIfStaleCallback"/>.
		/// </returns>
        public virtual CleanUpIfStaleCallback GetCleanUpIfStaleCallback()
        {
            return CleanUpIfStale;
        }

		/// <summary>
		/// Responsible for deleting the <see cref="UploadState"/> for a post-back ID from storage.
		/// </summary>
		/// <param name="postBackID">
		/// The post-back ID to be deleted.
		/// </param>
		/// <remarks>Subclasses should override this method to avoid leaking <see cref="UploadState"/>
		/// objects.</remarks>
        protected virtual void Delete(string postBackID)
        {
        }

		/// <summary>
		/// Calls <see cref="Delete"/> if the <see cref="UploadState"/> for the post-back ID is stale.
		/// </summary>
		/// <param name="postBackID">
		/// The post-back ID of the <see cref="UploadState"/> to delete if stale.
		/// </param>
		/// <remarks>The <see cref="UploadState"/> is considered stale if it has not been updated in the
		/// number of seconds indicated by the stateStaleAfterSeconds attribute of the &lt;neatUpload&gt;
		/// element.</remarks>
        protected void CleanUpIfStale(string postBackID)
        {
            UploadState uploadState = Load(postBackID);
            if (uploadState != null && IsStale(uploadState))
            {
                foreach (UploadedFile f in uploadState.Files)
                    f.Dispose();
                Delete(postBackID);
            }
        }

		/// <summary>
		/// Merges two <see cref="UploadState"/> objects.
		/// </summary>
		/// <param name="uploadState">
		/// The "local" <see cref="UploadState"/> object to merge, and the object that should contain the result
		/// of the merge.
		/// </param>
		/// <param name="storedUploadState">
		/// The stored <see cref="UploadState"/> object to merge, which will be left unchanged.
		/// </param>
		public static void Merge(UploadState uploadState, UploadState storedUploadState)
		{
            if (uploadState == storedUploadState)
            {
                uploadState.IsMerging = true;
                uploadState.OnMerged();
                uploadState.IsMerging = false;
                return;
            }
            if (storedUploadState != null)
            {
                UploadState uploadStateAtLastMerge = uploadState.UploadStateAtLastMerge;
                if (uploadStateAtLastMerge == null)
                    uploadStateAtLastMerge = new UploadState(uploadState.PostBackID);

                uploadState.IsMerging = true;

                if (uploadState.Status < storedUploadState.Status)
                    uploadState.Status = storedUploadState.Status;

                if (uploadState.BytesRead - uploadStateAtLastMerge.BytesRead + storedUploadState.BytesRead > uploadState.BytesTotal)
                    if (log.IsDebugEnabled) log.Debug("Too many bytes read");

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

                if (!uploadState.IsMultiRequest && storedUploadState.IsMultiRequest)
                    uploadState.IsMultiRequest = storedUploadState.IsMultiRequest;

                if (uploadState.ProcessingStateDict == null || uploadState.ProcessingStateDict.Count == 0)
                    uploadState._ProcessingStateDict = storedUploadState._ProcessingStateDict;

            }
			uploadState.OnMerged();
			uploadState.IsMerging = false;
		}

		/// <summary>
		/// Returns true if the specified <see cref="UploadState"/> is considered stale.
		/// </summary>
		/// <param name="uploadState">
		/// The <see cref="UploadState"/> to check for staleness
		/// </param>
		/// <returns>
		/// true if <paramref name="uploadState"/> is stale.
		/// </returns>
		/// <remarks>The <see cref="UploadState"/> is considered stale if it has not been updated in the
		/// number of seconds indicated by the stateStaleAfterSeconds attribute of the &lt;neatUpload&gt;
		/// element or it has been forced stale.</remarks>
		protected bool IsStale(UploadState uploadState)
		{
			return (uploadState.TimeOfLastMerge.AddSeconds(Config.Current.StateStaleAfterSeconds) < DateTime.Now);				
		}

		/// <summary>
		/// A delegate that NeatUpload should call when it thinks an <see cref="UploadState"/> might be stale.
		/// </summary>
        public delegate void CleanUpIfStaleCallback(string postBackID);
	}
}
