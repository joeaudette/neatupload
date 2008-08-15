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

namespace Brettle.Web.NeatUpload.Internal
{
	internal class SizeLimitingWorkerRequest : DecoratedWorkerRequest
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private long maxRequestLength;
		private long totalBytesRead;
		
		public SizeLimitingWorkerRequest (HttpWorkerRequest origWorker, long maxRequestLength) : base(origWorker)
		{
			this.maxRequestLength = maxRequestLength;
		}
		
		public override int ReadEntityBody (byte[] buffer, int size)
		{
			if (log.IsDebugEnabled) log.Debug("In ReadEntityBody() with size=" + size);
			int bytesRead = OrigWorker.ReadEntityBody(buffer, size);
			totalBytesRead += bytesRead;

			byte[] preloadedEntityBody = OrigWorker.GetPreloadedEntityBody();
			int bytesPreloaded = 0;
			if (preloadedEntityBody != null)
			{
				bytesPreloaded = preloadedEntityBody.Length;
			}

			if (totalBytesRead + bytesPreloaded > maxRequestLength)
			{ 
				IgnoreRemainingBodyAndThrow(new HttpException(413, "Request Entity Too Large"));
			}
			return bytesRead;
		}
	}
}

