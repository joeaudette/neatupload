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
using System.Runtime.Serialization;
using Brettle.Web.NeatUpload.Internal;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Indicates that the upload was too large.
	/// </summary>
	[Serializable]
	public class UploadTooLargeException : UploadException
	{
		/// <summary>
		/// Creates an <see cref="UploadTooLargeException"/>, given the maximum allowed
		/// request length, and the length of the request which was too large.
		/// </summary>
		/// <param name="maxRequestLength">
		/// The maximum allowed request length
		/// </param>
		/// <param name="requestLength">
		/// The length of the request which was too large.
		/// </param>
		/// <remarks>The HTTP status code for this exception will be 413, and the message
		/// will be retrieved from the "UploadTooLargeMessageFormat" resource in 
		/// NeatUpload.Strings.resx.</remarks>
		public UploadTooLargeException(long maxRequestLength, long requestLength) 
			: base(413, String.Format(ResourceManagerSingleton.GetResourceString("UploadTooLargeMessageFormat"), maxRequestLength, requestLength))
		{
			MaxRequestLength = maxRequestLength;
			RequestLength = requestLength;
		}

		/// <summary>
		/// Creates an <see cref="UploadTooLargeException"/>, given the maximum allowed
		/// request length.
		/// </summary>
		/// <param name="maxRequestLength">
		/// The maximum allowed request length
		/// </param>
		/// <remarks>The HTTP status code for this exception will be 413, and the message
		/// will be retrieved from the "UploadTooLargeMessageFormat" resource in 
		/// NeatUpload.Strings.resx.</remarks>
		[Obsolete("Use UploadTooLargeException(maxRequestLength, requestLength) instead")]
		public UploadTooLargeException(long maxRequestLength) 
			: this(maxRequestLength, 0)
		{
		}

		/// <summary>
		/// Creates an <see cref="UploadTooLargeException"/> when deserializing.
		/// </summary>
		/// <param name="info">
		/// A <see cref="SerializationInfo"/>
		/// </param>
		/// <param name="context">
		/// A <see cref="StreamingContext"/>
		/// </param>
		protected UploadTooLargeException(SerializationInfo info, StreamingContext context)
			: base (info, context) 
		{
			MaxRequestLength = info.GetInt64("UploadTooLargeException.MaxRequestLength");
			RequestLength = info.GetInt64("UploadTooLargeException.RequestLength");
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
			info.AddValue ("UploadTooLargeException.MaxRequestLength", MaxRequestLength);
			info.AddValue ("UploadTooLargeException.RequestLength", RequestLength);
		}

		/// <summary>
		/// The maximum allowed request length.
		/// </summary>
		public long MaxRequestLength = 0;

		/// <summary>
		/// The length of the request that was too large.
		/// </summary>
		public long RequestLength = 0;
	}
}
