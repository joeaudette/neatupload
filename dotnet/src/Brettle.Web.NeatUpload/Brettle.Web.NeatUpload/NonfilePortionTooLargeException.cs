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
	[Serializable]
	public class NonfilePortionTooLargeException : UploadException
	{			
		public NonfilePortionTooLargeException(long maxNormalRequestLength, long normalRequestLength) 
			: base(413, String.Format(ResourceManagerSingleton.GetResourceString("NonfilePortionTooLargeMessageFormat"), maxNormalRequestLength, normalRequestLength))
		{
			MaxNormalRequestLength = maxNormalRequestLength;
			NormalRequestLength = normalRequestLength;
		}

		[Obsolete("Use NonfilePortionTooLargeException(maxNormalRequestLength, normalRequestLength) instead")]
		public NonfilePortionTooLargeException(long maxNormalRequestLength) 
			: this(maxNormalRequestLength, 0)
		{
		}

		protected NonfilePortionTooLargeException(SerializationInfo info, StreamingContext context)
			: base (info, context) 
		{
			MaxNormalRequestLength = info.GetInt64("NonfilePortionTooLargeException.MaxNormalRequestLength");
			NormalRequestLength = info.GetInt64("NonfilePortionTooLargeException.NormalRequestLength");
		}

		public override void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData (info, context);
			info.AddValue ("NonfilePortionTooLargeException.MaxNormalRequestLength", MaxNormalRequestLength);
			info.AddValue ("NonfilePortionTooLargeException.NormalRequestLength", NormalRequestLength);
		}

		public long MaxNormalRequestLength = 0;
		public long NormalRequestLength = 0;
	}
}
