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
using System.Configuration;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// The base class representing a file received by an <see cref="IUploadModule"/>.
	/// The file might be stored on a local disk, in a database, on a remote
	/// filesystem, or somewhere else.
	/// </summary>
	[Serializable]
	public abstract class UploadedFile : IDisposable	{
		private UploadedFile() {}

		/// <summary>
		/// Constructs an <see cref="UploadedFile"/> associated with a particular
		/// control UniqueID, having a given filename and content type.
		/// </summary>
		/// <param name="controlUniqueID">
		/// The UniqueID of the control used to upload the file.
		/// </param>
		/// <param name="fileName">
		/// The filename sent by the browser.
		/// </param>
		/// <param name="contentType">
		/// The MIME content type sent by the browser.
		/// </param>
		protected UploadedFile(string controlUniqueID, string fileName, string contentType)
		{
			// IE sends a full path for the fileName.  We only want the actual filename.
			FileName = StripPath(fileName);
			ContentType = contentType;
			ControlUniqueID = controlUniqueID;
		}

		/// <summary>
		/// Removes the Windows-style path from the given filename.
		/// </summary>
		/// <param name="fileName">
		/// A filename, possibly including a Windows-style path.
		/// </param>
		/// <returns>
		/// Just the filename.
		/// </returns>
		/// <remarks>Some browsers (notably IE) send the full-path when uploading a file.
		/// Most do not.  We strip off the path for consistency.</remarks>
		protected static string StripPath(string fileName)
		{
			if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"^(\\\\[^\\]|([a-zA-Z]:)?\\).*"))
			{
				fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
			}
			return fileName;
		}

		/// <summary>
		/// Disposes of any temporary resources (e.g. temp files) used by this object.
		/// </summary>
		/// <remarks>If <see cref="MoveTo"/> was not called, then when this method
		/// returns, no remnants of the file should remain.</remarks>
		public abstract void Dispose();

		/// <summary>
		/// Whether this object corresponds to an actual file with either a non-empty
		/// filename or a non-zero length.
		/// </summary>
		/// <value>
		/// Whether this object corresponds to an actual file with either a non-empty
		/// filename or a non-zero length.
		/// </value>
		public abstract bool IsUploaded	{ get; }

		/// <summary>
		/// Creates a <see cref="Stream"/> to which the <see cref="IUploadModule"/> can 
		/// write the file as it is received.
		/// </summary>
		/// <returns>
		/// A <see cref="Stream"/>.
		/// </returns>
		public abstract Stream CreateStream();

		/// <summary>
		/// Moves the file to a permanent location.
		/// </summary>
		/// <param name="path">
		/// The location to which the file should be moved.
		/// </param>
		/// <param name="opts">
		/// A <see cref="MoveToOptions"/> object controlling details of the move,
		/// </param>
		/// <remarks>The <paramref name="path"/> could be a filesystem path or some
		/// other identifier, depending on the storage medium.  The 
		/// <paramref name="opts"/> could just be <see cref="MoveToOptions.Overwrite"/>
		/// or <see cref="MoveToOptions.None"/> to control whether any existing file
		/// at the same location should be replaced.  Or, it could be a module-specific
		/// subclass of <see cref="MoveToOptions"/> which provides additional 
		/// information.</remarks>
		public abstract void MoveTo(string path, MoveToOptions opts);

		/// <summary>
		/// The length in bytes of the specified file.
		/// </summary>
		/// <value>
		/// The length in bytes of the specified file.
		/// </value>
		public abstract long ContentLength { get; }

		/// <summary>
		/// Gets a <see cref="Stream"/> which can be used to access the file.
		/// </summary>
		/// <returns>
		/// A <see cref="Stream"/>
		/// </returns>
		public abstract Stream OpenRead();

		/// <summary>
		/// If the file is stored on disk, the corresponding
		/// <see cref="FileInfo"/> object.
		/// </summary>
		/// <value>
		/// If the file is stored on disk, the corresponding
		/// <see cref="FileInfo"/> object.
		/// </value>
		public FileInfo TmpFile;

		/// <summary>
		/// The filename sent by the browser, without any path.
		/// </summary>
		/// <value>
		/// The filename sent by the browser, without any path.
		/// </value>
		public string FileName;

		/// <summary>
		/// The MIME content type sent by the browser.
		/// </summary>
		/// <value>
		/// The MIME content type sent by the browser.
		/// </value>
		public string ContentType;

		/// <summary>
		/// The UniqueID of the control used to upload the file.
		/// </summary>
		/// <value>
		/// The UniqueID of the control used to upload the file.
		/// </value>
		public string ControlUniqueID;

		// InputStream and SaveAs() are provided to simplify switching from System.Web.HttpPostedFile.
		
		/// <summary>
		/// A readable <see cref="Stream"/> on the uploaded file. </summary>
		/// <remarks>
		/// A readable <see cref="Stream"/> on the uploaded file.  Note that the <see cref="Stream"/> is opened 
		/// when this property is first accessed and that stream becomes the permanent value of this property.  
		/// If you use this property and don't either Close() the stream or call <see cref="MoveTo"/> or
		/// <see cref="SaveAs"/> before the request ends you may get an exception when NeatUpload tries to delete 
		/// the underlying temporary storage at the end of the request.
		/// </remarks>
		public Stream InputStream 
		{
			get 
			{
				if (_InputStream == null) 
					_InputStream = OpenRead(); 
				return _InputStream; 
			} 
		}

		/// <summary>
		/// Equivalent to <code>MoveTo(path, MoveToOptions.Overwrite)</code>
		/// </summary>
		/// <param name="path">
		/// The location to which the file should be saved.
		/// </param>
		public void SaveAs(string path) { MoveTo(path, MoveToOptions.Overwrite); }

		[NonSerialized]
		private Stream _InputStream;
	}
}
