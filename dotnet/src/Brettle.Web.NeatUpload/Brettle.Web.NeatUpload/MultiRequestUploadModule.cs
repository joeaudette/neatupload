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
	/// to do multi-request uploads if the currently installed upload module 
	/// implements <see cref="IMultiRequestUploadModule"/>.
	/// The members of this class mostly delegate to the corresponding members 
	/// of the <see cref="IMultiRequestUploadModule"/> that is installed in the
	/// &lt;httpModules&gt; section of the Web.config.
	/// </summary>
	/// <remarks>The module treats the following sequence of requests as a single
    /// upload:
	/// <list type="number">
	///   <item>An initial POST request sent to the
	///     <see cref="UploadPath"/> containing:
	///     <list type="bullet">
	///       <item>a post-back ID in the query parameter named by
	///         <see cref="UploadModule.PostBackIDQueryParam"/></item>
	///       <item>a control UniqueID in the query parameter named by
	///         <see cref="ControlIDQueryParam"/></item>
	///       <item>"protected" cookies needed for authentication or session 
	///         identification in the query parameter named by
	///         <see cref="ArmoredCookiesQueryParam"/></item>
	///       <item>a space-delimited list of file sizes in the form field named by
	///         <see cref="UploadModule.FileSizesFieldName"/></item>
	///       <item>(optionally) a form field with a name starting with
	///         <see cref="UploadModule.ConfigFieldNamePrefix"/> followed by 
	///         the same control UniqueID as above, containing "protected" 
	///         module-specific configuration information.</item>
	///     </list>
	///   </item>
	///   <item>A sequence of upload request (one for each file size in 
	///     the file sizes field of the initial request) to the
	///     <see cref="UploadPath"/> containing:
	///     <list type="bullet">
	///       <item>a post-back ID in the query parameter named by
	///         <see cref="UploadModule.PostBackIDQueryParam"/></item>
	///       <item>a control UniqueID in the query parameter named by
	///         <see cref="ControlIDQueryParam"/></item>
	///       <item>"protected" cookies needed for authentication or session 
	///         identification in the query parameter named by
	///         <see cref="ArmoredCookiesQueryParam"/></item>
	///     </list>
	///   </item>
	///   <item>A final form submission upload request to any path for which 
	///     <see cref="UploadModule.IsEnabled"/> returns true.  This request
	///     must contain the postback ID in one of the following locations:
	///     <list type="bullet">
	///       <item>in the query parameter named by
	///         <see cref="UploadModule.PostBackIDQueryParam"/></item>
	///       <item>in the form field named by
	///         <see cref="UploadModule.PostBackIDFieldName"/></item>
	///       <item>in a file field name prefixed by 
	///         <see cref="UploadModule.FileFieldNamePrefix"/></item>
	///     </list>
	///     While this request is being handled, the module will make all the files
	///     associated with the post-back ID available via <see cref="UploadModule.Files"/>.
	///   </item>
	/// </list>
	/// </remarks>
	public class MultiRequestUploadModule : UploadModule
	{
		// Only static members...
		protected MultiRequestUploadModule() { }
		
		/// <summary>
		/// Whether an <see cref="IMultRequestUploadModule"/> is installed and 
		/// will handle requests to the same URL as the current request.
		/// </summary>
		/// <value>
		/// Whether an <see cref="IMultRequestUploadModule"/> is installed and 
		/// will handle requests to the same URL as the current request.
		/// </value>
		public static bool IsEnabled {
			get { return (InstalledModule != null && InstalledModule.IsEnabled); }
		}

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
		public static string FileSizesFieldName {
			get { return InstalledModule.FileSizesFieldName; }
		}

		/// <summary>
		/// The path (relative to the app root if starts with ~) to which all but the final request should
		/// be sent.
		/// </summary>
		/// <value>
        /// The path (relative to the app root if starts with ~) to which all but the final request should
		/// be sent.
		/// </value>
		public static string UploadPath {
			get { return InstalledModule.UploadPath; }
		}

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
		public static string ControlIDQueryParam {
			get { return InstalledModule.ControlIDQueryParam; }
		}
		
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
		public static string ArmoredCookiesQueryParam {
			get { return InstalledModule.ArmoredCookiesQueryParam; }
		}

        /// <summary>
        /// Gets a protected string to use as the value of the <see cref="ArmoredCookiesQueryParam"/>
        /// when making the requests in a multi-request upload.
        /// </summary>
        /// <returns>the protected string representing the cookies.</returns>
        /// <remarks>If the installed module does not explicitly support armored
        /// cookies, NeatUpoad will create an <see cref="NameValueCollection"/> 
        /// containing the cookie names/values that ASP.NET uses for session ID and forms
        /// auth, and will pass it to <see cref="ObjectProtector.Protect"/>.
        /// </remarks>
        public static string GetArmoredCookies()
        {
            string armoredCookies = InstalledModule.GetArmoredCookies();
            if (armoredCookies == null)
            {
                HttpCookieCollection cookies = HttpContext.Current.Request.Cookies;
                NameValueCollection authCookies = new NameValueCollection();
                string[] cookieNames
                    = new string[] { "ASP.NET_SESSIONID", "ASPSESSION", System.Web.Security.FormsAuthentication.FormsCookieName };
                foreach (string cookieName in cookieNames)
                {
                    HttpCookie cookie = cookies[cookieName];
                    if (cookie != null)
                        authCookies.Add(cookieName, cookie.Value);
                }

                armoredCookies = ObjectProtector.Protect(authCookies);
            }
            return armoredCookies;
        }

        private static IMultiRequestUploadModule InstalledModule
        {
			get {
				return UploadModule.InstalledModule as IMultiRequestUploadModule;
			}
		}
	}
}
