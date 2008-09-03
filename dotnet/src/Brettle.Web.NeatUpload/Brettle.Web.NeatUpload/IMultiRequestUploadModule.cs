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
	/// An <see cref="IUploadModule"/> that treats a sequence of 
	/// special requests as a single combined upload.
	/// </summary>
	/// <remarks>The module treats the following sequence of requests as a single
    /// upload:
	/// <list type="number">
	///   <item>An initial POST request sent to the
	///     <see cref="UploadPath"/> containing:
	///     <list type="bullet">
	///       <item>a post-back ID in the query parameter named by
	///         <see cref="IUploadModule.PostBackIDQueryParam"/></item>
	///       <item>a control UniqueID in the query parameter named by
	///         <see cref="ControlIDQueryParam"/></item>
	///       <item>"protected" cookies needed for authentication or session 
	///         identification in the query parameter named by
	///         <see cref="ArmoredCookiesQueryParam"/></item>
	///       <item>a space-delimited list of file sizes in the form field named by
	///         <see cref="IUploadModule.FileSizesFieldName"/></item>
	///       <item>(optionally) a form field with a name starting with
	///         <see cref="IUploadModule.ConfigFieldNamePrefix"/> followed by 
	///         the same control UniqueID as above, containing "protected" 
	///         module-specific configuration information.</item>
	///     </list>
	///   </item>
	///   <item>A sequence of upload request (one for each file size in 
	///     the file sizes field of the initial request) to the
	///     <see cref="UploadPath"/> containing:
	///     <list type="bullet">
	///       <item>a post-back ID in the query parameter named by
	///         <see cref="IUploadModule.PostBackIDQueryParam"/></item>
	///       <item>a control UniqueID in the query parameter named by
	///         <see cref="ControlIDQueryParam"/></item>
	///       <item>"protected" cookies needed for authentication or session 
	///         identification in the query parameter named by
	///         <see cref="ArmoredCookiesQueryParam"/></item>
	///     </list>
	///   </item>
	///   <item>A final form submission upload request to any path for which 
	///     <see cref="IUploadModule.IsEnabled"/> returns true.  This request
	///     must contain the postback ID in one of the following locations:
	///     <list type="bullet">
	///       <item>in the query parameter named by
	///         <see cref="IUploadModule.PostBackIDQueryParam"/></item>
	///       <item>in the form field named by
	///         <see cref="IUploadModule.PostBackIDFieldName"/></item>
	///       <item>in a file field name prefixed by 
	///         <see cref="IUploadModule.FileFieldNamePrefix"/></item>
	///     </list>
	///     While this request is being handled, the module will make all the files
	///     associated with the post-back ID available via <see cref="IUploadModule.Files"/>.
	///   </item>
	/// </list>
	/// </remarks>
	public interface IMultiRequestUploadModule : IUploadModule
	{
		/// <summary>
		/// The name of the field in the initial request that should contain the 
		/// space-delimited list of file sizes for the files that will be uploaded in the
		/// coming requests.
		/// </summary>
		/// <value>
		/// The name of the field in the initial request that should contain the 
		/// space-delimited list of file sizes for the files that will be uploaded in the
		/// coming requests.
		/// </value>
		string FileSizesFieldName { get; }

		/// <summary>
		/// The path (relative to the app root) to which all but the final request should
		/// be sent.
		/// </summary>
		/// <value>
		/// The path (relative to the app root) to which all but the final request should
		/// be sent.
		/// </value>
		string UploadPath { get; }

		/// <summary>
		/// The name of the query parameter that must be present for all but the final
		/// request, and which must contain the control UniqueID
		/// that the requests are to be associated with. 
		/// </summary>
		/// <value>
		/// The name of the query parameter that must be present for all but the final
		/// request, and which must contain the control UniqueID
		/// that the requests are to be associated with. 
		/// </value>
		/// <remarks>For example, if
		/// ControlIDQueryParam is "NeatUpload_MultiRequestControlID", then if a request is
		/// received with a query string of "NeatUpload_MultiRequestControlID=123ABC", all files
		/// or other upload data in the request will be assocated with the control with
		/// UniqueID "123ABC".  All requests except the final request must also
		/// specify the post-back ID in a separate query param whose name is given by the value
		/// of <see cref="PostBackIDQueryParam"/>.  The final request must
		/// contain the postback ID in one of the following locations:
		/// <list type="bullet">
		///   <item>in the query parameter named by
		///     <see cref="PostBackIDQueryParam"/></item>
		///   <item>in the form field named by
		///     <see cref="PostBackIDFieldName"/></item>
		///   <item>in a file field name prefixed by 
		///     <see cref="FileFieldNamePrefix"/></item>
		/// </list>
		/// </remarks>
		string ControlIDQueryParam { get; }

		/// <summary>
		/// The name of the query parameter that can contain "protected" cookies that
		/// the module should use if it needs to make HTTP requests while processing a
		/// request.
		/// </summary>
		/// <value>
		/// The name of the query parameter that can contain "protected" cookies that
		/// the module should use if it needs to make HTTP requests while processing a
		/// request.
		/// </value>
		/// <remarks>This is needed because the process making the request
		/// might be different from the browser that displays the rest
		/// of the web application.  For example, Flash always sends IE's cookies which means
		/// that the correct cookies are not sent for Firefox users.  The value of the query 
		/// parameter must be the value returned by
		/// <see cref="Protect"/> when it is passed a <see cref="NameValueCollection"/>
		/// that maps the cookie names to cookie values.  For security reasons, the module 
		/// must only use the cookie when processing requests to the 
		/// <see cref="UploadPath"/>.</remarks>
		string ArmoredCookiesQueryParam { get; }		
	}
}
