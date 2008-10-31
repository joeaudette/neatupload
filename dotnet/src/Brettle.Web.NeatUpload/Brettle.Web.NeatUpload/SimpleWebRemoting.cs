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
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{	
	public class SimpleWebRemoting
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public delegate object MethodCallHandler(string methodName, object[] args);

        public static void ProcessRemoteCallRequest(HttpContext context, MethodCallHandler methodCallHandler)
        {
            string protectedPayload = context.Request.Params["ProtectedPayload"];
            object[] methodCall = (object[])ObjectProtector.Unprotect(protectedPayload);
            object[] args = new object[methodCall.Length - 1];
            Array.Copy(methodCall, 1, args, 0, args.Length);
            object retVal = methodCallHandler((string)methodCall[0], args);
            ArrayList resultsArray = new ArrayList();
            resultsArray.Add(retVal);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is ICopyFromObject)
                    resultsArray.Add(args[i]);
            }

            string responseBody = ObjectProtector.Protect(resultsArray.ToArray());
            context.Response.ContentType = "application/octet-stream";
            context.Response.Write(responseBody);
        }

        public static object MakeRemoteCall(Uri uri, params object[] methodCall)
        {
            HttpCookieCollection httpCookies = HttpContext.Current.Request.Cookies;
            return MakeRemoteCall(uri, httpCookies, Config.Current.EncryptionKey, Config.Current.ValidationKey,
                                    methodCall);
        }

        internal static object MakeRemoteCall(Uri uri, HttpCookieCollection httpCookies, byte[] encryptionKey, byte[] validationKey, 
                                            params object[] methodCall)
        {
            CookieContainer cookieContainer = new CookieContainer();
            if (httpCookies != null)
                foreach (string name in httpCookies.AllKeys)
                {
                    string quotedCookieValue = httpCookies[name].Value;
                    if (quotedCookieValue.IndexOfAny(new char[] {',', ';'}) != -1)
                        quotedCookieValue = "\"" + quotedCookieValue.Replace("\"", "\\\"") + "\"";
                    try
                    {
                        cookieContainer.Add(new Cookie(name, quotedCookieValue, "/", uri.Host));
                    }
                    catch (Exception ex)
                    {
                        // We typically only need to use cookies that are used to identify the session
                        // so if other cookies throw exceptions, it is best to just ignore them.
                        if (log.IsDebugEnabled) log.DebugFormat("Ignore exception thrown by CookieContainer.Add(): {0}", ex);
                    }
                }
			WebClient wc = new WebClient();
            byte[] responseBytes = null;
            try
			{
	            wc.Headers.Add("Cookie", cookieContainer.GetCookieHeader(uri));
	            string protectedRequestPayload = ObjectProtector.Protect(methodCall, encryptionKey, validationKey);
	            NameValueCollection formValues = new NameValueCollection();
	            formValues.Add("ProtectedPayload", protectedRequestPayload);
	            responseBytes = wc.UploadValues(uri.ToString(), formValues);
			}
			catch (Exception ex)
			{
				log.Error(String.Format("Caught exception while making call to {0} at {1}", methodCall[0], uri), 
				          ex);
				throw;
			}
			finally
			{
				wc.Dispose();
			}
            string protectedResponsePayload = System.Text.Encoding.ASCII.GetString(responseBytes);
            if (protectedResponsePayload != null && protectedResponsePayload.Length > 0)
            {
                object[] results = null;
                results = (object[])ObjectProtector.Unprotect(protectedResponsePayload, encryptionKey, validationKey);
                int j = 1;
                for (int i = 1; i < methodCall.Length; i++)
                {
                    ICopyFromObject copyFromObject = methodCall[i] as ICopyFromObject;
                    if (copyFromObject != null)
                        copyFromObject.CopyFrom(results[j++]);
                }
                return results[0];
            }
            return null;
        }
    }
}
