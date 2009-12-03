/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005-2006  Dean Brettle

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
using System.Configuration;
using System.Security.Cryptography;

namespace Brettle.Web.NeatUpload
{
    [Serializable]
	public class HashingFilesystemUploadedFile : FilesystemUploadedFile
	{

		public HashingFilesystemUploadedFile(HashingFilesystemUploadStorageProvider provider, 
									  string controlUniqueID, string fileName, string contentType) 
			: base(provider, controlUniqueID, fileName, contentType)
		{
			AlgorithmName = provider.AlgorithmName;
		}
		
		public override Stream CreateStream()
		{
            HashAlgorithm algorithm = HashAlgorithm.Create(AlgorithmName);
            return new MyCryptoStream(this, base.CreateStream(), algorithm);
		}

        byte[] _Hash;

        /// <summary>
		/// The cryptographic hash of the uploaded file.</summary>
		/// <remarks>
		/// <see cref="HashSize" /> provides the length of the
		/// of the hash in bits.</remarks>
		public byte[] Hash
		{
			get { return _Hash; }
		}
		
        int _HashSize;
		/// <summary>
		/// The length of the of the cryptographic hash in bits.</summary>
		/// <remarks>
		/// <see cref="Hash" /> provides the hash itself.</remarks>
		public int HashSize
		{
			get { return _HashSize; }
		}

        string AlgorithmName = null;

        class MyCryptoStream : CryptoStream
        {
            public MyCryptoStream(HashingFilesystemUploadedFile hfuf, Stream destStream, HashAlgorithm algorithm)
                : base(destStream, algorithm, CryptoStreamMode.Write)
            {
                Algorithm = algorithm;
                Hfuf = hfuf;
            }

            bool IsAlgorithmDisposed = false;
            public override void Close()
            {
                base.Close();
                if (!IsAlgorithmDisposed)
                {
                    Hfuf._Hash = Algorithm.Hash;
                    Hfuf._HashSize = Algorithm.HashSize;
                    ((IDisposable)Algorithm).Dispose();
                    IsAlgorithmDisposed = true;
                }
            }

            HashAlgorithm Algorithm;
            HashingFilesystemUploadedFile Hfuf;
        }
	}
}
