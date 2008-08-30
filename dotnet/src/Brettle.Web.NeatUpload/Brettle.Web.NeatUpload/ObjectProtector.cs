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
	public class ObjectProtector
	{
		// Static methods only
		private ObjectProtector() { }
		
		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
		= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/

		public delegate void SignatureChecker(byte[] actualHash, byte[] expectedHash);
		public delegate void Deserializer(Stream s);
		public delegate void Serializer(Stream s);

		public static object Unprotect(string secureString)
		{
			SelfSerializingObject obj = new SelfSerializingObject();
			Unprotect(secureString, obj.Deserialize, AssertSignaturesAreEqual);
			return obj.Obj;
		}
		
		public static string Protect(object objectToSerialize)
		{
			SelfSerializingObject obj = new SelfSerializingObject();
			obj.Obj = objectToSerialize;
			return Protect(obj.Serialize);
		}

		public static void Unprotect(string secureString, Deserializer deserializer, SignatureChecker sigChecker)
		{			
			byte[] secureBytes = Convert.FromBase64String(secureString);
			MemoryStream secureStream = new MemoryStream(secureBytes);
			BinaryReader binaryReader = new BinaryReader(secureStream);
			byte[] actualHash = binaryReader.ReadBytes(binaryReader.ReadByte());
			byte[] iv = binaryReader.ReadBytes(binaryReader.ReadByte());
			byte[] cipherText = binaryReader.ReadBytes((int)(secureStream.Length - secureStream.Position));
			
			// Verify the hash
			KeyedHashAlgorithm macAlgorithm = KeyedHashAlgorithm.Create();
			macAlgorithm.Key = Config.Current.ValidationKey;
			byte[] expectedHash = macAlgorithm.ComputeHash(cipherText);
			sigChecker(actualHash, expectedHash);
			
			// Decrypt the ciphertext
			MemoryStream cipherTextStream = new MemoryStream(cipherText);
			SymmetricAlgorithm cipher = SymmetricAlgorithm.Create();
			cipher.Mode = CipherMode.CBC;
			cipher.Padding = PaddingMode.PKCS7;
			cipher.Key = Config.Current.EncryptionKey;
			cipher.IV = iv;
			CryptoStream cryptoStream = new CryptoStream(cipherTextStream, cipher.CreateDecryptor(), CryptoStreamMode.Read);
			try
			{
				deserializer(cryptoStream);
			}
			finally
			{
				cryptoStream.Close();
			}
		}
		
		public static string Protect(Serializer serializer)
		{
			// Encrypt it
			MemoryStream cipherTextStream = new MemoryStream();
			SymmetricAlgorithm cipher = SymmetricAlgorithm.Create();
			cipher.Mode = CipherMode.CBC;
			cipher.Padding = PaddingMode.PKCS7;
			cipher.Key = Config.Current.EncryptionKey;
			CryptoStream cryptoStream = new CryptoStream(cipherTextStream, cipher.CreateEncryptor(), CryptoStreamMode.Write);
			serializer(cryptoStream);
			cryptoStream.Close();
			byte[] cipherText = cipherTextStream.ToArray();
			
			// MAC the ciphertext
			KeyedHashAlgorithm macAlgorithm = KeyedHashAlgorithm.Create();
			macAlgorithm.Key = Config.Current.ValidationKey;
			byte[] hash = macAlgorithm.ComputeHash(cipherText);
			
			// Concatenate MAC length, MAC, IV length, IV, and ciphertext into an array.
			MemoryStream secureStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(secureStream);
			binaryWriter.Write((byte)hash.Length);
			binaryWriter.Write(hash);
			binaryWriter.Write((byte)cipher.IV.Length);
			binaryWriter.Write(cipher.IV);
			binaryWriter.Write(cipherText);
			binaryWriter.Close();
			
			// return Base64-encoded value suitable for putting in a hidden form field
			return Convert.ToBase64String(secureStream.ToArray());
		}
		
		public static void AssertSignaturesAreEqual(byte[] actualHash, byte[] expectedHash)
		{
			if (actualHash.Length != expectedHash.Length)
			{
				throw new Exception("actualHash.Length (" + actualHash.Length + ")" +
				                    " != expectedHash.Length (" + expectedHash.Length + ")");
			}
			for (int i = 0; i < expectedHash.Length; i++)
			{
				if (actualHash[i] != expectedHash[i])
				{
					throw new Exception("actualHash[" + i + "] (" + (int)actualHash[i] + ")" +
					                    " != expectedHash[" + i + "] (" + (int)expectedHash[i] + ")");
				}
			}
		}
	}

	class SelfSerializingObject
	{
		public void Deserialize(Stream s)
		{
			System.Web.UI.LosFormatter scFormatter = new System.Web.UI.LosFormatter();
			Obj = scFormatter.Deserialize(s);
		}

		public void Serialize(Stream s)
		{
			LosFormatter scFormatter = new LosFormatter();
			scFormatter.Serialize(s, Obj);
		}

		public object Obj;
	}

	
}
