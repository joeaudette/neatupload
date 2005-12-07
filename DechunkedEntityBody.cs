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
	internal class DechunkedEntityBody : EntityBody
	{
		internal DechunkedEntityBody(HttpWorkerRequest worker) : base(worker)
		{
		}
		
		internal override int Read(byte[] buffer, int position, int count)
		{
			int totalBytesRead = 0;
			while (count > 0 && !IsEndOfBody)
			{
				if (BytesLeftInChunk == 0)
				{
					BytesLeftInChunk = ReadChunkSize();
					if (BytesLeftInChunk == 0)
					{
						ReadTrailers();
						IsLastChunk = true;
						IsEndOfBody = true;
						break;
					}
				}
				int bytesRead = ReadOrigEntityBody(buffer, position, (int)Math.Min(count, BytesLeftInChunk));
				if (bytesRead == 0)
				{
					IsEndOfBody = true;
					break;
				}
				count -= bytesRead;
				BytesLeftInChunk -= bytesRead;
				position += bytesRead;
				totalBytesRead += bytesRead;
				if (BytesLeftInChunk == 0)
				{
					ReadLine();
				}
			}
			return totalBytesRead;
		}
		
		private int ReadChunkSize()
		{
			string chunkSizeString = ReadLine();
			return Int32.Parse(chunkSizeString, NumberStyles.HexNumber);
		}
		
		
		private string ReadLine()
		{
			byte[] lineBytes = new byte[80];
			int pos = 0;
			while (pos < lineBytes.Length)
			{
				if (ReadOrigEntityBody(lineBytes, pos, 1) != 1)
				{
					throw new System.IO.EndOfStreamException();
				}
				if (pos > 0 && lineBytes[pos-1] == '\r' && lineBytes[pos] == '\n')
				{
					return System.Text.Encoding.ASCII.GetString(lineBytes, 0, pos-1);
				}
				pos++;
			}
			throw new ApplicationException("No CRLF found");
		}
		
		private void ReadTrailers()
		{
			string line = null;
			string headerName = null, headerValue = null;
			while (string.Empty != (line = ReadLine()))
			{
				string header = line.TrimStart(' ', '\t');
				if (header != line)
				{
					headerValue += line;
				}
				else
				{
					if (FoundTrailer != null && headerName != null)
					{
						FoundTrailer(headerName, headerValue);
					}
				
					int colonPos = header.IndexOf(':');
					if (colonPos < 0)
					{
						throw new System.ApplicationException("Could not find colon in header line: " + header);
					}
					headerName = header.Substring(0, colonPos);
					headerValue = header.Substring(colonPos + 1).Trim();
				}
			}
			if (FoundTrailer != null && headerName != null)
			{
				FoundTrailer(headerName, headerValue);
			}
		}
		
		internal delegate void FoundTrailerCallBack(string headerName, string headerValue);
		
		internal FoundTrailerCallBack FoundTrailer;
				
		internal override bool IsIncomplete { get { return !IsLastChunk; } }
		
		private bool IsLastChunk = false;
		private long BytesLeftInChunk = 0;
	}
}
