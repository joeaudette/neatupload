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
		public static int Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine(Environment.GetCommandLineArgs()[0] + " base_name_of_entity_body_file");
				return -1;
			}
			string entityBodyBaseName = args[0];
			MockWorkerRequest mockWorkerRequest = new MockWorkerRequest(entityBodyBaseName);
			FilteringWorkerRequest filteringWorkerRequest = new FilteringWorkerRequest(mockWorkerRequest);
			Stream filteredBodyStream = File.Create(entityBodyBaseName + ".body.filtered");
			byte[] preloadedBody = filteringWorkerRequest.GetPreloadedEntityBody();
			filteredBodyStream.Write(preloadedBody, 0, preloadedBody.Length);
			int read = 0;
			byte[] buffer = new byte[8192*1024];
			while (0 < (read = filteringWorkerRequest.ReadEntityBody(buffer, buffer.Length)))
			{
				filteredBodyStream.Write(buffer, 0, read);
			}
			mockWorkerRequest.EndOfRequest();
			filteredBodyStream.Close();
			Hashtable uploadedFiles = filteringWorkerRequest.GetUploadContext().uploadedFiles;
			foreach (string id in uploadedFiles.Keys)
			{
				System.Console.WriteLine("Control ID = " + id);
				UploadedFile uploadedFile = uploadedFiles[id] as UploadedFile;
				System.Console.WriteLine("  ContentType = " + uploadedFile.ContentType);
				System.Console.WriteLine("  FileName = " + uploadedFile.FileName);
				if (uploadedFile.TmpFile != null)
				{
					FileInfo destFile = new FileInfo(entityBodyBaseName + ".file." + id);
					if (destFile.Exists)
					{
						destFile.Delete();
					}
					uploadedFile.TmpFile.MoveTo(destFile.FullName);
					System.Console.WriteLine("  TmpFile.FullName = " + uploadedFile.TmpFile.FullName);
					System.Console.WriteLine("  TmpFile.Length = " + uploadedFile.TmpFile.Length);
				}
			}
			return 0;
		}
	}
}

