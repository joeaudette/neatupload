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
using System.Collections.Specialized;

namespace Brettle.Web.NeatUpload
{
	public class FilesystemUploadedFile : UploadedFile
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public FilesystemUploadedFile(FilesystemUploadStorageProvider provider, 
		                                string controlUniqueID, string fileName, string contentType)
		                                : base(controlUniqueID, fileName, contentType)
		{
			Initialize(provider, controlUniqueID, fileName, contentType, null);
		}
		                                
		public FilesystemUploadedFile(FilesystemUploadStorageProvider provider, 
		                                string controlUniqueID, string fileName, string contentType,
		                                NameValueCollection storageConfig)
			: base(controlUniqueID, fileName, contentType)
		{
			Initialize(provider, controlUniqueID, fileName, contentType, storageConfig);
		}
		
		private void Initialize(FilesystemUploadStorageProvider provider, 
		                                string controlUniqueID, string fileName, string contentType,
		                                NameValueCollection storageConfig)
		{
			DirectoryInfo tmpDirInfo = provider.GetTempDirectory(storageConfig);
			if (!tmpDirInfo.Exists)
			{
				tmpDirInfo.Create();
			}
			string name = Guid.NewGuid().ToString("N"); // 32 hex digits
			tmpFileName = Path.Combine(tmpDirInfo.FullName, name);
			TmpFile = new FileInfo(tmpFileName);
		}
		
		public override void Dispose()
		{
			if (log.IsDebugEnabled) 
				log.DebugFormat("In Dispose(): TmpFile.FullName = {0}", TmpFile.FullName);
			if (TmpFile.Exists && TmpFile.FullName == (new FileInfo(tmpFileName)).FullName)
			{
				log.DebugFormat("Calling TmpFile.Delete()");
				TmpFile.Delete();
			}
		}

		private string tmpFileName;
		
		public override bool IsUploaded
		{
			get { return (TmpFile != null && (TmpFile.Length > 0 || FileName.Length > 0)); }
		}

		public override Stream CreateStream()
		{
			return TmpFile.Create();
		}

		public override void MoveTo(string path, MoveToOptions opts)
		{
			if (opts.CanOverwrite && File.Exists(path))
			{
				File.Delete(path);
			}
			TmpFile.MoveTo(path);
		}

		public override long ContentLength 
		{
			get { return (TmpFile != null ? TmpFile.Length : 0); }
		}

		public override Stream OpenRead()
		{
			if (TmpFile == null)
			{
				return null;
			}
			return TmpFile.OpenRead();
		} 
	}
}
