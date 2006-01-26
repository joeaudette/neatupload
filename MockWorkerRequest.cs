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
	internal class MockWorkerRequest : DecoratedWorkerRequest
	{
		// Create a logger for use in this class
/*
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);		
*/

		private Stream LogEntityBodyStream = null;
		private StreamReader LogEntityBodySizesStream = null;
		private string ContentTypeHeader;
		private string ContentLengthHeader;
		private byte[] PreloadedBody = null;
		
		public MockWorkerRequest (Stream logEntityBodyStream, StreamReader logEntityBodySizesStream) : base(null)
		{
			LogEntityBodyStream = logEntityBodyStream;
			LogEntityBodySizesStream = logEntityBodySizesStream;
			ContentTypeHeader = LogEntityBodySizesStream.ReadLine();
			ContentLengthHeader = LogEntityBodySizesStream.ReadLine();
			long preloadedBodyLength = long.Parse(LogEntityBodySizesStream.ReadLine());
			if (preloadedBodyLength > 0)
			{
				PreloadedBody = new byte[preloadedBodyLength];
				LogEntityBodyStream.Read(PreloadedBody, 0, PreloadedBody.Length);
			}
		}
		
		public MockWorkerRequest (string logEntityBodyBaseName)
			: this(File.Open(logEntityBodyBaseName + ".body", FileMode.Open),
		    	   File.OpenText(logEntityBodyBaseName + ".sizes"))
		{
		}
		
		private int bytesRemainingInBlock = 0;
		private byte[] block;
		public override int ReadEntityBody (byte[] buffer, int size)
		{
			if (size == 0)
			{
				return 0;
			}
			int read = 0;
			if (bytesRemainingInBlock > 0)
			{
				read = Math.Min(size, bytesRemainingInBlock);
				Buffer.BlockCopy(block, block.Length - bytesRemainingInBlock, buffer, 0, read);
			}
			else
			{
				bytesRemainingInBlock = int.Parse(LogEntityBodySizesStream.ReadLine());
				block = new byte[bytesRemainingInBlock];
				LogEntityBodyStream.Read(block, 0, bytesRemainingInBlock);
				read = Math.Min(size, bytesRemainingInBlock);
				Buffer.BlockCopy(block, 0, buffer, 0, read);
			}
			bytesRemainingInBlock -= read;
			return read;
		}

		public override string GetKnownRequestHeader (int index)
		{
			if (index == HttpWorkerRequest.HeaderContentLength)
			{
				return ContentLengthHeader;
			}
			else if (index == HttpWorkerRequest.HeaderContentType)
			{
				return ContentTypeHeader;
			}
			return null;
		}
		
		public override byte [] GetPreloadedEntityBody ()
		{
			return PreloadedBody;
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			return (long.Parse(ContentLengthHeader) == PreloadedBody.Length);
		}

		public override void EndOfRequest ()
		{
			LogEntityBodyStream.Close();
			LogEntityBodySizesStream.Close();
			base.EndOfRequest();
		}
	}
}

