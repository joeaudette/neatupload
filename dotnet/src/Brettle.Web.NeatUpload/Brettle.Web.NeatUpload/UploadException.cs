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

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// An exception indicating that an upload has been rejected for some reason.
	/// </summary>
	[Serializable]
	public class UploadException : Exception
	{
		/// <summary>
		/// Constructs an <see cref="UploadException"/> given an HTTP status code
		/// and a message.
		/// </summary>
		/// <param name="httpStatusCode">
		/// The HTTP status code to return to the browser, if the upload can't be
		/// stopped.
		/// </param>
		/// <param name="message">
		/// A description of the exception.
		/// </param>
		public UploadException(int httpStatusCode, string message) : base(message)
		{
			HttpCode = httpStatusCode;
		}

		/// <summary>
		/// Creates an <see cref="UploadException"/> when deserializing.
		/// </summary>
		/// <param name="info">
		/// A <see cref="SerializationInfo"/>
		/// </param>
		/// <param name="context">
		/// A <see cref="StreamingContext"/>
		/// </param>
		protected UploadException(SerializationInfo info, StreamingContext context)
			: base (info, context) 
		{
			HttpCode = info.GetInt32("UploadException.HttpCode");
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
			info.AddValue ("UploadException.HttpCode", HttpCode);
		}

		/// <summary>
		/// The HTTP status code associated with this exception.
		/// </summary>
		public int HttpCode;
	}
}
