/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005, 2006  Dean Brettle

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
using System.IO;
using System.Web;

namespace Brettle.Web.NeatUpload
{
	public class UploadedFileCollection : NameObjectCollectionBase
	{
		internal UploadedFileCollection() {}
		
		public new object SyncRoot = new object();
		
		public new bool IsSynchronized { get { return true; } }
		
		public UploadedFile this[string key]
		{
			get 
			{ 
				return Get(key);
			}
		}
				
		public UploadedFile this[int index]
		{
			get 
			{ 
				return Get(index);
			}
		}
		
		public string[] AllKeys
		{
			get
			{
				lock (SyncRoot)	{ return this.BaseGetAllKeys(); }
			}
		}

		public UploadedFile Get(string key)
		{
			lock (SyncRoot)
			{
				UploadedFile file = (UploadedFile)this.BaseGet(key);
				if (! (file is AspNetUploadedFile))
				{
					return file;
				}
				// Its an AspNetUploadedFile which means it hasn't yet been sent to the UploadStorageProvider.
				// We send it to the UploadStorageProvider now, and replace it with the new UploadedFile returned
				// by the UploadStorageProvider.

				HttpPostedFile postedFile = HttpContext.Current.Request.Files[key];
				UploadContext ctx = UploadContext.Current;
				if (ctx == null)
				{
					// We use a temporary UploadContext so that we have something we can pass to the
					// UploadStorageProvider.  Note that unlike when the UploadHttpModule is used,
					// this temporary context is not shared between uploaded files.
					ctx = new UploadContext();
					ctx.SetContentLength(HttpContext.Current.Request.ContentLength);
					ctx.Status = UploadStatus.NormalInProgress;
				}
				UploadStorageConfig storageConfig = UploadStorage.CreateUploadStorageConfig();
				string storageConfigString = HttpContext.Current.Request.Form[UploadContext.ConfigNamePrefix + "-" + key];
				if (storageConfigString != null && storageConfigString != string.Empty)
				{
					storageConfig.Unprotect(storageConfigString);
				}
				file = UploadStorage.CreateUploadedFile(ctx, key, postedFile.FileName, postedFile.ContentType, storageConfig);
				Stream outStream = null, inStream = null;
				try
				{
					outStream = file.CreateStream();
					inStream = postedFile.InputStream;
					byte[] buf = new byte[4096];
					int bytesRead = -1;
					while (outStream.CanWrite && inStream.CanRead 
						   && (bytesRead = inStream.Read(buf, 0, buf.Length)) > 0)
					{
						outStream.Write(buf, 0, bytesRead);
						ctx.BytesRead += bytesRead;
					}
					ctx.BytesRead = ctx.ContentLength;
					ctx.Status = UploadStatus.Completed;
				}
				finally
				{
					if (inStream != null) inStream.Close();
					if (outStream != null) outStream.Close();
				}
				this.BaseSet(key, file);
				return file;
			}
		}

		public UploadedFile Get(int index)
		{
			return Get(GetKey(index));
		}
		
		public string GetKey(int index)
		{
			lock (SyncRoot)	{ return this.BaseGetKey(index); }
		}
		
		internal void Add(string key, UploadedFile file)
		{
			lock (SyncRoot) { this.BaseAdd(key, file); }
		}
	}
}
