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
	/// <summary>
	/// A collection of <see cref="UploadedFile"/> objects, indexed by the UniqueID of
	/// the control that uploaded them.
	/// </summary>
    [Serializable]
    public class UploadedFileCollection : NameObjectCollectionBase, ICollection
	{
		internal UploadedFileCollection() {}

        protected UploadedFileCollection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

		private object _SyncRoot = new object();

		/// <summary>
		/// An object that can be used to synchronize access to this collection.
		/// </summary>
		/// <value>
		/// An object that can be used to synchronize access to this collection.
		/// </value>
		object ICollection.SyncRoot { get { return _SyncRoot; } }

		/// <summary>
		/// Returns true.
		/// </summary>
		/// <value>
		/// Returns true.
		/// </value>
		bool ICollection.IsSynchronized { get { return true; } }

		/// <summary>
		/// The <see cref="UploadedFile"/> in the collection that was uploaded from
		/// the control with the specified UniqueID, or null if there is no such file.
		/// </summary>
		/// <value>
		/// The <see cref="UploadedFile"/> in the collection that was uploaded from
		/// the control with the specified UniqueID, or null if there is no such file.
		/// </value>
		public UploadedFile this[string key]
		{
			get 
			{ 
				return Get(key);
			}
		}

		/// <summary>
		/// The index'th <see cref="UploadedFile"/> in the collection.
		/// </summary>
		/// <value>
		/// The index'th <see cref="UploadedFile"/> in the collection.
		/// </value>
		public UploadedFile this[int index]
		{
			get 
			{ 
				return Get(index);
			}
		}

		/// <summary>
		/// The UniqueIDs of all of the controls with files in this collection.
		/// </summary>
		/// <value>
		/// The UniqueIDs of all of the controls with files in this collection.
		/// </value>
		public string[] AllKeys
		{
			get
			{
				lock (_SyncRoot)	{ return this.BaseGetAllKeys(); }
			}
		}


		/// <summary>
		/// Get the <see cref="UploadedFile"/> in the collection that was uploaded from
		/// the control with the specified UniqueID, or null if there is no such file.
		/// </summary>
		/// <param name="key">
		/// The UniqueID of the control.
		/// </param>
		/// <returns>
		/// The corresponding <see cref="UploadedFile"/>, or null if there is no such file.
		/// </returns>
		public UploadedFile Get(string key)
		{
			lock (_SyncRoot)
			{
				return (UploadedFile)this.BaseGet(key);
			}
		}

		/// <summary>
		/// Gets the index'th <see cref="UploadedFile"/> in the collection.
		/// </summary>
		/// <param name="index">
		/// The index into the collection.
		/// </param>
		/// <returns>
		/// The corresponding <see cref="UploadedFile"/>.
		/// </returns>
		public UploadedFile Get(int index)
		{
			lock (_SyncRoot)
			{
				return (UploadedFile)this.BaseGet(index);
			}
		}

		/// <summary>
		/// Gets an <see cref="IEnumerator"/> that can be used to enumerate through
		/// the <see cref="UploadedFile"/> objects in the collection.
		/// </summary>
		/// <returns>
		/// The <see cref="IEnumerator"/>.
		/// </returns>
		public new IEnumerator GetEnumerator()
		{
			return BaseGetAllValues().GetEnumerator();
		}

		/// <summary>
		/// Gets the index'th control UniqueID in the collection.
		/// </summary>
		/// <param name="index">
		/// The index into the collection.
		/// </param>
		/// <returns>
		/// The UniqueID of the control the uploaded the corresponding file.
		/// </returns>
		public string GetKey(int index)
		{
			lock (_SyncRoot)	{ return this.BaseGetKey(index); }
		}
		
		internal void Add(string key, UploadedFile file)
		{
			lock (_SyncRoot) { this.BaseAdd(key, file); }
			if (Changed != null)
				Changed(this, null);
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

		internal event EventHandler Changed;
	}
}
