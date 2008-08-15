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
using System.Runtime.Serialization;

namespace Brettle.Web.NeatUpload
{
    [Serializable]
    public class UploadedFileCollection : NameObjectCollectionBase, ICollection
	{
		internal UploadedFileCollection() {}

        protected UploadedFileCollection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

		public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

		private object _SyncRoot = new object();
		
		object ICollection.SyncRoot { get { return _SyncRoot; } }

		bool ICollection.IsSynchronized { get { return true; } }
		
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
				lock (_SyncRoot)	{ return this.BaseGetAllKeys(); }
			}
		}

		public UploadedFile Get(string key)
		{
			lock (_SyncRoot)
			{
				return (UploadedFile)this.BaseGet(key);
			}
		}

		public UploadedFile Get(int index)
		{
			lock (_SyncRoot)
			{
				return (UploadedFile)this.BaseGet(index);
			}
		}
		
		public virtual IEnumerator GetEnumerator()
		{
			return BaseGetAllValues().GetEnumerator();
		}
		
		public string GetKey(int index)
		{
			lock (_SyncRoot)	{ return this.BaseGetKey(index); }
		}
		
		internal void Add(string key, UploadedFile file)
		{
			lock (_SyncRoot) { this.BaseAdd(key, file); }
		}

		internal UploadedFileCollection GetReadOnlyCopy()
		{
			UploadedFileCollection readOnlyCollection = new UploadedFileCollection();
			lock (_SyncRoot)
			{
				for (int i = 0; i < Count; i++)
					readOnlyCollection.Add(this.GetKey(i), this.Get(i));
				readOnlyCollection.IsReadOnly = true;
			}
			return readOnlyCollection;
		}
	}
}
