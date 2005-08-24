/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005  Dean Brettle

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
using System.IO;
using System.Web;

namespace Brettle.Web.NeatUpload
{
	public class FormContext
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private const string formContextKey = UploadContext.ContextItemKeyPrefix + "FormContext";
		
		internal static FormContext Current
		{
			get {
				FormContext formContext = HttpContext.Current.Items[formContextKey] as FormContext;
				if (formContext == null)
				{
					formContext = new FormContext();
					HttpContext.Current.Items[formContextKey] = formContext;
				}
				return formContext;
			}
		}
		
		internal string PostBackID;
		
		internal FormContext()
		{
			// Create a secure GUID - a 128-bit secure random number
			System.Security.Cryptography.RandomNumberGenerator rng 
				= System.Security.Cryptography.RandomNumberGenerator.Create();
			byte[] randomBytes = new byte[16];
			rng.GetBytes(randomBytes);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < randomBytes.Length; i++)
			{
				sb.AppendFormat("{0:X2}", randomBytes[i]);
			}			
			string guid = sb.ToString();
			PostBackID = UploadContext.NamePrefix + guid;
			log.Debug("PostBackID=" + PostBackID);
		}
		
		internal string GenerateFileID(string controlUniqueID)
		{
			return PostBackID + "-" + controlUniqueID;
		}
		
	}
}
