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
    /// <summary>
    /// Converts serializable objects to and from secure strings so they
    /// can be passed through untrusted code.
    /// </summary>
    /// <remarks>
    /// <see cref="Protect(object)"/> serializes, encrypts, signs, and base64-encodes 
    /// an object tree to produce a secure string.  
    /// <see cref="Unprotect(string)"/> base64-decodes 
    /// the string, verifies the signature, decrypts the ciphertext and deserializes
    /// the object tree.  All objects in the object tree must have the
    /// <see cref="SerializableAttribute"/>.
    /// </remarks>
	public class ObjectProtector
	{
		// Static methods only
		private ObjectProtector() { }
		
		// Create a logger for use in this class
		/*
		private static readonly log4net.ILog log
		= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		*/

        /// <summary>
        /// A method that verifies that two signatures are the same and throws an
        /// exception if they aren't.
        /// </summary>
        /// <param name="actualHash">the actual signature</param>
        /// <param name="expectedHash">the expected signature</param>
		public delegate void SignatureChecker(byte[] actualHash, byte[] expectedHash);

        /// <summary>
        /// A method of an object that reconstructs the object from a <see cref="Stream"/>
        /// of serialized bytes.
        /// </summary>
        /// <param name="s">the <see cref="Stream"/> of serialized bytes.</param>
		public delegate void Deserializer(Stream s);

        /// <summary>
        /// A method of an object that serializes the object into a <see cref="Stream"/>
        /// of serialized bytes.
        /// </summary>
        /// <param name="s">the <see cref="Stream"/> to which the serialized bytes
        /// will be written.</param>
        public delegate void Serializer(Stream s);

        /// <summary>
        /// Converts a secure string back to the object tree it represents.
        /// </summary>
        /// <param name="secureString">a <see cref="string"/> returned by an earlier
        /// call to <see cref="Protect(object)"/></param>
        /// <returns>the object that was passed to <see cref="Protect(object)"/></returns>
        /// <exception>throws an <see cref="Exception"/> if the signature is
        /// not valid.</exception>
        /// <remarks>The encryption key and algorithms specified in the encryptionKey,
        /// encryption, and validation attributes of the &lt;neatUpload&gt; element are used
        /// to decrypt the ciphertext.  They must have
        /// the same values as they did when <see cref="Protect(object)"/> was called or
        /// an exception will occur.</remarks>
        public static object Unprotect(string secureString)
		{
            return Unprotect(secureString, 
                Config.Current.EncryptionKey, Config.Current.ValidationKey, 
                Config.Current.EncryptionAlgorithm, Config.Current.ValidationAlgorithm);
		}

        /// <summary>
        /// Converts a secure string back to the object tree it represents.
        /// </summary>
        /// <param name="secureString">a <see cref="string"/> returned by an earlier
        /// call to <see cref="Protect(object)"/></param>
        /// <param name="encryptionKey">the key to use to decrypt the ciphertext</param>
        /// <param name="validationKey">ignored</param>
        /// <returns>the object that was passed to <see cref="Protect(object)"/></returns>
        /// <exception>throws an <see cref="Exception"/> if the signature is
        /// not valid.</exception>
        /// <remarks>The encryption key must have
        /// the same value as it did when <see cref="Protect(object, byte[], byte[])"/> was called or
        /// an exception will occur.</remarks>
        public static object Unprotect(string secureString, byte[] encryptionKey, byte[] validationKey)
        {
            return Unprotect(secureString, encryptionKey, validationKey, (string)null, (string)null);
        }

        /// <summary>
        /// Converts a secure string back to the object tree it represents.
        /// </summary>
        /// <param name="secureString">a <see cref="string"/> returned by an earlier
        /// call to <see cref="Protect(object)"/></param>
        /// <param name="encryptionKey">the key to use to decrypt the ciphertext</param>
        /// <param name="validationKey">ignored</param>
        /// <param name="encryptionAlgorithm">the algorithm to use to decrypt the ciphertext, null means use the default</param>
        /// <param name="validationAlgorithm">the algorithm to use to verify the signature, null means use the default</param>
        /// <returns>the object that was passed to <see cref="Protect(object)"/></returns>
        /// <exception>throws an <see cref="Exception"/> if the signature is
        /// not valid.</exception>
        /// <remarks>The encryption key and algorithms must have
        /// the same values as they did when <see cref="Protect(object, byte[], byte[])"/> was called or
        /// an exception will occur.</remarks>
        public static object Unprotect(string secureString, byte[] encryptionKey, byte[] validationKey, string encryptionAlgorithm, string validationAlgorithm)
        {
            SelfSerializingObject obj = new SelfSerializingObject();
            Unprotect(secureString, encryptionKey, validationKey, encryptionAlgorithm, validationAlgorithm, obj.Deserialize, AssertSignaturesAreEqual);
            return obj.Obj;
        }

        /// <summary>
        /// Converts an object tree to a secure string.
        /// </summary>
        /// <param name="objectToSerialize">the object at the root of the object tree
        /// to protect.</param>
        /// <returns>a secure string that can be passed to <see cref="Unprotect(string)"/> to
        /// retrieve the original object.</returns>
        /// <remarks>The encryption key and algorithms specified in the encryptionKey,
        /// encryption, and validation attributes of the &lt;neatUpload&gt; element are used
        /// to encrypt the serialized object and sign it, respectively.  They must have
        /// the same values when <see cref="Unprotect(string)"/> is called or
        /// an exception will occur.</remarks>
        public static string Protect(object objectToSerialize)
		{
            return Protect(objectToSerialize, 
                Config.Current.EncryptionKey, Config.Current.ValidationKey,
                Config.Current.EncryptionAlgorithm, Config.Current.ValidationAlgorithm);
		}

        /// <summary>
        /// Converts an object tree to a secure string.
        /// </summary>
        /// <param name="objectToSerialize">the object at the root of the object tree
        /// to protect.</param>
        /// <param name="encryptionKey">the key to use to encrypt the object</param>
        /// <param name="validationKey">ignored</param>
        /// <returns>a secure string that can be passed to <see cref="Unprotect(string)"/> to
        /// retrieve the original object.</returns>
        /// <remarks>The encryption key is used
        /// to encrypt the serialized object and sign it, respectively.  It must have
        /// the same values when <see cref="Unprotect(string, byte[], byte[])"/> is called or
        /// an exception will occur.</remarks>
        public static string Protect(object objectToSerialize, byte[] encryptionKey, byte[] validationKey)
        {
            return Protect(objectToSerialize, encryptionKey, validationKey, null, null);
        }

        /// <summary>
        /// Converts an object tree to a secure string.
        /// </summary>
        /// <param name="objectToSerialize">the object at the root of the object tree
        /// to protect.</param>
        /// <param name="encryptionKey">the key to use to encrypt the object</param>
        /// <param name="validationKey">ignored</param>
        /// <param name="encryptionAlgorithm">the name of the encryption algorithm to use, null means use default</param>
        /// <param name="validationAlgorithm">the name of the signing algorithm to use, null means use default</param>
        /// <returns>a secure string that can be passed to <see cref="Unprotect(string)"/> to
        /// retrieve the original object.</returns>
        /// <remarks>The encryption key and algorithms are used
        /// to encrypt the serialized object.  They must have
        /// the same values when <see cref="Unprotect(string, byte[], byte[])"/> is called or
        /// an exception will occur.</remarks>
        public static string Protect(object objectToSerialize, byte[] encryptionKey, byte[] validationKey, string encryptionAlgorithm, string validationAlgorithm)
        {
            SelfSerializingObject obj = new SelfSerializingObject();
            obj.Obj = objectToSerialize;
            return Protect(obj.Serialize, encryptionKey, validationKey, encryptionAlgorithm, validationAlgorithm);
        }

        /// <summary>
        /// Converts a secure string back to the object tree it represents, using
        /// a custom <see cref="Deserializer"/> and <see cref="SignatureChecker"/>.
        /// </summary>
        /// <param name="secureString">the secure string to be converted back to an
        /// object tree.</param>
        /// <param name="encryptionKey">the key to use to decrypt the ciphertext</param>
        /// <param name="validationKey">the key to use to verify the signature</param>
        /// <param name="deserializer">a <see cref="Deserializer"/> delegate from the
        /// root object of the object tree that can recreate the object tree from a
        /// <see cref="Stream"/> of serialized bytes.</param>
        /// <param name="sigChecker">a <see cref="SignatureChecker"/> delegate that
        /// compares an actual signature to the expected signature, throwin an exception
        /// if they don't match.</param>
        /// <remarks>The encryption and validation keys must have
        /// the same values as they did when <see cref="Protect(Serializer, byte[], byte[])"/> was called or
        /// an exception will occur.</remarks>
        public static void Unprotect(string secureString, byte[] encryptionKey, byte[] validationKey, Deserializer deserializer, SignatureChecker sigChecker)
        {
            Unprotect(secureString, encryptionKey, validationKey, null, null, deserializer, sigChecker);
        }
        
        /// <summary>
        /// Converts a secure string back to the object tree it represents, using
        /// a custom <see cref="Deserializer"/> and <see cref="SignatureChecker"/>.
        /// </summary>
        /// <param name="secureString">the secure string to be converted back to an
        /// object tree.</param>
        /// <param name="encryptionKey">the key to use to decrypt the ciphertext</param>
        /// <param name="validationKey">ignored</param>
        /// <param name="encryptionAlgorithm">the name of the encryption algorithm to use, null means use default</param>
        /// <param name="validationAlgorithm">the name of the signing algorithm to use, null means use default</param>
        /// <param name="deserializer">a <see cref="Deserializer"/> delegate from the
        /// root object of the object tree that can recreate the object tree from a
        /// <see cref="Stream"/> of serialized bytes.</param>
        /// <param name="sigChecker">a <see cref="SignatureChecker"/> delegate that
        /// compares an actual signature to the expected signature, throwin an exception
        /// if they don't match.</param>
        /// <remarks>The encryption key and algorithms must have
        /// the same values as they did when <see cref="Protect(Serializer, byte[], byte[])"/> was called or
        /// an exception will occur.</remarks>
        public static void Unprotect(string secureString, byte[] encryptionKey, byte[] unused, string encryptionAlgorithm, string validationAlgorithm, Deserializer deserializer, SignatureChecker sigChecker)
		{			
			byte[] secureBytes = Convert.FromBase64String(secureString);
			MemoryStream secureStream = new MemoryStream(secureBytes);
			BinaryReader binaryReader = new BinaryReader(secureStream);
			byte[] actualHash = binaryReader.ReadBytes(binaryReader.ReadByte());
			byte[] iv = binaryReader.ReadBytes(binaryReader.ReadByte());
			byte[] cipherText = binaryReader.ReadBytes((int)(secureStream.Length - secureStream.Position));
			
			// Verify the hash
			HashAlgorithm hashAlgorithm 
                = validationAlgorithm != null
                ? HashAlgorithm.Create(validationAlgorithm)
                : HashAlgorithm.Create();
            byte[] expectedHash = hashAlgorithm.ComputeHash(cipherText);
			sigChecker(actualHash, expectedHash);
			
			// Decrypt the ciphertext
			MemoryStream cipherTextStream = new MemoryStream(cipherText);
			SymmetricAlgorithm cipher 
                = encryptionAlgorithm != null
                ? SymmetricAlgorithm.Create(encryptionAlgorithm)
                : SymmetricAlgorithm.Create();
			cipher.Mode = CipherMode.CBC;
			cipher.Padding = PaddingMode.PKCS7;
			cipher.Key = encryptionKey;
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

        /// <summary>
        /// Converts am object tree to a secure string, using
        /// a custom <see cref="Serializer"/>.
        /// </summary>
        /// <param name="serializer">a <see cref="Serializer"/> delegate from the
        /// root object of the object tree that can serialize the object tree into a
        /// <see cref="Stream"/> of serialized bytes.</param>
        /// <param name="encryptionKey">the key to use to encrypt the object</param>
        /// <param name="validationKey">ignored</param>
        /// <remarks>The encryption key  must have
        /// the same value when <see cref="Unprotect(string, byte[], byte[], Deserializer, SignatureChecker)"/> is called or
        /// an exception will occur.</remarks>
        public static string Protect(Serializer serializer, byte[] encryptionKey, byte[] validationKey)
        {
            return Protect(serializer, encryptionKey, validationKey, null, null);
        }

        /// <summary>
        /// Converts am object tree to a secure string, using
        /// a custom <see cref="Serializer"/>.
        /// </summary>
        /// <param name="serializer">a <see cref="Serializer"/> delegate from the
        /// root object of the object tree that can serialize the object tree into a
        /// <see cref="Stream"/> of serialized bytes.</param>
        /// <param name="encryptionKey">the key to use to encrypt the object</param>
        /// <param name="validationKey">ignored</param>
        /// <param name="encryptionAlgorithm">the name of the encryption algorithm to use, null means use default</param>
        /// <param name="validationAlgorithm">the name of the hash algorithm to use, null means use default</param>
        /// <remarks>The encryption key and algorithms must have
        /// the same values when <see cref="Unprotect(string, byte[], byte[], Deserializer, SignatureChecker)"/> is called or
        /// an exception will occur.</remarks>
        public static string Protect(Serializer serializer, byte[] encryptionKey, byte[] unused, string encryptionAlgorithm, string validationAlgorithm)
		{
			// Encrypt it
			MemoryStream cipherTextStream = new MemoryStream();
            SymmetricAlgorithm cipher 
                = encryptionAlgorithm != null 
                ? SymmetricAlgorithm.Create(encryptionAlgorithm) 
                : SymmetricAlgorithm.Create();
			cipher.Mode = CipherMode.CBC;
			cipher.Padding = PaddingMode.PKCS7;
			cipher.Key = encryptionKey;
			CryptoStream cryptoStream = new CryptoStream(cipherTextStream, cipher.CreateEncryptor(), CryptoStreamMode.Write);
			serializer(cryptoStream);
			cryptoStream.Close();
			byte[] cipherText = cipherTextStream.ToArray();
			
			// hash the ciphertext
			HashAlgorithm hashAlgorithm
                = validationAlgorithm != null
                ? HashAlgorithm.Create(validationAlgorithm)
                : HashAlgorithm.Create();
            byte[] hash = hashAlgorithm.ComputeHash(cipherText);
			
			// Concatenate hash length, hash, IV length, IV, and ciphertext into an array.
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
		
        /// <summary>
        /// Compares an actual signature to an expected signature, throwing an exception if they 
        /// don't match.
        /// </summary>
        /// <param name="actualHash">the actual signature</param>
        /// <param name="expectedHash">the expected signature</param>
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
