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
using System.Collections;
using System.Globalization;
using System.Web;
using System.Web.Configuration;
using System.IO;

namespace Brettle.Web.NeatUpload
{
	internal class FixedSizeEntityBody : EntityBody
	{
		internal FixedSizeEntityBody(HttpWorkerRequest worker, long origContentLength) : base(worker)
		{
			BytesRemaining = origContentLength;
		}
		
		internal override int Read(byte[] buffer, int position, int count)
		{
			if (BytesRemaining <= 0 || count <= 0)
				return 0;
			int bytesRead = ReadOrigEntityBody(buffer, position, (int)Math.Min(count, BytesRemaining));
			BytesRemaining -= bytesRead;
			if (bytesRead == 0 || BytesRemaining == 0)
			{
				IsEndOfBody = true;
			}
			return bytesRead;
		}
		
		internal override bool IsIncomplete { get { return (BytesRemaining != 0); } }
		
		private long BytesRemaining;
	}
}
