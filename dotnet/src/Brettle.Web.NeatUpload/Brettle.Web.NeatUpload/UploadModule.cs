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
using System.Web;
using System.Collections.Specialized;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Provides an API (via its static members) that upload controls can use 
	/// to access the currently installed upload module.
	/// The members of this class mostly delegate to the corresponding members 
	/// of the <see cref="IUploadModule"/> that is installed in the
	/// &lt;httpModules&gt; section of the Web.config.
	/// </summary>
	public class UploadModule
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
		public static string PostBackIDQueryParam {
			get { return InstalledModule.PostBackIDQueryParam; }
		}

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
		public static string FileFieldNamePrefix {
			get { return InstalledModule.FileFieldNamePrefix; }
		}

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
		public static string ConfigFieldNamePrefix {
			get { return InstalledModule.ConfigFieldNamePrefix; }
		}

		/// <summary>
		/// Whether a module is installed and will handle requests to the same URL
		/// as the current request.
		/// </summary>
		/// <value>
		/// Whether a module is installed and will handle requests to the same URL
		/// as the current request.
		/// </value>
		public static bool IsEnabled {
			get { return (InstalledModule != null && InstalledModule.IsEnabled); }
		}

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
		public static NameValueCollection Unprotect(string armoredString)
		{
			return InstalledModule.Unprotect(armoredString);
		}

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
		public static string Protect(NameValueCollection nvc)
		{
			return InstalledModule.Protect(nvc);
		}

		/// <summary>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the post-back ID of the current request.
		/// </summary>
		/// <value>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the post-back ID of the current request.
		/// </value>
		public static UploadedFileCollection Files {
			get { return InstalledModule.Files; }
		}

		/// <summary>
		/// The post-back ID associated with the current request, or null if there was none.
		/// </summary>
		/// <value>
		/// The post-back ID associated with the current request, or null if there was none.
		/// </value>
		public static string PostBackID {
			get { return InstalledModule.PostBackID; }
		}

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
		public static void SetProcessingState(string postBackID, string controlUniqueID, object state)
		{
			InstalledModule.SetProcessingState(postBackID, controlUniqueID, state);
		}

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
		public static void BindProgressState(string postBackID, string controlUniqueID, IUploadProgressState progressState)
		{
			InstalledModule.BindProgressState(postBackID, controlUniqueID, progressState);
		}

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
		public static void CancelPostBack(string postBackID)
		{
			InstalledModule.CancelPostBack(postBackID);
		}

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
		/// <remarks>If an <see cref="IUploadModule"/> is not installed, this method will
		/// wrap the <paramref name="file"/> in an <see cref="UploadedFile"/> subclass that
		/// delegates all members to the corresponding members of <paramref name="file"/></remarks>
		public static UploadedFile ConvertToUploadedFile(string controlUniqueID, HttpPostedFile file)
		{
			if (InstalledModule != null)
				return InstalledModule.ConvertToUploadedFile(controlUniqueID, file);
			else
				return new AspNetUploadedFile(controlUniqueID, file);
		}

		/// <summary>
		/// The name of the field in the pre-async-upload request that should contain the 
		/// space-delimited list of file sizes for the files that will be uploaded in the
		/// coming async-upload requests.
		/// </summary>
		/// <value>
		/// The name of the field in the pre-async-upload request that should contain the 
		/// space-delimited list of file sizes for the files that will be uploaded in the
		/// coming async-upload requests.
		/// </value>
		public static string FileSizesFieldName {
			get { return InstalledModule.FileSizesFieldName; }
		}

		/// <summary>
		/// The path (relative to the app root) to which the pre-async-upload request and
		/// subsequent async-upload request should be sent.
		/// </summary>
		/// <value>
		/// The path (relative to the app root) to which the pre-async-upload request and
		/// subsequent async-upload request should be sent.
		/// </value>
		public static string AsyncUploadPath {
			get { return InstalledModule.AsyncUploadPath; }
		}

		/// <summary>
		/// The name of the query parameter that must be present for the pre-async-upload
		/// request and each async-upload request, and which must contain the control UniqueID
		/// that the async uploads are to be associated with. 
		/// </summary>
		/// <value>
		/// The name of the query parameter that must be present for the pre-async-upload
		/// request and each async-upload request, and which must contain the control UniqueID
		/// that the async uploads are to be associated with. 
		/// </value>
		/// <remarks>For example, if
		/// AsyncControlIDQueryParam is "NeatUpload_AsyncControlID", then if a request is
		/// received with a query string of "NeatUpload_AsyncControlID=123ABC", all files
		/// or pre-async-upload data in the request will be assocated with the control with
		/// UniqueID "123ABC".  Async-upload requests and pre-async-upload requests must also
		/// specify the post-back ID in a separate query param whose name is given by the value
		/// of <see cref="PostBackIDQueryParam"/>.
		/// </remarks>
		public static string AsyncControlIDQueryParam {
			get { return InstalledModule.AsyncControlIDQueryParam; }
		}
		
		/// <summary>
		/// The name of the query parameter that can contain "protected" cookies that
		/// the module should use if it needs to make HTTP requests while processing a
		/// pre-async-upload or async-upload request.
		/// </summary>
		/// <value>
		/// The name of the query parameter that can contain "protected" cookies that
		/// the module should use if it needs to make HTTP requests while processing a
		/// pre-async-upload or async-upload request.
		/// </value>
		/// <remarks>This is needed because the process making the pre-async-upload request
		/// or async-upload requests might be different from the browser that displays the rest
		/// of the web application.  For example, Flash always sends IE's cookies which means
		/// that the correct cookies are not sent for Firefox users.  The value of the query 
		/// parameter must be the value returned by
		/// <see cref="Protect"/> when it is passed a <see cref="NameValueCollection"/>
		/// that maps the cookie names to cookie values.</remarks>
		public static string ArmoredCookiesQueryParam {
			get { return InstalledModule.ArmoredCookiesQueryParam; }
		}
		
		private static bool _IsInstalled = true;
		private static IUploadModule _InstalledModule;
		private static IUploadModule InstalledModule {
			get {
				if (!_IsInstalled) 
					return null;
				if (_InstalledModule == null)
				{
					HttpModuleCollection modules = HttpContext.Current.ApplicationInstance.Modules;
					foreach (string moduleName in modules.AllKeys)
					{
						IHttpModule module = modules[moduleName];
						if (module is IUploadModule)
						{
							_InstalledModule = (IUploadModule) module;
							break;
						}
					}
					if (_InstalledModule == null) 
						_IsInstalled = false;
				}
				return _InstalledModule;
			}
		}
	}
}
