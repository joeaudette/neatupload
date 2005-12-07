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
	internal abstract class EntityBody
	{
		protected EntityBody(HttpWorkerRequest worker) : base()
		{
			OrigWorker = worker;
			OrigPreloadedBody = OrigWorker.GetPreloadedEntityBody();
		}
		
		internal abstract int Read(byte[] buffer, int position, int count);
		
		protected int ReadOrigEntityBody(byte[] destBuf, int position, int count)
		{
			int totalRead = 0;
			if (OrigPreloadedBody != null)
			{
				int read = Math.Min(count, OrigPreloadedBody.Length - OrigPreloadedBodyPos);
				if (read > 0) 
				{
					Buffer.BlockCopy(OrigPreloadedBody, OrigPreloadedBodyPos, destBuf, position+totalRead, read);
				}
				if (ReadSome != null)
				{
					ReadSome(true, destBuf, position+totalRead, read);
				}
				OrigPreloadedBodyPos += read;
				if (read < count)
				{
					OrigPreloadedBody = null;
				}
				count -= read;
				totalRead += read;
			}
			if (count > 0)
			{
				byte[] localBuffer = new byte[count];
				int read = OrigWorker.ReadEntityBody(localBuffer, count);
				if (read > 0) 
				{
					Buffer.BlockCopy(localBuffer, 0, destBuf, position+totalRead, read);
				}
				if (ReadSome != null)
				{
					ReadSome(true, destBuf, position+totalRead, read);
				}
				totalRead += read;
			}
			return totalRead;
		}
				
		internal abstract bool IsIncomplete { get; }
		
		internal delegate void ReadSomeCallBack(bool isPreloaded, byte[] buffer, int position, int count);
		
		internal ReadSomeCallBack ReadSome;
		
		protected bool IsEndOfBody;
		protected HttpWorkerRequest OrigWorker;
		private byte[] OrigPreloadedBody;
		private int OrigPreloadedBodyPos;
	}
}
