/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2006  Dean Brettle

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
using System.Collections.Specialized;
using System.Reflection;
using System.Security.Cryptography;
using System.IO;
using System.Web.UI;
using Brettle.Web.NeatUpload.Internal.Module;

namespace Brettle.Web.NeatUpload
{
	public class ArmoredNameValueCollection : NameValueCollection
	{
		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
		= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/
		protected virtual void Deserialize(Stream s)
		{
			// Deserialize the storageConfig
			System.Web.UI.LosFormatter scFormatter = new System.Web.UI.LosFormatter();
			Hashtable ht = (Hashtable)scFormatter.Deserialize(s);
			
			// Convert to a NameValueCollection.  We only use Hashtable for serialization because
			// LosFormatter can serialize it efficiently.
			if (ht != null)
			{
				foreach (string key in ht.Keys)
				{
					this[key] = (string)ht[key];
				}
			}
		}
		
		protected virtual void Serialize(Stream s)
		{
			// Convert the StorageConfig to a Hashtable because LosFormatter can serialize Hashtables very
			// efficiently.
			Hashtable ht = new Hashtable();
			foreach (string key in Keys)
			{
				ht[key] = this[key];
			}
			LosFormatter scFormatter = new LosFormatter();
			scFormatter.Serialize(s, ht);			
		}

		public void Unprotect(string secureString)
		{
			ObjectProtector.Unprotect(secureString, Deserialize, AssertSignaturesAreEqual);
		}
		
		public string Protect()
		{
			return ObjectProtector.Protect(Serialize);
		}
		
		protected virtual void AssertSignaturesAreEqual(byte[] actualHash, byte[] expectedHash)
		{
			ObjectProtector.AssertSignaturesAreEqual(actualHash, expectedHash);
		}
	}
}
