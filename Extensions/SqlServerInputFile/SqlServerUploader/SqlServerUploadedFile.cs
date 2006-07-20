/*
 
SqlServerUploader - an addon to NeatUpload to allow uploading files to stream
directly into a database.
Copyright (C) 2006  Joakim Wennergren (jokedst@gmail.com)

NeatUpload is an HttpModule and User Controls for uploading large files.
NeatUpload is created and maintained by Dean Brettle (www.brettle.com)

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
using Brettle.Web.NeatUpload;
using System.Security.Cryptography;
using System.Data.SqlClient;

namespace Hitone.Web.SqlServerUploader
{
    /// <summary>
    /// Memory structure for one sql streamed file during upload
    /// </summary>
    public class SqlServerUploadedFile : UploadedFile
    {
        /// <summary> Returns generated identity value if data was written to a table with an IDENTITY-column </summary>
        public int Identity { get { return _identity; } }
        private int _identity = -1;

        private bool _verified = false;
        private bool _disposed = false;

        public SqlServerUploadedFile(   SqlServerUploadStorageProvider provider,
                                        string controlUniqueID, 
                                        string fileName, 
                                        string contentType,
                                        UploadStorageConfig storageConfig
                                    ) : base(controlUniqueID, fileName, contentType)
		{
            Initialize(provider, storageConfig);
		}

        internal SqlServerUploadStorageProvider _provider = null;
        private SqlServerBlobStream _blobStream = null;

        private void Initialize(SqlServerUploadStorageProvider provider,
                                        UploadStorageConfig storageConfig)
        {
            //Simply store the provider, the SqlServerBlobStream takes care of everything else (i hope)
            _provider = provider;

            // If hash algorithm is specified, create an object to calculate hash
            if (provider.HashAlgorithm != null && provider.HashAlgorithm.Length > 0){
                _hashName = provider.HashAlgorithm;
                _hashAlgorithm = System.Security.Cryptography.HashAlgorithm.Create(provider.HashAlgorithm);
            }
        }

        /// <summary>
        /// Gets the size of the uploaded file in bytes
        /// </summary>
        public override long ContentLength { get { return (_blobStream != null ? _blobStream.Length : 0); } }

        public override Stream CreateStream()
        {
            // _blobStream = new SqlServerBlobStream(this);
            // Use the BIG constructor that takes in _everything_ and figures out how to use it
            _blobStream = new SqlServerBlobStream(_provider.ConnectionString, _provider.TableName, _provider.DataColumnName, _provider.PartialFlagColumnName, _provider.FileNameColumnName, this.FileName, _provider.MIMETypeColumnName, this.ContentType,
                _provider.CreateProcedure, _provider.OpenProcedure, _provider.WriteProcedure, _provider.ReadProcedure, _provider.CleanupProcedure, _provider.RenameProcedure, _provider.StoreHashProcedure, _provider.DeleteProcedure);

            _identity = _blobStream.Identity;   //Get generated identity (if any) from the stream

            // If hash algorithm is specified, enlcose the blobstream in a hash crypto-transformation
            if (_hashAlgorithm != null)
                return new CryptoStream(_blobStream, _hashAlgorithm, CryptoStreamMode.Write);

            return _blobStream;
        }


        /// <summary>
        /// Converts an integer value into its hexadecimal counterpart
        /// </summary>
        /// <param name="i">Integer value to convert. Is assumed to be less than 16. If i>=16 the behaviour is undefined</param>
        /// <returns>The hex counterpart of the input value</returns>
        private static char GetHexValue(int i)
        {
            return (char)((ushort)(i + (i < 10 ? '0' : ('a' - 10))));
        }

        /// <summary>
        /// Converts a byte array to a hexadecimal string
        /// </summary>
        /// <param name="bytes">The byte array to convert</param>
        /// <returns>A string containing the input arra in hexadecimal format</returns>
        /// <remarks>Mimics the System.BitConverter.ToString behaviour but without the dashes</remarks>
        public static string ToHex(byte[] bytes)
        {
            int bi = 0;
            char[] hex = new char[bytes.Length * 2];

            for (int i = 0; i < hex.Length; i += 2)
            {
                byte b = bytes[bi++];
                hex[i] = GetHexValue(b / 0x10);
                hex[i + 1] = GetHexValue(b % 0x10);
            }
            return new string(hex, 0, hex.Length);
        }

        private void SaveHash()
        {
            SqlConnection connection = new SqlConnection(_provider.ConnectionString);
            SqlCommand command = connection.CreateCommand();
            if (_provider.StoreHashProcedure != null && _provider.StoreHashProcedure.Length > 0) {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = _provider.StoreHashProcedure;
            } else
                command.CommandText = string.Format("UPDATE [{0}] Set [{1}]=@Hash Where $IDENTITY=@Identity", _provider.TableName, _provider.HashColumnName);
            SqlServerBlobStream.AddWithValue(command.Parameters, "@Hash", ToHex(_hashAlgorithm.Hash));
            SqlServerBlobStream.AddWithValue(command.Parameters, "@Identity", _blobStream.Identity);

            connection.Open();
            try { command.ExecuteNonQuery(); }
            finally { connection.Close(); }
        }

        /// <summary>
        /// Renames the current file to the given name. Requires that either renameProcedure or TebleName/FileNameColumnName is specified
        /// </summary>
        /// <param name="newName">Filename to change to</param>
        private void Rename(string newName)
        {
            SqlConnection connection = new SqlConnection(_provider.ConnectionString);
            SqlCommand command = connection.CreateCommand();
            if (_provider.RenameProcedure != null && _provider.RenameProcedure.Length > 0)
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = _provider.RenameProcedure;
            }
            else
                command.CommandText = string.Format("UPDATE [{0}] Set [{1}]=@FileName Where $IDENTITY=@Identity", _provider.TableName, _provider.FileNameColumnName);
            SqlServerBlobStream.AddWithValue(command.Parameters, "@FileName", newName);
            SqlServerBlobStream.AddWithValue(command.Parameters, "@Identity", _blobStream.Identity);

            connection.Open();
            try { command.ExecuteNonQuery(); }
            finally { connection.Close(); }

            FileName = newName;
        }

        public override void Dispose()
        {
            if (_disposed) return;
            if (!_verified)
                _blobStream.Delete();
            else
            {
                //Check if we should store the hash in the database
                if (_hashAlgorithm != null && (_provider.HashColumnName != null || _provider.StoreHashProcedure != null))
                    SaveHash();
            }
            if (_hashAlgorithm != null)
                ((IDisposable)_hashAlgorithm).Dispose();
            _blobStream.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Returns wether a file was uploaded into this object
        /// </summary>
        public override bool IsUploaded
        {
            get {
                //I certainly would like a better way to check if a file was uploaded, but this seems to be it
                return (_blobStream.Length > 0 && FileName.Length > 0);
            }
        }

        /// <summary>
        /// If called, this stream is considered "verified" and is allowed to stay in the datadase. If this is never called, the row is deleted
        /// </summary>
        /// <param name="path"><c>Optional</c> If specified, and the target data table has a FileNameField specified, the filename is changed. If <c>null</c> the filename remains the same</param>
        /// <param name="opts">Ignored</param>
        /// <remarks>This slightly aquard behaviour is due to the NeatUpload framework, where "MoveTo" is the only suitable method exposed by InputFile</remarks>
        public override void MoveTo(string path, MoveToOptions opts)
        {
            _verified = true;
            //Change filename in the database
            if (path != null && (
                (_provider.FileNameColumnName != null && _provider.FileNameColumnName.Length > 0) || 
                (_provider.RenameProcedure != null && _provider.RenameProcedure.Length > 0)))
                Rename(path);
        }

        /// <summary>
        /// If called this file is considered verified and is allowed to stay in the database.
        /// </summary>
        public void Verify()
        {
            _verified = true;
        }

        //TODO: Test function below
        /// <summary>
        /// Opens the newly created file for reading
        /// </summary>
        /// <returns>Stream with file data</returns>
        public override Stream OpenRead()
        {
            if (_disposed) throw new ObjectDisposedException("SqlServerBlobStream");
            if (!_blobStream.CanRead) throw new NotSupportedException();
            _blobStream.Seek(0, SeekOrigin.Begin);
            return _blobStream;
        }



        // Hash-Speicific code is below
        private HashAlgorithm _hashAlgorithm = null;
        private string _hashName = string.Empty;

        /// <summary>
        /// The cryptographic hash of the uploaded file.</summary>
        /// <remarks><see cref="HashSize" /> provides the length of the hash in bits.</remarks>
        public byte[] Hash
        {
            get { return _hashAlgorithm != null ? _hashAlgorithm.Hash : null; }
        }

        /// <summary>
        /// The length of the of the cryptographic hash in bits.</summary>
        /// <remarks><see cref="Hash" /> provides the hash itself.</remarks>
        public int HashSize
        {
            get { return _hashAlgorithm != null ? _hashAlgorithm.HashSize : -1; }
        }

        /// <summary>
        /// Name of hash algorithm used
        /// </summary>
        public string HashName { get { return _hashName; } }
    }
}
