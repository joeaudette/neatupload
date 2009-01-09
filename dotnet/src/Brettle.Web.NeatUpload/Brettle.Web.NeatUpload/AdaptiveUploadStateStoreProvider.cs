// SessionBasedUploadStateStoreProvider.cs - Part of NeatUpload
//
//  Copyright (C) 2008 Dean Brettle
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
//
//

using System;
using System.Web;
using System.Web.SessionState;
using System.IO;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Acts like <see cref="InProcUploadStateStoreProvider"/> if a the session state mode is Off or
	/// InProc.  Otherwise, acts like <see cref="SessionBasedUploadStateStoreProvider"/>.
	/// </summary>
	/// <remarks>This class
	/// is instantiated by NeatUpload if it is added in the &lt;providers&gt; section of the &lt;neatUpload&gt;
	/// section.  Application developers should not instantiate it directly.</remarks>
	public class AdaptiveUploadStateStoreProvider : UploadStateStoreProvider
	{
		/// <summary>
		/// Initialize this object using the name and attributes.  The name and attributes are
		/// passed to the <see cref="SessionBasedUploadStateStoreProvider"/> that this provider uses
		/// if the session state mode is something other than Off or InProc.
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/> to be passed to the <see cref="SessionBasedUploadStateStoreProvider.Initialize"/>
		/// </param>
		/// <param name="attrs">
		/// A <see cref="System.Collections.Specialized.NameValueCollection"/> to be passed to the <see cref="SessionBasedUploadStateStoreProvider.Initialize"/>
		/// </param>
        public override void Initialize(string name, System.Collections.Specialized.NameValueCollection attrs)
        {
            base.Initialize(name, attrs);
            InProcProvider = new InProcUploadStateStoreProvider();
            SessionBasedProvider = new SessionBasedUploadStateStoreProvider();
            SessionBasedProvider.Initialize(name, attrs);
        }

		/// <value>
		/// A human readable description of this provider.
		/// </value>
        public override string Description { get { return "Delegates to InProcUploadStateStoreProvider if SessionStateMode is Off or InProc, otherwise delegates to SessionBasedUploadStateStoreProvider."; } }

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
        public override UploadState Load(string postBackID)
		{
            return Provider.Load(postBackID);
		}

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
		public override void MergeAndSave(UploadState uploadState)
		{
            Provider.MergeAndSave(uploadState);
		}

		/// <summary>
		/// Returns the <see cref="CleanUpIfStaleCallback"/> that NeatUpload should call when a post-back ID
		/// might be stale.
		/// </summary>
		/// <returns>
		/// The <see cref="CleanUpIfStaleCallback"/>.
		/// </returns>
        public override CleanUpIfStaleCallback GetCleanUpIfStaleCallback()
        {
            return Provider.GetCleanUpIfStaleCallback();
        }

        private object SyncRoot = new object();
        private InProcUploadStateStoreProvider InProcProvider;
        private SessionBasedUploadStateStoreProvider SessionBasedProvider;

        private UploadStateStoreProvider _Provider;
        private UploadStateStoreProvider Provider {
            get
            {
                if (_Provider != null)
                    return _Provider;

                lock (SyncRoot)
                {
                    if (_Provider == null)
                    {
                        HttpSessionState session = HttpContext.Current.Session;
                        SessionStateMode mode;
                        if (session != null)
                        {
                            mode = session.Mode;
                        }
                        else
                        {
                            UriBuilder handlerUriBuilder = new UriBuilder(HttpContext.Current.Request.Url);
                            handlerUriBuilder.Path = HttpContext.Current.Response.ApplyAppPathModifier(SessionBasedProvider.HandlerUrl);
                            mode = (SessionStateMode)SimpleWebRemoting.MakeRemoteCall(handlerUriBuilder.Uri, "GetSessionStateMode");
                        }
                        if (mode == SessionStateMode.Off || mode == SessionStateMode.InProc)
                            _Provider = InProcProvider;
                        else
                        {
                            _Provider = SessionBasedProvider;
                        }
                    }
                    return _Provider;
                }
            }
        }
	}
}
