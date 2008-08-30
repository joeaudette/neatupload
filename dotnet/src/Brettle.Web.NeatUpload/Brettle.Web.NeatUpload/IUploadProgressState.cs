/*
 
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2008  Dean Brettle

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

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// An object representing the progress of an upload.  This object can be passed to 
	/// <see cref="UploadModule.BindProgressState"/> to get the current progress of
	/// the upload.
	/// </summary>
	public interface IUploadProgressState
	{
		/// <summary>
		/// The status of the upload as a member of the <see cref="UploadStatus"/>
		/// enumeration.
		/// </summary>
		/// <value>
		/// The status of the upload as a member of the <see cref="UploadStatus"/>
		/// enumeration.
		/// </value>
		UploadStatus Status { get; set; }

		/// <summary>
		/// The number of file bytes received for the upload.
		/// </summary>
		/// <value>
		/// The number of file bytes received for the upload.
		/// </value>
		/// <remarks>Only bytes that are part of file fields that the module processes
		/// are included in this count.</remarks>
		long FileBytesRead { get; set; }

		/// <summary>
		/// The total number of bytes received for the upload so far.
		/// </summary>
		/// <value>
		/// The total number of bytes received for the upload so far.
		/// </value>
		/// <remarks>This includes <see cref="FileBytesRead"/> as well as all other
		/// bytes included the upload (e.g. other form fields or file fields that are
		/// not processed by the module.</remarks>
		long BytesRead { get; set; }

		/// <summary>
		/// The total number of bytes expected for the uploaded.
		/// </summary>
		/// <value>
		/// The total number of bytes expected for the uploaded.
		/// </value>
		/// <remarks>For non-async uploads, this is determined by the Content-Length 
		/// header of the request.  For async-uploads, it is determined by the total
		/// of the sizes in the file sizes field in the pre-async-upload request.  It
		/// may go up when the final regular form submission request is received, since
		/// it's Content-Length header will then be added.</remarks>
		long BytesTotal { get; set; }

		/// <summary>
		/// The fraction (between 0.0 and 1.0) of the upload that has been received.
		/// </summary>
		/// <value>
		/// The fraction (between 0.0 and 1.0) of the upload that has been received.
		/// </value>
		/// <remarks>Computed as <see cref="BytesRead"/>/<see cref="BytesTotal"/></remarks>
		double FractionComplete { get; set; }

		/// <summary>
		/// An estimate of the number of bytes received during the past second.
		/// </summary>
		/// <value>
		/// An estimate of the number of bytes received during the past second.
		/// </value>
		int BytesPerSec { get; set; }

		/// <summary>
		/// If an <see cref="UploadException"/> (or subclass) was thrown while
		/// processing the upload, that exception.  Otherwise, null.
		/// </summary>
		/// <value>
		/// If an <see cref="UploadException"/> (or subclass) was thrown while
		/// processing the upload, that exception.  Otherwise, null.
		/// </value>
		UploadException Rejection { get; set; }

		/// <summary>
		/// If an exception that was not an <see cref="UploadException"/> 
		/// while processing the upload, that exception.  Otherwise, null.
		/// </summary>
		/// <value>
		/// If an exception that was not an <see cref="UploadException"/> 
		/// while processing the upload, that exception.  Otherwise, null.
		/// </value>
		Exception Failure { get; set; }

		/// <summary>
		/// An estimate of the time remaining until the upload is complete.
		/// </summary>
		/// <value>
		/// An estimate of the time remaining until the upload is complete.
		/// </value>
		TimeSpan TimeRemaining { get; set; }

		/// <summary>
		/// The time since the upload started.
		/// </summary>
		/// <value>
		/// The time since the upload started.
		/// </value>
		TimeSpan TimeElapsed { get; set; }

		/// <summary>
		/// The client-side filename (not including path) of the file currently
		/// being received.
		/// </summary>
		/// <value>
		/// The client-side filename (not including path) of the file currently
		/// being received.
		/// </value>
		string CurrentFileName { get; set; }

		/// <summary>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the upload.
		/// </summary>
		/// <value>
		/// A collection of the <see cref="UploadedFile"/> objects associated with the
		/// the upload.
		/// </value>
		UploadedFileCollection Files { get; set; }

		/// <summary>
		/// A serializable object representing the processing state for the upload request.
		/// </summary>
		/// <value>
		/// A serializable object representing the processing state for the upload request.
		/// </value>
		/// <remarks>This provides a way to communicate from request processing the upload
		/// to the request displaying progress which might be on different servers in
		/// a web garden/farm.</remarks>
		object ProcessingState { get; set; }
	}
}
