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
using System.Text;

namespace Hitone.Web.SqlServerUploader
{
    /// <summary>
    /// <see cref="InputFile"/> that provides access to the identity of the newly created row when used with
    /// the SqlServerUploadStorageProvider 
    /// </summary>
    /// <remarks>
    /// You can use the <see cref="Identity"/> property to access the identity of the sql table row</remarks>
    public class SqlServerInputFile : Brettle.Web.NeatUpload.InputFile
    {
        /// <summary>
        /// The identity of the newly created row in the database, if the table has an identity column
        /// </summary>
        public int Identity { get { return SqlFile.Identity; } }

        private SqlServerUploadedFile SqlFile
        {
            get { return (SqlServerUploadedFile)File; }
        }

        /// <summary>
        /// Verfies the file as a valid file that should be kept. If neither this nor MoveTo are called, the uploaded data is deleted from the database
        /// </summary>
        public void Verify() { SqlFile.Verify(); }

        /// <summary>
        /// The cryptographic hash of the uploaded file.</summary>
        /// <remarks>
        /// <see cref="HashSize" /> provides the length of the
        /// of the hash in bits.</remarks>
        public byte[] Hash
        {
            get { return SqlFile.Hash; }
        }

        /// <summary>
        /// The length of the of the cryptographic hash in bits.</summary>
        /// <remarks>
        /// <see cref="Hash" /> provides the hash itself.</remarks>
        public int HashSize
        {
            get { return SqlFile.HashSize; }
        }

        /// <summary>
        /// Name of hash algorithm used
        /// </summary>
        public string HashName { get { return SqlFile.HashName; } }
    }
}
