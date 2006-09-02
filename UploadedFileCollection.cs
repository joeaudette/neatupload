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
				lock (SyncRoot)	{ return (UploadedFile)this.BaseGet(key); }
			}
		}
				
		public UploadedFile this[int index]
		{
			get 
			{ 
				lock (SyncRoot)	{ return (UploadedFile)this.BaseGet(index); }
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
			return this[key];
		}

		public UploadedFile Get(int index)
		{
			return this[index];
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
