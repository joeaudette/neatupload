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
	/// <summary>
	/// An <see cref="IHttpModule"/> that supports this interface and is installed
	/// in the &lt;httpModules&gt; section can be used with
	/// the NeatUpload controls or other controls that use the static methods of
	/// <see cref="UploadModule"/>.
	/// </summary>
	public interface IUploadModule
	{
		/// <summary>
		/// The name of the query parameter that can contain the post-back ID with
		/// which files in the request should be associated.  
		/// </summary>
		/// <value>
		/// The name of the query parameter that can contain the post-back ID with
		/// which files in the request should be associated.  
		/// </value>
		/// <remarks>For example, if
		/// PostBackIDQueryParam is "NeatUpload_PostBackID", then if a request is
		/// received with a query string of "NeatUpload_PostBackID=123ABC", all files
		/// in the request will be assocated with post-back ID "123ABC".  The post-back
		/// ID must not contain the character "-".
		/// </remarks>
		string PostBackIDQueryParam { get; }

		/// <summary>
		/// The prefix for the names of file fields in requests.
		/// </summary>
		/// <value>
		/// The prefix for the names of file fields in requests.
		/// </value>
		/// <remarks>If a file field has a name that starts with this prefix, the prefix
		/// must be followed by the post-back ID, then a "-", then a control's UniqueID.
		/// The file will then be associated with that post-back ID and control UniqueID.
		/// </remarks>
		string FileFieldNamePrefix { get; }

		/// <summary>
		/// The prefix for the names of config fields in requests.
		/// </summary>
		/// <value>
		/// The prefix for the names of config fields in requests.
		/// </value>
		/// <remarks>If a field has a name that starts with this prefix, the prefix must
		/// be followed by a control's UniqueID.  The contents of the field must have
		/// been returned by a previous call to <see cref="Protect"/>.  The module will pass the 
		/// field contents to <see cref="Unprotect"/> and the resulting NameValueCollection
		/// will be used in an implementation way when processing the file fields for
		/// the same control UniqueID.
		/// </remarks>
		string ConfigFieldNamePrefix { get; }

		/// <summary>
		/// Whether the module will handle requests to the same URL as the current 
		/// request.
		/// </summary>
		/// <value>
		/// Whether the module will handle requests to the same URL as the current 
		/// request.
		/// </value>
		bool IsEnabled { get; }

		/// <summary>
		/// Converts a <see cref="String"/> returned by <see cref="Protect"/> back to the 
		/// <see cref="NameValueCollection"/> that was passed to <see cref="Protect"/>
		/// </summary>
		/// <param name="armoredString">
		/// A <see cref="System.String"/> returned by <see cref="Protect"/>
		/// </param>
		/// <returns>
		/// The <see cref="NameValueCollection"/> that was passed to <see cref="Protect"/>
		/// </returns>
		NameValueCollection Unprotect(string armoredString);

		/// <summary>
		/// Converts a <see cref="NameValueCollection"/> to a <see cref="String"/> that
		/// an attacker can not use to access any part of the <see cref="NameValueCollection"/>
		/// and can not change without causing <see cref="Unprotect"/> to fail.  The 
		/// returned <see cref="String"/> can be passed to <see cref="Unprotect"/> to get
		/// the original <see cref="NameValueCollection"/> back.
		/// </summary>
		/// <param name="nvc">
		/// A <see cref="NameValueCollection"/>
		/// </param>
		/// <returns>
		/// The <see cref="System.String"/> that can be passed to <see cref="Unprotect"/> to
		/// get the original <see cref="NameValueCollection"/> back.
		/// </returns>
		/// <remarks>This is used to protect config fields and cookie parameters.</remarks>
		string Protect(NameValueCollection nvc);

		/// <summary>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the post-back ID of the current request.
		/// </summary>
		/// <value>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the post-back ID of the current request.
		/// </value>
		UploadedFileCollection Files { get; }

		/// <summary>
		/// The post-back ID associated with the current request, or null if there was none.
		/// </summary>
		/// <value>
		/// The post-back ID associated with the current request, or null if there was none.
		/// </value>
		string PostBackID { get; }

		/// <summary>
		/// Sets the processing state object associated with the specified post-back ID and
		/// control UniqueID.  The processing state object can be retrieved by passing an
		/// <see cref="IUploadProgressState"/> object to <see cref="BindProgressState"/> and
		/// then accessing <see cref="IUploadProgressState.ProcessingState"/>.
		/// </summary>
		/// <param name="postBackID">
		/// The post-back ID that the processing state is associated with.
		/// </param>
		/// <param name="controlUniqueID">
		/// The UniqueID of the control that the processing state is associated with.
		/// </param>
		/// <param name="state">
		/// A <see cref="System.Object"/> that represents the processing state.  This must be
		/// serializable.
		/// </param>
		void SetProcessingState(string postBackID, string controlUniqueID, object state);

		/// <summary>
		/// Fills in an <see cref="IUploadProgressState"/> object with the progress state for
		/// a given post-back ID and control UniqueID.
		/// </summary>
		/// <param name="postBackID">
		/// The post-back ID for which the progress state should be retrieved.
		/// </param>
		/// <param name="controlUniqueID">
		/// The UniqueID of the control for which the progress state should be retrieved.
		/// </param>
		/// <param name="progressState">
		/// A <see cref="IUploadProgressState"/> to be filled in with the progress state
		/// for the given post-back ID and control UniqueID.
		/// </param>
		void BindProgressState(string postBackID, string controlUniqueID, IUploadProgressState progressState);

		/// <summary>
		/// Cancels the upload specified by the given post-back ID.
		/// </summary>
		/// <param name="postBackID">
		/// The post-back ID of the upload to cancel.
		/// </param>
		/// <remarks>The module should attempt to stop the upload if possible.  Calling
		/// <see cref="BindProgressState"/> after calling this method must cause 
		/// <see cref="IUploadProgressState.Status"/> to be <see cref="UploadStatus.Cancelled"/>.
		/// </remarks>
		void CancelPostBack(string postBackID);

		/// <summary>
		/// Converts an <see cref="HttpPostedFile"/> to an <see cref="UploadedFile"/>
		/// that is associated with a particular control.
		/// </summary>
		/// <param name="controlUniqueID">
		/// The UniqueID of the control with which the returned <see cref="UploadedFile"/>
		/// should be associated.  If the <see cref="UploadedFile"/> is added to an
		/// <see cref="UploadedFileCollection"/>, the UniqueID can be used to retrieve
		/// it.
		/// </param>
		/// <param name="file">
		/// The <see cref="HttpPostedFile"/> to convert to an <see cref="UploadedFile"/>.
		/// </param>
		/// <returns>
		/// The <see cref="UploadedFile"/> that corresponds to the <see cref="HttpPostedFile"/>.
		/// </returns>
		UploadedFile ConvertToUploadedFile(string controlUniqueID, HttpPostedFile file);
	}
}
