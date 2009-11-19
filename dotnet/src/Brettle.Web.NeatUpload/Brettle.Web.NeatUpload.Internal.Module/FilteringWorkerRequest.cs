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
using System.Collections.Specialized;

namespace Brettle.Web.NeatUpload.Internal.Module
{
	internal class FilteringWorkerRequest : DecoratedWorkerRequest
	{
		
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public FilteringWorkerRequest (HttpWorkerRequest origWorker) : base(origWorker)
		{
		}

		UploadState _UploadState;
		private UploadState UploadState {
			get {
				if (_UploadState == null)
				{
					_UploadState = UploadHttpModule.CurrentUploadState;
				}
				return _UploadState;
			}
			set {
				_UploadState = value;
				UploadHttpModule.CurrentUploadState = value;
			}
		}

		string _MultiRequestControlID;
		private string MultiRequestControlID {
			get {
				if (_MultiRequestControlID == null)
				{
					_MultiRequestControlID = UploadHttpModule.CurrentMultiRequestControlID;
				}
				return _MultiRequestControlID;
			}
		}
		
		private const int maxHeadersSize = 512; // Total bytes required to hold all headers and boundary.
		private const int bufferSize = 4096; // Arbitrary (but > maxHeadersSize) 
		private byte[] buffer = new byte[bufferSize];
        private UploadedFile uploadedFile = null;
        private string controlID = null;
		private Stream outputStream = null;
		private Stream fileStream = null;
		MemoryStream preloadedEntityBodyStream = null;		
		// Init preloadedEntityBody to a 0-length array in case an error occurs before we call 
		// preloadedEntityBodyStream.ToArray().  If init it to null instead we'll get a NullReferenceException
		private byte[] preloadedEntityBody = new byte[0]; 
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
			if (valPos < 0) 
            {
			    nameEqual = ";" + attrName + "=";
			    valPos = header.IndexOf(nameEqual, colonPos+1);
            }
            if (valPos < 0)
            {
                nameEqual = "\t" + attrName + "=";
                valPos = header.IndexOf(nameEqual, colonPos + 1);
            }
            if (valPos < 0)
                return null;
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
			if (UploadState != null && UploadState.Status == UploadStatus.Cancelled)
			{
				throw new HttpException(204, "Upload cancelled by user");
			}

            double secsToWait = 0;
            if (UploadState != null && Config.Current.MaxUploadRate > 0
                && UploadState.TimeElapsed != TimeSpan.MinValue)
            {
                double desiredSecs 
                	= ((double)UploadState.BytesRead) / Config.Current.MaxUploadRate;
                secsToWait = Math.Max(0, desiredSecs - UploadState.TimeElapsed.TotalSeconds);
            }

            // NOTE: if secsToWait = 0, this will simply yield to other threads so that the progress bar 
            // has a chance to update.
            System.Threading.Thread.Sleep((int)(1000 * secsToWait));

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
				if (MultiRequestControlID == null && UploadState != null)
				{
					UploadState.BytesRead += bytesRead;
				}

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
			int bytesParsed = parsePos - readPos;
            outputStream.Write(buffer, readPos, bytesParsed);
			if (outputStream == fileStream && UploadState != null)
			{
				UploadState.FileBytesRead += bytesParsed;
				if (MultiRequestControlID != null)
				{
					UploadState.BytesRead += bytesParsed;
				}
			}
			readPos = parsePos;
			
/*
			if (log.IsDebugEnabled) 
			{
				log.DebugFormat("preloadedEntityBodyStream.Length = {0}, UploadHttpModule.MaxNormalRequestLength = {1}",
							preloadedEntityBodyStream.Length, UploadHttpModule.MaxNormalRequestLength);
			}
*/
			// If the entire request or the non-file portion of the request is too large, throw an exception.
			if (this.grandTotalBytesRead > UploadHttpModule.MaxRequestLength)
			{
				if (log.IsDebugEnabled) log.Debug("Request Entity Too Large");
				throw new UploadTooLargeException(UploadHttpModule.MaxRequestLength, this.grandTotalBytesRead);
			}
			if (preloadedEntityBodyStream.Length > UploadHttpModule.MaxNormalRequestLength )
			{
				if (log.IsDebugEnabled) log.Debug("Nonfile Portion of Request Entity Too Large");
				throw new NonfilePortionTooLargeException(UploadHttpModule.MaxNormalRequestLength, preloadedEntityBodyStream.Length);
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

		internal void ParseMultipart()
		{
			if (isParsed)
			{
				return;
			}
			isParsed = true;
			try
			{
                bool readEntireRequest = ParseOrThrow();
                if (!readEntireRequest)
                {
                    // Wait 5 seconds to see if the user cancelled the request.
                    System.Threading.Thread.Sleep(5000);
                    // Setting the status to Failed will force a MergeAndSave which
                    // will change the status to Cancelled if the user cancelled
                    // the request.
                    UploadState.Status = UploadStatus.Failed;
                    // If the user did cancel the request, then stop all further
                    // processing of the request so that no exceptions are logged.
                    if (UploadState.Status == UploadStatus.Cancelled)
                    {
                        // Make sure that all the files associated
                        // with a cancelled multi-request upload get disposed.
                        if (MultiRequestControlID != null)
                        {
                            RegisterFilesForDisposal(MultiRequestControlID);
                            UploadStorage.DisposeAtEndOfRequest(uploadedFile);
                        }
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                        return;
                    }
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
                        throw new HttpException(400, String.Format("Data length ({0}) is shorter than Content-Length ({1}) and client is still connected after {2} secs.",
                                                                    grandTotalBytesRead, origContentLength, Math.Round(UploadState.TimeElapsed.TotalSeconds)));
                    }
                    else
                    {
                        throw new HttpException(400, String.Format("Client disconnected after receiving {0} of {1} bytes in {2} secs -- user probably cancelled upload.",
                                                                    grandTotalBytesRead, origContentLength, Math.Round(UploadState.TimeElapsed.TotalSeconds)));
                    }
                }
			}
			catch (Exception ex)
			{
				if (UploadState != null)
				{
					// We need to remember the exception here because the 
					// FormsAuthenticationHttpModule in ASP.NET 1.1 will eat any exception we throw and
					// the UploadHttpModule's RememberError handler will not get called.
					this.Exception = ex;
					if (ex is UploadException)
					{
						UploadState.Rejection = (UploadException)ex;
						UploadState.Status = UploadStatus.Rejected;
						// Wait 5 seconds to give the client a chance to stop the request.  If the client
						// stops the request, the user will see the original form instead of an error page.
						// Regardless, the progress display will show the error so the user knows what went wrong.
						System.Threading.Thread.Sleep(5000);
					}
					else if (UploadState.Status != UploadStatus.Cancelled)
					{
						UploadState.Failure = ex;
						UploadState.Status = UploadStatus.Failed;
					}

                    // If an error occurs during the upload of one file during
                    // a multi-request upload, make sure that all the files associated
                    // with that upload get disposed.
                    if (MultiRequestControlID != null)
                    {
                        RegisterFilesForDisposal(MultiRequestControlID);
                        UploadStorage.DisposeAtEndOfRequest(uploadedFile);
                    }
				}
					
				try
				{
					byte[] buffer = new byte[4096];
					while (0 < OrigWorker.ReadEntityBody(buffer, buffer.Length))
						; // Ignore the remaining body
				}
				catch (Exception)
				{
					// Ignore any errors that occur in the process.
				}

				log.Error("Rethrowing exception", ex);
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
			}
		}
		
		private Stream LogEntityBodyStream = null;
		private StreamWriter LogEntityBodySizesStream = null;
		private bool ParseOrThrow()
		{			
			origPreloadedBody = OrigWorker.GetPreloadedEntityBody();
			string contentTypeHeader = OrigWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentType);
			string contentLengthHeader = OrigWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderContentLength);
			string transferEncodingHeader = OrigWorker.GetKnownRequestHeader(HttpWorkerRequest.HeaderTransferEncoding);
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
			
			FieldNameTranslator translator = new FieldNameTranslator();
			if (MultiRequestControlID == null && UploadState != null)
			{
				UploadState.BytesTotal += origContentLength;
			}
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
			else
			{
				ContentEncoding = HttpContext.Current.Response.ContentEncoding;
			}
			preloadedEntityBodyStream = new MemoryStream();
			Hashtable storageConfigStreamTable = new Hashtable();
			Stream postBackIDStream = null;
			outputStream = preloadedEntityBodyStream;
			readPos = writePos = parsePos = 0;
			while (CopyUntilBoundary())
			{
				// If we were writing to a file, close it
				if (outputStream == fileStream && outputStream != null)
				{
                    UploadState.Files.Add(controlID, uploadedFile);
                    outputStream.Close();
				}
				
				// If we were receiving the value generated by the HiddenPostBackID control, set the postback ID.
				if (postBackIDStream != null)
				{
					postBackIDStream.Seek(0, System.IO.SeekOrigin.Begin);
					StreamReader sr = new System.IO.StreamReader(postBackIDStream);
					translator.PostBackID = sr.ReadToEnd();
					postBackIDStream = null;
				}

				// parse the headers
				string name = null, fileName = null, contentType = null;
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
						name = GetAttribute(header, "name");
						fileName = GetAttribute(header, "filename");
					}
					else if (String.Compare(headerName, "Content-Type", true) == 0)
					{
						contentType = header.Substring(colonPos + 1).Trim();
					}
				}
				if (log.IsDebugEnabled) log.Debug("name = " + name);
				if (log.IsDebugEnabled) log.Debug("fileName = " + fileName);
				if (log.IsDebugEnabled) log.DebugFormat("name = " + name);
				if (log.IsDebugEnabled) log.DebugFormat("fileName = " + fileName);
				controlID = null;
				if (name == Config.Current.PostBackIDQueryParam && postBackIDStream == null)
				{
					postBackIDStream = outputStream = new System.IO.MemoryStream();
					readPos = parsePos; // Skip past the boundary and headers
				}
				else if (name != null
				    && null != (controlID = translator.ConfigFieldNameToControlID(name)))
				{
					storageConfigStreamTable[controlID] = outputStream = new System.IO.MemoryStream();
					readPos = parsePos; // Skip past the boundary and headers
				}
				else if (name != null
				         && null != (controlID = translator.FileFieldNameToControlID(name)))
				{
					if (log.IsDebugEnabled) log.DebugFormat("name != null && controlID != null");
					if (UploadState == null)
					{
						UploadState = UploadStateStore.OpenReadWriteOrCreate(translator.FileFieldNameToPostBackID(name));
						if (transferEncodingHeader != "chunked")
							UploadState.Status = UploadStatus.NormalInProgress;
						else
							UploadState.Status = UploadStatus.ChunkedInProgress;
                        UploadState.BytesTotal += origContentLength;
                        UploadState.BytesRead += grandTotalBytesRead;
					}

					UploadStorageConfig storageConfig = null;
					
					if (UploadState.MultiRequestObject != null)
					{
						string secureStorageConfigString = UploadState.MultiRequestObject as string;
						if (secureStorageConfigString != null)
						{
							storageConfig = UploadStorage.CreateUploadStorageConfig();
							storageConfig.Unprotect(secureStorageConfigString);
							if (log.IsDebugEnabled) log.DebugFormat("storageConfig[tempDirectory]={0}", storageConfig["tempDirectory"]);
						}
					}
					string configID = translator.FileIDToConfigID(controlID);
					MemoryStream storageConfigStream = storageConfigStreamTable[configID] as MemoryStream;
					if (storageConfigStream != null)
					{
						storageConfigStream.Seek(0, System.IO.SeekOrigin.Begin);
						StreamReader sr = new System.IO.StreamReader(storageConfigStream);
						string secureStorageConfigString = sr.ReadToEnd();
						if (log.IsDebugEnabled)
						{
							log.Debug("storageConfigStream = " + secureStorageConfigString);
						}
						storageConfig = UploadStorage.CreateUploadStorageConfig();
						storageConfig.Unprotect(secureStorageConfigString);
						
						// Write out a part for the config hidden field
						if (log.IsDebugEnabled) log.DebugFormat("Calling WriteReplacementFormField({0}, {1})", configID, secureStorageConfigString);
						WriteReplacementFormField(configID, secureStorageConfigString);
						// Remove the stream from the table, so we don't write the replacement field again.
						storageConfigStreamTable.Remove(configID);
					}
					
					if (fileName != null)
					{
						if (log.IsDebugEnabled) log.DebugFormat("filename != null");
						if (log.IsDebugEnabled) log.Debug("Calling UploadContext.Current.CreateUploadedFile(" + controlID + "...)");
						UploadContext tempUploadContext = new UploadContext();
						tempUploadContext._ContentLength = origContentLength;
						uploadedFile 
							= UploadStorage.CreateUploadedFile(tempUploadContext, controlID, fileName, contentType, storageConfig);
                        if (MultiRequestControlID == null)
                            UploadStorage.DisposeAtEndOfRequest(uploadedFile);
						outputStream = fileStream = uploadedFile.CreateStream();
                        UploadState.CurrentFileName = uploadedFile.FileName;
                        readPos = parsePos; // Skip past the boundary and headers
	
						// If the client-specified content length is too large, we set the status to
						// RejectedRequestTooLarge so that progress displays will stop.  We do this after 
						// having created the UploadedFile because that is necessary for the progress display
						// to find the uploadContext.
						if (origContentLength > UploadHttpModule.MaxRequestLength)
						{
							if (log.IsDebugEnabled) log.Debug("contentLength > MaxRequestLength");
							throw new UploadTooLargeException(UploadHttpModule.MaxRequestLength, origContentLength);
						}
	
						// Write out a replacement part that just contains the filename as the value.
						WriteReplacementFormField(controlID, fileName);
					}
					else
					{
						if (log.IsDebugEnabled) log.DebugFormat("filename == null");
						// Since filename==null this must just be a hidden field with a name that
						// looks like a file field.  It is just an indication that when this request
						// ends, the associated uploaded files should be disposed.
						if (MultiRequestControlID == null)
						{
                            if (log.IsDebugEnabled) log.DebugFormat("MultiRequestControlID == null");
							RegisterFilesForDisposal(controlID);
						}
						outputStream = preloadedEntityBodyStream;
					}
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
                return false;
			}
            return true;
		}

		internal void RegisterFilesForDisposal(string controlUniqueID)
		{
			foreach (UploadedFile f in UploadState.Files)
			{
				if (log.IsDebugEnabled) log.DebugFormat("Checking {0} == {1}", f.ControlUniqueID, controlUniqueID);
				if (f.ControlUniqueID == controlUniqueID)
				{
					if (log.IsDebugEnabled) log.DebugFormat("DisposeAtEndOfRequest({0})", f);
					UploadStorage.DisposeAtEndOfRequest(f);
				}
			}
		}

		private void WriteReplacementFormField(string name, string val)
		{
			preloadedEntityBodyStream.Write(boundary, 0, boundary.Length);
			System.Text.StringBuilder replacementPart = new System.Text.StringBuilder();
			replacementPart.Append("\r\nContent-Disposition: form-data; name=\"" + name + "\"\r\n\r\n");
			replacementPart.Append(val);
			byte[] replacementPartBytes = System.Text.Encoding.ASCII.GetBytes(replacementPart.ToString());
			preloadedEntityBodyStream.Write(replacementPartBytes, 0, replacementPartBytes.Length);
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

