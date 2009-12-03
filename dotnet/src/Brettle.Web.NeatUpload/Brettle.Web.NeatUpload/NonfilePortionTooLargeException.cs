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
	/// Indicates that the non-file portion of a request was too large.
	/// </summary>
	/// <remarks>To allow large uploads, ASP.NET needs to allow large requests.  The upload
	/// module only streams files in the request that are associated with a post-back ID.
	/// It does not stream other files or other portions of the request (collectively the
	/// non-file portion of the request).  If the non-file portion of the request is too
	/// large, the upload module can throw this exception as a way of rejecting the request.
	/// </remarks>
	[Serializable]
	public class NonfilePortionTooLargeException : UploadException
	{			
		/// <summary>
		/// Creates an <see cref="NonfilePortionTooLargeException"/>, given the maximum allowed
		/// length of the non-file portion of the request, and the actual length of the non-file
		/// portion of the request.
		/// </summary>
		/// <param name="maxNormalRequestLength">
		/// The maximum allowed length of the non-file portion of the request.
		/// </param>
		/// <param name="normalRequestLength">
		/// The length of the non-file portion of the request.
		/// </param>
		/// <remarks>The HTTP status code for this exception will be 413, and the message
		/// will be retrieved from the "NonfilePortionTooLargeMessageFormat" resource in 
		/// NeatUpload.Strings.resx.</remarks>
		public NonfilePortionTooLargeException(long maxNormalRequestLength, long normalRequestLength) 
			: base(413, String.Format(ResourceManagerSingleton.GetResourceString("NonfilePortionTooLargeMessageFormat"), maxNormalRequestLength, normalRequestLength))
		{
			MaxNormalRequestLength = maxNormalRequestLength;
			NormalRequestLength = normalRequestLength;
		}


		/// <summary>
		/// Creates an <see cref="NonfilePortionTooLargeException"/>, given the maximum allowed
		/// length of the non-file portion of the request.
		/// </summary>
		/// <param name="maxNormalRequestLength">
		/// The maximum allowed length of the non-file portion of the request.
		/// </param>
		/// <remarks>The HTTP status code for this exception will be 413, and the message
		/// will be retrieved from the "NonfilePortionTooLargeMessageFormat" resource in 
		/// NeatUpload.Strings.resx.</remarks>
		[Obsolete("Use NonfilePortionTooLargeException(maxNormalRequestLength, normalRequestLength) instead")]
		public NonfilePortionTooLargeException(long maxNormalRequestLength) 
			: this(maxNormalRequestLength, 0)
		{
		}

		/// <summary>
		/// Creates an <see cref="NonfilePortionTooLargeException"/> when deserializing.
		/// </summary>
		/// <param name="info">
		/// A <see cref="SerializationInfo"/>
		/// </param>
		/// <param name="context">
		/// A <see cref="StreamingContext"/>
		/// </param>
		protected NonfilePortionTooLargeException(SerializationInfo info, StreamingContext context)
			: base (info, context) 
		{
			MaxNormalRequestLength = info.GetInt64("NonfilePortionTooLargeException.MaxNormalRequestLength");
			NormalRequestLength = info.GetInt64("NonfilePortionTooLargeException.NormalRequestLength");
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
			info.AddValue ("NonfilePortionTooLargeException.MaxNormalRequestLength", MaxNormalRequestLength);
			info.AddValue ("NonfilePortionTooLargeException.NormalRequestLength", NormalRequestLength);
		}

		/// <summary>
		/// The maximum allowed length of the non-file portion of the request.
		/// </summary>
		public long MaxNormalRequestLength = 0;

		/// <summary>
		/// The length of the non-file portion of the request.
		/// </summary>
		public long NormalRequestLength = 0;
	}
}
