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
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Web.UI.Design;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Security.Permissions;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// <see cref="InputFile"/> that provides access to the cryptographic hash of the file contents when used with
	/// the HashingFilesystemUploadStorageProvider 
	/// </summary>
	/// <remarks>
	/// You can use the <see cref="Hash"/> property to access the hash of the uploaded file.</remarks>
	public class HashedInputFile : InputFile
	{
		/// <summary>
		/// The cryptographic hash of the uploaded file.</summary>
		/// <remarks>
		/// <see cref="HashSize" /> provides the length of the
		/// of the hash in bits.</remarks>
		public byte[] Hash
		{
			get { return HashedFile.Hash; }
		}
		
		/// <summary>
		/// The length of the of the cryptographic hash in bits.</summary>
		/// <remarks>
		/// <see cref="Hash" /> provides the hash itself.</remarks>
		public int HashSize
		{
			get { return HashedFile.HashSize; }
		}
		
		private HashingFilesystemUploadedFile HashedFile
		{
			get { return (HashingFilesystemUploadedFile)File; }
		}		
	}
}
