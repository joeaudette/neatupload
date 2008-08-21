/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2006  Dean Brettle

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
using System.Runtime.Serialization;
using Brettle.Web.NeatUpload.Internal;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Indicates that a StorageConfig object is not valid.
	/// </summary>
	[Serializable]
	public class InvalidStorageConfigException : UploadException
	{
		/// <summary>
		/// Creates an <see cref="InvalidStorageConfigException"/>, given a
		/// text description of the problem.
		/// </summary>
		/// <param name="details">
		/// A description of the problem.
		/// </param>
		public InvalidStorageConfigException(string details) 
			: base(500, String.Format(ResourceManagerSingleton.GetResourceString("InvalidStorageConfigMessageFormat"), details))
		{
			Details = details;
		}

		/// <summary>
		/// Creates an <see cref="InvalidStorageConfigException"/> when deserializing.
		/// </summary>
		/// <param name="info">
		/// A <see cref="SerializationInfo"/>
		/// </param>
		/// <param name="context">
		/// A <see cref="StreamingContext"/>
		/// </param>
		protected InvalidStorageConfigException(SerializationInfo info, StreamingContext context)
			: base (info, context) 
		{
			Details = info.GetString("InvalidStorageConfigException.Details");
		}

		/// <summary>
		/// Serializes this object.
		/// </summary>
		/// <param name="info">
		/// A <see cref="SerializationInfo"/>
		/// </param>
		/// <param name="context">
		/// A <see cref="StreamingContext"/>
		/// </param>
		public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData (info, context);
			info.AddValue ("InvalidStorageConfigException.Details", Details);
		}

		/// <summary>
		/// The description of the problem.
		/// </summary>
		public string Details;
	}
}
