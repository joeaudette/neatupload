/*

NeatUpload - an HttpModule and User Control for uploading large files
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
using System.IO;

namespace Brettle.Web.NeatUpload
{
	public class FilterTester
	{
		public abstract class ResultAccumulator
		{
			abstract public Stream FilteredBodyStream { get; }
			abstract public void AddFile(string id, FileInfo tmpFile, string contentType, string fileName);
		}
		
		public static void Run(Stream entityBodyStream,
		                      StreamReader entityBodySizesStream,
		                      ResultAccumulator results)
		{
			MockWorkerRequest mockWorkerRequest = new MockWorkerRequest(entityBodyStream, entityBodySizesStream);
			FilteringWorkerRequest filteringWorkerRequest = new FilteringWorkerRequest(mockWorkerRequest);
			byte[] preloadedBody = filteringWorkerRequest.GetPreloadedEntityBody();
			results.FilteredBodyStream.Write(preloadedBody, 0, preloadedBody.Length);
			int read = 0;
			byte[] buffer = new byte[8192*1024];
			while (0 < (read = filteringWorkerRequest.ReadEntityBody(buffer, buffer.Length)))
			{
				results.FilteredBodyStream.Write(buffer, 0, read);
			}
			mockWorkerRequest.EndOfRequest();
			results.FilteredBodyStream.Close();
			UploadedFileCollection uploadedFiles = filteringWorkerRequest.GetUploadContext().Files;
			foreach (string id in uploadedFiles.Keys)
			{
				UploadedFile uploadedFile = uploadedFiles[id] as UploadedFile;
				results.AddFile(id, uploadedFile.TmpFile, uploadedFile.ContentType, uploadedFile.FileName);
			}
			return;
				
		}
	}
}

