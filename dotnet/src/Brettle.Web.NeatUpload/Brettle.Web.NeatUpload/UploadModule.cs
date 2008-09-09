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
using Brettle.Web.NeatUpload.Internal.UI;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Provides an API (via its static members) that upload controls can use 
	/// to access the currently installed upload module.
	/// The members of this class mostly delegate to the corresponding members 
	/// of the <see cref="IUploadModule"/> that is installed in the
	/// &lt;httpModules&gt; section of the Web.config.
	/// </summary>
	/// <remarks>
	/// When <see cref="IsEnabled"/> returns true, an <see cref="IUploadModule"/>
	/// is installed in the &lt;httpModules&gt; section of the Web.config and will
	/// handle POST requests to the current request's URL
	/// that have a Content-Type header of
	/// "multipart/form-data" and contain a post-back ID in one
	/// of the following locations:
	/// <list type="bullet">
	///   <item>If the post-back ID is in a query parameter named by
	///     <see cref="PostBackIDQueryParam"/>, all files in the request will
	///     be associated with the post-back ID.  The files can be retrieved
	///     from the <see cref="Files"/> collection using the names of the 
	///     file fields in the request.  These names are typically the 
	///     UniqueIDs of the controls that uploaded the files.</item>
	///   <item>If the post-back ID is in the form field named by
	///     <see cref="PostBackIDFieldName"/>, all files that occur after 
	///     form field will be associated with the post-back ID.    
	///     Those files can be retrieved
	///     from the <see cref="Files"/> collection using the names of the 
	///     file fields in the request.  These names are typically the 
	///     UniqueIDs of the controls that uploaded the files.</item>
	///   <item>If the post-back ID is in a file field name prefixed by 
	///     <see cref="FileFieldNamePrefix"/>, that file will be associated
	///     with the post-back ID.  That file can be retrieved
	///     from the <see cref="Files"/> collection using the portion of
	///     the file field name after the first "-".  That portion of the name
	///     typically corresponds to the UniqueID of the controls that uploaded
	///     the files.</item>
	/// </list>
	/// For requests that specify a post-back ID, the module can also use "protected"
	/// configuration information associated with a particular file if the
	/// file field is preceded in the request by a
	/// field with a name consisting of the value <see cref="ConfigFieldNamePrefix"/>
	/// followed by the files key in <see cref="Files"/> collection (typically the
	/// uploading control's UniqueID).
	/// 
	/// <para>In addition, <see cref="BindProgressState"/> provides information 
	/// concerning the progress of the upload,
	/// <see cref="CancelPostBack"/> tells the module it should ignore the
	/// remainder of the upload, <see cref="SetProcessingState"/> associates an
	/// arbitrary object with an upload after it has been received but before the
	/// end of the request, and <see cref="ConvertToUploadedFile"/> to convert an
	/// <see cref="HttpPostedFile"/> into an <see cref="UploadedFile"/></para>
	/// </remarks>
	public class UploadModule
	{
		// Only static members...
		protected UploadModule() { }
		
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
		/// The name of the form field that can contain the post-back ID with
		/// which all subsequent files in the request should be associated.  
		/// </summary>
		/// <value>
		/// The name of the form field that can contain the post-back ID with
		/// which all subsequent files in the request should be associated.  
		/// </value>
		/// <remarks>For example, if
		/// PostBackIDFieldName is "NeatUpload_PostBackID", then if a request contains
		/// a form field named "NeatUpload_PostBackID" with a value of "123ABC", all 
		/// subsequent files
		/// in the request will be assocated with post-back ID "123ABC".  The post-back
		/// ID must not contain the character "-".
		/// </remarks>
		public static string PostBackIDFieldName {
			get { return InstalledModule.PostBackIDFieldName; }
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
        /// been returned by a previous call to <see cref="UploadStorageConfig.Protect"/>.  
        /// The module will pass the 
        /// field contents to <see cref="UploadStorageConfig.Unprotect"/> of an object
        /// returned by <see cref="UploadModule.CreateUploadStorageConfig"/> and
        /// will be used in an implementation dependent way when processing the file
        /// fields for the same control UniqueID.
        /// </remarks>
        public static string ConfigFieldNamePrefix
        {
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
        /// Creates and returns a new <see cref="UploadStorageConfig"/> (or 
        /// subclass) for use with the current request.
        /// </summary>
        /// <returns>a new <see cref="UploadStorageConfig"/> or subclass.</returns>
        /// <remarks>If the installed module does not explicitly support
        /// creating <see cref="UploadStorageConfig"/> objects, this method returns
        /// the <see cref="UploadStorageConfig"/> created by the currently selected
        /// <see cref="UploadStorageProvider"/>.</remarks>
		public static UploadStorageConfig CreateUploadStorageConfig()
		{
			UploadStorageConfig storageConfig = InstalledModule.CreateUploadStorageConfig();
            if (storageConfig == null)
                storageConfig = UploadStorage.CreateUploadStorageConfig();
            return storageConfig;
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
		/// Sets the processing state object associated with the current upload and
		/// given control UniqueID.  The processing state object can be retrieved by passing an
		/// <see cref="IUploadProgressState"/> object to <see cref="BindProgressState"/> and
		/// then accessing <see cref="IUploadProgressState.ProcessingState"/>.
		/// </summary>
		/// <param name="controlUniqueID">
		/// The UniqueID of the control that the processing state is associated with.
		/// </param>
		/// <param name="state">
		/// A <see cref="System.Object"/> that represents the processing state.  This must be
		/// serializable.
		/// </param>
		/// <returns>true if the module supports setting processing state, otherwise false.</returns>
		public static bool SetProcessingState(string controlUniqueID, object state)
		{
			return InstalledModule.SetProcessingState(controlUniqueID, state);
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
        /// <remarks>The progress state is cached for the length of the current request to
        /// ensure consistency across multiple calls during a single request and to reduce 
        /// the number of calls made
        /// to the underlying <see cref="IUploadModule"/> implementation since some 
        /// implementations might need to access the network to determine the 
        /// progress state.</remarks>
		public static void BindProgressState(string postBackID, string controlUniqueID, IUploadProgressState progressState)
		{
            HttpContext ctx = HttpContext.Current;
            if (ctx != null)
            {
                InstalledModule.BindProgressState(postBackID, controlUniqueID, progressState);
                return;
            }

            string cacheKey = String.Format("NeatUpload_cachedProgressState_{0}-{1}", postBackID, controlUniqueID);
            UploadProgressState cachedProgressState = ctx.Items[cacheKey] as Internal.UI.UploadProgressState;
            if (cachedProgressState == null)
            {
                cachedProgressState = new UploadProgressState();
                InstalledModule.BindProgressState(postBackID, controlUniqueID, cachedProgressState);
                ctx.Items[cacheKey] = cachedProgressState;
            }
            UploadProgressState.Copy(cachedProgressState, progressState);
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
		/// <remarks>If an <see cref="IUploadModule"/> is not installed or it does not support
		/// conversion, this method will
		/// wrap the <paramref name="file"/> in an <see cref="UploadedFile"/> subclass that
		/// delegates all members to the corresponding members of <paramref name="file"/></remarks>
		public static UploadedFile ConvertToUploadedFile(string controlUniqueID, HttpPostedFile file)
		{
			UploadedFile uploadedFile = null;
			if (InstalledModule != null)
				uploadedFile = InstalledModule.ConvertToUploadedFile(controlUniqueID, file);
			if (uploadedFile == null)
				uploadedFile = new AspNetUploadedFile(controlUniqueID, file);
			return uploadedFile;
		}

        /// <summary>
        /// Adds a message to the IIS log file.
        /// </summary>
        /// <param name="param">The message to add.</param>
        /// <remarks>If the implementation does not suppport this method, it does nothing.</remarks>
        public static void AppendToLog(string param)
        {
            InstalledModule.AppendToLog(param);
        }

		private static bool _IsInstalled = true;
		private static IUploadModule _InstalledModule;
		internal static IUploadModule InstalledModule {
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
