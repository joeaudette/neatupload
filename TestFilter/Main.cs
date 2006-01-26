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
using System.IO;
using System.Text;

[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch=true)]

class MainClass
{
	public class MyResultAccumulator : Brettle.Web.NeatUpload.FilterTester.ResultAccumulator
	{
		public MyResultAccumulator(string entityBodyBaseName, Stream filteredBodyStream)
		{
			EntityBodyBaseName = entityBodyBaseName;
			_FilteredBodyStream = filteredBodyStream;
		}
		
		private string EntityBodyBaseName;
		
		private Stream _FilteredBodyStream;
		public override Stream FilteredBodyStream
		{
			get { return _FilteredBodyStream; }
		}
		public override void AddFile(string id, FileInfo tmpFile, string contentType, string fileName)
		{
			System.Console.WriteLine("Control ID = " + id);
			System.Console.WriteLine("  ContentType = " + contentType);
			System.Console.WriteLine("  FileName = " + fileName);
			if (tmpFile != null)
			{
				FileInfo destFile = new FileInfo(EntityBodyBaseName + ".file." + id);
				if (destFile.Exists)
				{
					destFile.Delete();
				}
				tmpFile.MoveTo(destFile.FullName);
				System.Console.WriteLine("  TmpFile.FullName = " + tmpFile.FullName);
				System.Console.WriteLine("  TmpFile.Length = " + tmpFile.Length);
			}
		}
	}
	
	public class SingleFileResultAccumulator : Brettle.Web.NeatUpload.FilterTester.ResultAccumulator
	{
		public SingleFileResultAccumulator(Stream filteredBodyStream)
		{
			_FilteredBodyStream = filteredBodyStream;
		}
				
		private Stream _FilteredBodyStream;
		public override Stream FilteredBodyStream
		{
			get { return _FilteredBodyStream; }
		}
		
		public string ID;
		public FileInfo TmpFile;
		public string ContentType;
		public string FileName;
		public override void AddFile(string id, FileInfo tmpFile, string contentType, string fileName)
		{
			ID = id;
			ContentType = contentType;
			FileName = fileName;
			TmpFile = tmpFile;
		}
	}
	
	public static int Main(string[] args)
	{
		if (args.Length == 1)
		{
			string entityBodyBaseName = args[0];
			Stream logEntityBodyStream = File.Open(entityBodyBaseName + ".body", FileMode.Open);
			StreamReader logEntityBodySizesStream = File.OpenText(entityBodyBaseName + ".sizes");
			Stream filteredBodyStream = File.Create(entityBodyBaseName + ".body.filtered");
			MyResultAccumulator results = new MyResultAccumulator(entityBodyBaseName, filteredBodyStream);
			Brettle.Web.NeatUpload.FilterTester.Run(logEntityBodyStream, logEntityBodySizesStream, results);
			return 0;
		}
		
		string boundary = @"---------------------------boundary";
		for (int i = 4096; i >= 0; i--)
		{
			StringBuilder contentBuilder = new StringBuilder(i);
			contentBuilder.Append('x', i);
			MemoryStream entityBodyStream = new MemoryStream();
			StreamWriter entityBodyStreamWriter = new StreamWriter(entityBodyStream);
			entityBodyStreamWriter.NewLine = "\r\n";
			entityBodyStreamWriter.WriteLine(@"--" + boundary);
			entityBodyStreamWriter.WriteLine(@"Content-Disposition: form-data; name=""NeatUpload_" + String.Format("{0:00000}", i) + @"-inputFile""; filename=""test.txt""");
			entityBodyStreamWriter.WriteLine(@"Content-Type: text/plain");
			entityBodyStreamWriter.WriteLine(@"");
			entityBodyStreamWriter.WriteLine(contentBuilder.ToString());
			/*
			entityBodyStreamWriter.WriteLine(@"--" + boundary);
			entityBodyStreamWriter.WriteLine(@"Content-Disposition: form-data; name=""dummy""");
			entityBodyStreamWriter.WriteLine(@"");
			entityBodyStreamWriter.WriteLine(@"foo");
			*/
			entityBodyStreamWriter.WriteLine(@"--" + boundary + "--");
			entityBodyStreamWriter.Flush();
			
			MemoryStream entityBodySizesStream = new MemoryStream();
			StreamWriter entityBodySizesStreamWriter = new StreamWriter(entityBodySizesStream);
			entityBodySizesStreamWriter.WriteLine(@"multipart/form-data; boundary=" + boundary);
			entityBodySizesStreamWriter.WriteLine(entityBodyStream.Length);
			entityBodySizesStreamWriter.WriteLine(entityBodyStream.Length);
			entityBodySizesStreamWriter.WriteLine(0);
			entityBodySizesStreamWriter.Flush();
			
			entityBodyStream.Seek(0, SeekOrigin.Begin);
			entityBodySizesStream.Seek(0, SeekOrigin.Begin);
			
			StreamReader entityBodySizesStreamReader = new StreamReader(entityBodySizesStream);
			Stream filteredBodyStream = new MemoryStream();
			SingleFileResultAccumulator results
				= new SingleFileResultAccumulator(filteredBodyStream);
			Brettle.Web.NeatUpload.FilterTester.Run(entityBodyStream, entityBodySizesStreamReader, results);
			
			FileInfo destFile = new FileInfo("actual." + contentBuilder.Length);
			if (destFile.Exists)
			{
				destFile.Delete();
			}
			if (results.TmpFile.Length != contentBuilder.Length)
			{
				System.Console.WriteLine("");
				System.Console.WriteLine(i + ": actual(" + results.TmpFile.Length + ") != expected(" + contentBuilder.Length + ")");
				results.TmpFile.MoveTo(destFile.FullName);
				// results.TmpFile.Delete();
				
				// throw new ApplicationException("wrong length: actual(" + results.TmpFile.Length + ") != expected(" + contentBuilder.Length + ")");
			}
			else
			{
				System.Console.Write(".");
				System.Console.Out.Flush();
				results.TmpFile.Delete();
			}
			
		}
		return 0;		
	}
}
	
	