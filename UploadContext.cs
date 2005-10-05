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
using System.Web;
using System.Collections;

namespace Brettle.Web.NeatUpload
{
	internal class UploadContext
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		internal const string ContextItemKeyPrefix = "NeatUpload.UploadedFile-";
				
		// All NeatUpload InputFile controls will have name attributes starting with the following prefix
		// This will appear in form field values, query strings, and window names.
		// Note: Must not include a "." because it confuses IE's javascript) and must not include a "-" 
		// because we use that separate the postback id from the control id.
		internal const string NamePrefix = "NeatUpload_";

		internal static UploadContext Current
		{
			get {
				
				FilteringWorkerRequest worker = UploadHttpModule.GetCurrentWorkerRequest() as FilteringWorkerRequest;
				if (worker == null)
					return null;
				return worker.GetUploadContext();
			}
		}
		
		internal static UploadContext FindByID(string postBackID)
		{
			UploadContext uploadContext = HttpContext.Current.Application[postBackID] as UploadContext;
			if (log.IsDebugEnabled) log.Debug("Application[" + postBackID + "] = " + uploadContext);
			return uploadContext;
		}
		
		internal UploadContext()
		{
			this.startTime = System.DateTime.Now;
		}
		
		private Hashtable uploadedFiles = Hashtable.Synchronized(new Hashtable());

		private string postBackID;
		
		internal string PostBackID
		{
			get { lock(this) { return postBackID; } }
			set { lock(this) { postBackID = value; } }
		}
				
		internal UploadedFile GetUploadedFile(string controlUniqueID)
		{
			if (log.IsDebugEnabled) log.Debug("In GetUploadedFile() controlUniqueID=" + controlUniqueID);
			return uploadedFiles[controlUniqueID] as UploadedFile; 
		}
		
		internal UploadedFile CreateUploadedFile(string fileID, string fileName, string contentType)
		{
			// Get the control's unique ID from the fileID
			int dashIndex = fileID.IndexOf('-');
			string controlUniqueID = fileID.Substring(dashIndex + 1);
			if (log.IsDebugEnabled) log.Debug("In CreateUploadedFile() controlUniqueID=" + controlUniqueID);
			UploadedFile uploadedFile = new UploadedFile(controlUniqueID, fileName, contentType);			
			uploadedFiles[controlUniqueID] = uploadedFile;
			
			// Set the PostBackID from the fileID
			PostBackID = fileID.Substring(0, dashIndex);
			
			// Add a reference to this UploadContext to the Application so that it can be accessed
			// by the ProgressBar in a separate request.
			if (log.IsDebugEnabled) log.Debug("Storing UploadContext in Application[" + PostBackID + "]");
			HttpContext.Current.Application[PostBackID] = this;
			
			return uploadedFile;
		}
		
		internal void RemoveUploadedFiles()
		{
			if (log.IsDebugEnabled) log.Debug("In RemoveUploadedFiles");
			lock (uploadedFiles.SyncRoot)
			{
				foreach (UploadedFile f in uploadedFiles.Values)
				{
					f.Dispose();
				}
				// Don't clear the Hashtable, because we use it to determine the number of files uploaded in the
				// last postback.
				// uploadedFiles.Clear();
			}
		}
		
		internal int NumUploadedFiles
		{
			get 
			{
				int numUploadedFiles = 0;
				lock(uploadedFiles.SyncRoot) 
				{
					foreach (UploadedFile f in uploadedFiles.Values)
					{
						if (f.IsUploaded)
						{
							numUploadedFiles++;
						}
					}
				}
				return numUploadedFiles;
			}
		}	
		
		internal double PercentComplete
		{
			get 
			{
				lock (this)
				{
					if (ContentLength == 0)
					{
						return 0;
					}
					return BytesRead * 100.0 / ContentLength; 
				}
			}
		}
		
		private DateTime startTime;
		internal DateTime StartTime
		{
			get { lock(this) { return startTime; } }
			set { lock(this) { startTime = value; } }
		}
		
		private int bytesRead;
		internal int BytesRead
		{
			get { lock(this) { return bytesRead; } }
			set { lock(this) { bytesRead = value; } }
		}

		private int contentLength;
		internal int ContentLength
		{
			get { lock(this) { return contentLength; } }
			set { lock(this) { contentLength = value; } }
		}
		
		private UploadStatus status = UploadStatus.InProgress;
		internal UploadStatus Status
		{
			get { lock(this) { return status; } }
			set { lock(this) { status = value; } }
		}
 
		internal TimeSpan TimeRemaining
		{
			get {
				lock(this) 
				{
					if (BytesRead == 0)
					{
						return TimeSpan.MaxValue;
					}
					else
					{
						return new TimeSpan((ContentLength - BytesRead) * (DateTime.Now - StartTime).Ticks / BytesRead);
					}
				}
			}
		}

	}
}
