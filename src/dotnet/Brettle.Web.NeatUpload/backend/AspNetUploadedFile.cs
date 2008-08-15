/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005,2006  Dean Brettle

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
using System.Web;
using System.Configuration;

namespace Brettle.Web.NeatUpload.Internal.Module
{
	// Instances of this class are just placeholders in an UploadedFileCollection for files that the UploadHttpModule
	// doesn't stream to the UploadStorageProvider while the request is being received (e.g. because it is
	// disabled/uninstalled or NeatUpload wasn't told to stream the files).  When they are
	// first retrieved from the collection after the request has been received, UploadedFileCollection 
	// replaces them with UploadedFiles created by the UploadStorageProvider.
	internal class AspNetUploadedFile : UploadedFile
	{
		internal AspNetUploadedFile(string controlUniqueID, HttpPostedFile httpPostedFile) 
			: base(controlUniqueID, httpPostedFile.FileName, httpPostedFile.ContentType)
		{
			File = httpPostedFile;
		}

		private HttpPostedFile File;
						
		public override void Dispose() { }

		public override bool IsUploaded	
		{
			get { return ((FileName != null && FileName.Length > 0) || File.ContentLength > 0); }
		}

		public override System.IO.Stream CreateStream() 
		{
			return File.InputStream;
		}
			

		public override void MoveTo(string path, MoveToOptions opts) 
		{
			if (opts.CanOverwrite && System.IO.File.Exists(path))
			{
				System.IO.File.Delete(path);
			}
			File.SaveAs(path);
		}

		public override long ContentLength 
		{
			get { return File.ContentLength; }
		}

		public override System.IO.Stream OpenRead()
		{
			return File.InputStream;
		}
	}
}
