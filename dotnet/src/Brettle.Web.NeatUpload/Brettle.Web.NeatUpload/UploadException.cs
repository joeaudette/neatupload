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
	[Serializable]
	public class UploadException : Exception
	{
		public UploadException(int httpStatusCode, string message) : base(message)
		{
			HttpCode = httpStatusCode;
		}
		
		protected UploadException(SerializationInfo info, StreamingContext context)
			: base (info, context) 
		{
			HttpCode = info.GetInt32("UploadException.HttpCode");
		}

		public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData (info, context);
			info.AddValue ("UploadException.HttpCode", HttpCode);
		}
		
		public int HttpCode;
	}
}
