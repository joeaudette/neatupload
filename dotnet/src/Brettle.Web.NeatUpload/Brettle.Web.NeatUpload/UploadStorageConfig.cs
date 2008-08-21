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

namespace Brettle.Web.NeatUpload
{
	public class UploadStorageConfig : ArmoredNameValueCollection
	{
		protected override void AssertSignaturesAreEqual(byte[] actualHash, byte[] expectedHash)
		{
			if (actualHash.Length != expectedHash.Length)
			{
				throw new InvalidStorageConfigException("actualHash.Length (" + actualHash.Length + ")" +
				                                        " != expectedHash.Length (" + expectedHash.Length + ")");
			}
			for (int i = 0; i < expectedHash.Length; i++)
			{
				if (actualHash[i] != expectedHash[i])
				{
					throw new InvalidStorageConfigException("actualHash[" + i + "] (" + (int)actualHash[i] + ")" +
					                                        " != expectedHash[" + i + "] (" + (int)expectedHash[i] + ")");
				}
			}
		}
	}
}
