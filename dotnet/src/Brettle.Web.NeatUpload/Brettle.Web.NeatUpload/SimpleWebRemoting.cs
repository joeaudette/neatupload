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
            context.Response.Flush();
        }

        public static object MakeRemoteCall(Uri uri, params object[] methodCall)
        {
            CookieContainer cookieContainer = new CookieContainer();
            HttpCookieCollection httpCookies = HttpContext.Current.Request.Cookies;
            if (httpCookies != null)
                foreach (string name in httpCookies.AllKeys)
                    cookieContainer.Add(new Cookie(name, httpCookies[name].Value, "/", uri.Host));
			WebClient wc = new WebClient();
            byte[] responseBytes = null;
            try
			{
	            wc.Headers.Add("Cookie", cookieContainer.GetCookieHeader(uri));
	            string protectedRequestPayload = ObjectProtector.Protect(methodCall);
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
                results = (object[])ObjectProtector.Unprotect(protectedResponsePayload);
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
