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
	internal class FilteringWorkerRequest : DecoratedWorkerRequest
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private UploadContext uploadContext;
		
		internal UploadContext GetUploadContext()
		{
			return uploadContext;
		}
		

		public FilteringWorkerRequest (HttpWorkerRequest origWorker) : base(origWorker)
		{
			this.uploadContext = new UploadContext();
		}
		
		private const int maxHeadersSize = 512; // Total bytes required to hold all headers and boundary.
		private const int bufferSize = 4096; // Arbitrary (but > maxHeadersSize) 
		private byte[] buffer = new byte[bufferSize]; 
		private Stream outputStream = null;
		private Stream fileStream = null;
		MemoryStream preloadedEntityBodyStream = null;		
		private byte[] preloadedEntityBody = null;
		private int writePos = 0; // Where to put the next byte read from OrigWorker
		private int readPos = 0; // Where to get the next byte to put in a stream 
		private int parsePos = 0; // Where to get the next byte to parse
		private byte[] tmpBuffer = new byte[bufferSize];
		private byte[] boundary;
		private long origContentLength = -1;

		private int entityBodyPos = 0;
		private bool isParsed = false;

/*		
		// The following 2 methods are useful for debugging but use them sparingly.
		// They produce a lot of output.		
		private void printParsePos(string msg)
		{
			byte[] tmpBuf = new byte[256];
			Buffer.BlockCopy(buffer, parsePos, tmpBuf, 0, Math.Min(writePos - parsePos, 256));
			if (log.IsDebugEnabled) log.Debug(msg + ": buffer[parsePos]=" + System.Text.Encoding.ASCII.GetString(tmpBuf));
		}
				
		private void printWriting(string msg)
		{
			byte[] tmpBuf = new byte[parsePos - readPos];
			Buffer.BlockCopy(buffer, readPos, tmpBuf, 0, parsePos - readPos);
			if (log.IsDebugEnabled) log.Debug("Writing to: " + outputStream + " " + msg + "[" +readPos + "," + parsePos + "]="
							 + System.Text.Encoding.ASCII.GetString(tmpBuf));
		}
*/

		private string GetAttribute(string header, string attrName)
		{
			int colonPos = header.IndexOf(':');
			string nameEqual = " " + attrName + "=";
			int valPos = header.IndexOf(nameEqual, colonPos+1);
			if (valPos < 0) return null;
			valPos += nameEqual.Length;
			int endValPos;
			if (header[valPos] == '"')
			{
				valPos++;
				endValPos = header.IndexOf('"', valPos);
				if (endValPos < 0) return null;
			}
			else
			{
				endValPos = header.IndexOf(';', valPos);
				if (endValPos < 0)
					endValPos = header.IndexOf(' ', valPos);
				if (endValPos < 0)
					endValPos = header.Length;
			}
			return header.Substring(valPos, endValPos - valPos);
		}

		private string GetLine()
		{
			int lineStart = parsePos;
			int lfIndex = Array.IndexOf(buffer, (byte)'\n', parsePos, writePos-parsePos);
			if (lfIndex < 0) 
			{
				return null;
			}
			parsePos = lfIndex+1;
			if (lfIndex > 0 && buffer[lfIndex-1] == '\r') 
				lfIndex--;
			return ContentEncoding.GetString(buffer, lineStart, lfIndex-lineStart);
		}
		
		private System.Text.Encoding ContentEncoding = System.Text.Encoding.UTF8;
		
		private static bool ArraysEqual(byte[] arr1, int pos1, byte[] arr2, int pos2, int count)
		{
			if (pos1 + count > arr1.Length || pos2 + count > arr2.Length)
				return false;
			for (int i = 0; i < count; i++)
			{
				if (arr1[pos1 + i] != arr2[pos2 + i])
					return false;
			}
			return true;
		}
		
		private bool FindBoundary(bool doneReading)
		{
			while (parsePos + boundary.Length + 4 <= writePos)
			{			
				int lfIndex = Array.IndexOf(buffer, (byte)'\n', parsePos + boundary.Length, 
											writePos - (parsePos + boundary.Length));
				if (lfIndex < 0)
				{
					parsePos = writePos;
					if (!doneReading)
					{
						parsePos -= (boundary.Length + 4);
					}
					return false;
				}
				if (lfIndex > 0 && buffer[lfIndex-1] == '\r') 
				{
					lfIndex--;
				}
				if (lfIndex - boundary.Length >= 0)
				{
					parsePos = lfIndex - boundary.Length; 
					if (lfIndex >= 2 && buffer[lfIndex-1] == '-' && buffer[lfIndex-2] == '-')
					{
						parsePos -= 2;
					}
					if (parsePos >= 0 && ArraysEqual(boundary, 0, buffer, parsePos, boundary.Length))
					{
						if (log.IsDebugEnabled) log.Debug("grandTotalBytesRead=" + grandTotalBytesRead);
						if (log.IsDebugEnabled) log.Debug("Found boundary");
						return true;
					}
					parsePos = lfIndex;
				}
				else
				{
					parsePos = lfIndex;
				}
			}
			return false;
		}

		private bool doneReading = false;
		private long grandTotalBytesRead = 0;
		private int origPreloadedBodyPos = 0;
		private byte[] origPreloadedBody = null;
		
		private int ReadOrigEntityBody(byte[] destBuf, int count)
		{
			// If the upload was cancelled, return a 204 error code which tells the client that it
			// "SHOULD NOT change its document view from that which caused the request to be sent" (RFC 2616 10.2.5)
			if (uploadContext.Status == UploadStatus.Cancelled)
			{
				throw new HttpException(204, "Upload cancelled by user");
			}
			
			int totalRead = 0;
			if (origPreloadedBody != null)
			{
				int read = Math.Min(count, origPreloadedBody.Length - origPreloadedBodyPos);
				if (read > 0) 
				{
					Buffer.BlockCopy(origPreloadedBody, origPreloadedBodyPos, destBuf, totalRead, read);
				}
				origPreloadedBodyPos += read;
				if (read < count)
				{
					origPreloadedBody = null;
				}
				count -= read;
				totalRead += read;
			}
			if (count > 0)
			{
				byte[] localBuffer = new byte[count];
				int read = OrigWorker.ReadEntityBody(localBuffer, count);
				if (Config.Current.DebugDirectory != null)
				{
					LogEntityBodyStream.Write(localBuffer, 0, read);
					LogEntityBodySizesStream.WriteLine(read);
				}
				if (read > 0) 
				{
					Buffer.BlockCopy(localBuffer, 0, destBuf, totalRead, read);
				}
				totalRead += read;
			}
			return totalRead;
		}
		
		private int FillBuffer()
		{
			if (doneReading)
				return 0;
			int bytesRead = 0;
			int totalBytesRead = 0;
/*
			if (log.IsDebugEnabled) log.DebugFormat("tmpBuffer.Length = {0}, bufferSize = {1}, writePos = {2}, origContentLength = {3}, grandTotalBytesRead = {4}",
												tmpBuffer.Length, bufferSize, writePos, origContentLength, grandTotalBytesRead);
*/
			while (writePos < bufferSize 
					&& 0 < (bytesRead = ReadOrigEntityBody(tmpBuffer, 
					(origContentLength == -1) ? (bufferSize - writePos) : (int)Math.Min(bufferSize - writePos, origContentLength - grandTotalBytesRead))))
			{
				// Fill the buffer
				Buffer.BlockCopy(tmpBuffer, 0, buffer, writePos, bytesRead);
				writePos += bytesRead;
				totalBytesRead += bytesRead;
				grandTotalBytesRead += bytesRead;
				uploadContext.BytesRead = grandTotalBytesRead;
/*
				if (log.IsDebugEnabled) log.DebugFormat("tmpBuffer.Length = {0}, bufferSize = {1}, writePos = {2}, origContentLength = {3}, grandTotalBytesRead = {4}",
													tmpBuffer.Length, bufferSize, writePos, origContentLength, grandTotalBytesRead);
*/
			}
			if (bytesRead == 0)
				doneReading = true;
			return totalBytesRead;
		}


		private void WriteParsedToOutputStream()
		{
			outputStream.Write(buffer, readPos, parsePos-readPos);
			readPos = parsePos;
			
/*
			if (log.IsDebugEnabled) 
			{
				log.DebugFormat("preloadedEntityBodyStream.Length = {0}, UploadHttpModule.MaxNormalRequestLength = {1}",
							preloadedEntityBodyStream.Length, UploadHttpModule.MaxNormalRequestLength);
			}
*/
			// If the file or non-file portion of the request is too large, throw an exception.
			if (preloadedEntityBodyStream.Length > UploadHttpModule.MaxNormalRequestLength 
				|| this.grandTotalBytesRead > UploadHttpModule.MaxRequestLength ) {
				if (log.IsDebugEnabled) log.Debug("Request Entity Too Large");
				throw new UploadTooLargeException(UploadHttpModule.MaxRequestLength);
			}
		}

		private void ShiftAndFill()
		{
			Buffer.BlockCopy(buffer, parsePos, buffer, 0, writePos-parsePos);
			writePos -= parsePos;
			readPos -= parsePos;
			parsePos = 0;
			// Fill the rest of the buffer
			if (!doneReading && FillBuffer() == 0)
				doneReading = true;
		}

		private bool CopyUntilBoundary()
		{
			// Look for the boundary
			while (true)
			{
				// If necessary, shift and refill the buffer
				if (parsePos + boundary.Length + maxHeadersSize > writePos)
				{
					// Write everything that has been parsed to output stream
					WriteParsedToOutputStream();

					// Put the parse position at the beginning of the buffer
					ShiftAndFill();
				}
				
				// Look for a boundary.  If we find one, return true.  If we don't
				// loop again until we run out of data.
				bool foundBoundary = FindBoundary(doneReading);

				// Write everything that has been parsed to output stream
				WriteParsedToOutputStream();

				if (foundBoundary) 
				{
					if (parsePos + maxHeadersSize > writePos)
					{
						ShiftAndFill();
					}
					return true;
				}
				else if (doneReading)
					break;
			}
			return false;
		}

		private void ParseMultipart()
		{
			if (isParsed)
			{
				return;
			}
			isParsed = true;
			try
			{
				ParseOrThrow();
			}
			catch (Exception ex)
			{
				uploadContext.Exception = ex;
				if (ex is UploadException)
				{
					uploadContext.Status = UploadStatus.Rejected;
					// Wait 5 seconds to give the client a chance to stop the request.  If the client
                    // stops the request, the user will see the original form instead of an error page.
                    // Regardless, the progress display will show the error so the user knows what went wrong.
					System.Threading.Thread.Sleep(5000);
				}
				else if (uploadContext.Status != UploadStatus.Cancelled)
				{
					uploadContext.Status = UploadStatus.Failed;
				}

				byte[] buffer = new byte[4096];
				while (0 < OrigWorker.ReadEntityBody(buffer, buffer.Length))
					; // Ignore the remaining body
				
				log.Error("Rethrowing exception in ParseMultipart", ex);
				throw;
			}
			finally
			{
				if (fileStream != null)
					fileStream.Close();
				if (preloadedEntityBodyStream != null)
					preloadedEntityBodyStream.Close();
				if (LogEntityBodyStream != null)
					LogEntityBodyStream.Close();
				if (LogEntityBodySizesStream != null)
					LogEntityBodySizesStream.Close();
				uploadContext.StopTime = DateTime.Now;
			}
		}
		
		private Stream LogEntityBodyStream = null;
		private StreamWriter LogEntityBodySizesStream = null;
		private void ParseOrThrow()
		{			
			origPreloadedBody = OrigWorker.GetPreloadedEntityBody();
			string contentTypeHeader = OrigWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentType);
			string contentLengthHeader = OrigWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentLength);
			if (contentLengthHeader != null)
			{
				origContentLength = Int64.Parse(contentLengthHeader);
			}

			if (Config.Current.DebugDirectory != null)
			{
				string logEntityBodyBaseName = Path.Combine(Config.Current.DebugDirectory.FullName,
			                                                DateTime.Now.Ticks.ToString());
				LogEntityBodyStream = File.Create(logEntityBodyBaseName + ".body");
				LogEntityBodySizesStream = File.CreateText(logEntityBodyBaseName + ".sizes");
				LogEntityBodySizesStream.WriteLine(contentTypeHeader);
				LogEntityBodySizesStream.WriteLine(contentLengthHeader);
				if (origPreloadedBody != null)
				{
					LogEntityBodyStream.Write(origPreloadedBody, 0, origPreloadedBody.Length);
					LogEntityBodySizesStream.WriteLine(origPreloadedBody.Length);
				}
				else
				{
					LogEntityBodySizesStream.WriteLine(0);
				}
			}
			uploadContext.SetContentLength(origContentLength);
			if (log.IsDebugEnabled) log.Debug("=" + contentLengthHeader + " -> " + origContentLength);
			
			boundary = System.Text.Encoding.ASCII.GetBytes("--" + GetAttribute(contentTypeHeader, "boundary"));
			if (log.IsDebugEnabled) log.Debug("boundary=" + System.Text.Encoding.ASCII.GetString(boundary));
			
			string charset = GetAttribute(contentTypeHeader, "charset");
			if (charset != null)
			{
				try
				{
					System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(charset);
					ContentEncoding = encoding;
				}
				catch (NotSupportedException)
				{
					if (log.IsDebugEnabled) log.Debug("Ignoring unsupported charset " + charset + ".  Using utf-8.");
				}
			}

			preloadedEntityBodyStream = new MemoryStream();
			outputStream = preloadedEntityBodyStream;
			readPos = writePos = parsePos = 0;
			while (CopyUntilBoundary())
			{
				// If we were writing to a file, close it
				if (outputStream == fileStream && outputStream != null)
				{
					outputStream.Close();
				}
				
				// parse the headers
				string fileID = null, fileName = null, contentType = null;
				if (boundary[0] != (byte)'\r')
				{
					byte[] newBoundary = new byte[boundary.Length + 2];
					Buffer.BlockCopy(boundary, 0, newBoundary, 2, boundary.Length);
					newBoundary[0] = (byte)'\r';
					newBoundary[1] = (byte)'\n';
					boundary = newBoundary;
				}
				else
				{
					GetLine(); // Blank line
				}
				GetLine(); // boundary line
				string header;
				while (null != (header = GetLine()))
				{
					if (log.IsDebugEnabled) log.Debug("header=" + header);
					int colonPos = header.IndexOf(':');
					if (colonPos < 0)
					{
						break;
					}
					string headerName = header.Substring(0, colonPos);
					if (String.Compare(headerName, "Content-Disposition", true) == 0)
					{
						fileID = GetAttribute(header, "name");
						fileName = GetAttribute(header, "filename");
					}
					else if (String.Compare(headerName, "Content-Type", true) == 0)
					{
						contentType = header.Substring(colonPos + 1).Trim();
					}
				}
				if (log.IsDebugEnabled) log.Debug("fileID = " + fileID);
				if (log.IsDebugEnabled) log.Debug("fileName = " + fileName);
				if (fileName != null && fileID.StartsWith(UploadContext.NamePrefix))
				{
					if (log.IsDebugEnabled) log.Debug("Calling UploadContext.Current.CreateUploadedFile(" + fileID + "...)");
					UploadedFile uploadedFile = uploadContext.CreateUploadedFile(fileID, fileName, contentType);
					outputStream = fileStream = uploadedFile.CreateStream();
					readPos = parsePos; // Skip past the boundary and headers

					// If the client-specified content length is too large, we set the status to
					// RejectedRequestTooLarge so that progress displays will stop.  We do this after 
					// having created the UploadedFile because that is necessary for the progress display
					// to find the uploadContext.
					if (origContentLength > UploadHttpModule.MaxRequestLength)
					{
						if (log.IsDebugEnabled) log.Debug("contentLength > MaxRequestLength");
						uploadContext.Status = UploadStatus.Cancelled;
						throw new UploadTooLargeException(UploadHttpModule.MaxRequestLength);
					}

					// Write out a replacement part that just contains the filename as the value.
					preloadedEntityBodyStream.Write(boundary, 0, boundary.Length);
					System.Text.StringBuilder replacementPart = new System.Text.StringBuilder();
					replacementPart.Append("\r\nContent-Disposition: form-data; name=\"" + uploadedFile.ControlUniqueID + "\"\r\n\r\n");
					replacementPart.Append(fileName);
					byte[] replacementPartBytes = System.Text.Encoding.ASCII.GetBytes(replacementPart.ToString());
					preloadedEntityBodyStream.Write(replacementPartBytes, 0, replacementPartBytes.Length);
				}
				else
				{
					outputStream = preloadedEntityBodyStream;
				}
			}
			if (log.IsDebugEnabled) log.Debug("Done parsing.");
			outputStream.WriteByte(10);
			outputStream.Close();
			preloadedEntityBody = preloadedEntityBodyStream.ToArray();
			preloadedEntityBodyStream = null;
			if (grandTotalBytesRead < origContentLength)
			{
				uploadContext.Status = UploadStatus.Cancelled;
				bool isClientConnected = false;
				try
				{
					isClientConnected = OrigWorker.IsClientConnected();
				}
				catch (Exception)
				{
					// Mono throws an exception if the client is no longer connected.
				}
				if (isClientConnected)
				{
					throw new HttpException (400, String.Format("Data length ({0}) is shorter than Content-Length ({1}) and client is still connected.", grandTotalBytesRead, origContentLength));
				}
				else
				{
					throw new HttpException (400, String.Format("Client disconnected after receiving {0} of {1} bytes -- user probably cancelled upload.", grandTotalBytesRead, origContentLength));
				}
			}
			uploadContext.Status = UploadStatus.Completed;
		}
		
		public override int ReadEntityBody (byte[] buffer, int size)
		{
			ParseMultipart();
			int count = Math.Min(size, preloadedEntityBody.Length - entityBodyPos);
			Buffer.BlockCopy(preloadedEntityBody, entityBodyPos, buffer, 0, count);
			entityBodyPos += count;
			return count;
		}

		public override string GetKnownRequestHeader (int index)
		{
			if (index == HttpWorkerRequest.HeaderContentLength)
			{
				ParseMultipart();
				return preloadedEntityBody.Length.ToString();
			}
			return OrigWorker.GetKnownRequestHeader (index);
		}

		public override void EndOfRequest ()
		{
			base.EndOfRequest();
		}
		
		public override byte [] GetPreloadedEntityBody ()
		{
			ParseMultipart();
			return preloadedEntityBody;
		}

		public override bool IsEntireEntityBodyIsPreloaded ()
		{
			ParseMultipart();
			return true;
		}
	}
}

