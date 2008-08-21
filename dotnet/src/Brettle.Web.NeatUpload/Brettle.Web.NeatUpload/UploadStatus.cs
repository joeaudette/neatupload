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
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// The status of an upload.
	/// </summary>
	public enum UploadStatus : long
	{
		/// <summary>
		/// Either the upload is unknown (e.g. an invalid post-back ID) or
		/// hasn't started to be received yet.
		/// </summary>
		Unknown,

		/// <summary>
		/// The upload is still being received and the request had a Content-Length
		/// header.
		/// </summary>
		NormalInProgress,

		/// <summary>
		/// The upload is still being received and the request did not have a Content-Length
		/// header and has a Transfer-Encoding of "chunked".
		/// </summary>
		ChunkedInProgress, 
		
		/// <summary>
		/// The entire upload has been received but the request is still being
		/// processed (e.g. the upload page code-behind is still executing).
		/// </summary>
		ProcessingInProgress, 

		/// <summary>
		/// <see cref="UploadModule.SetProcessingState"/> was called for this upload and
		/// the request has completed.
		/// </summary>
		ProcessingCompleted, 

		/// <summary>
		/// <see cref="UploadModule.SetProcessingState"/> was not called for this upload
		/// and the request has completed.
		/// </summary>
		Completed,

		/// <summary>
		/// The upload was cancelled by calling <see cref="UploadModule.CancelPostBack"/>
		/// </summary>
		Cancelled, 

		/// <summary>
		/// An <see cref="UploadException"/> (or subclass) was thrown while receiving or
		/// processing the upload.
		/// </summary>
		Rejected,

		/// <summary>
		/// An exception that is not an <see cref="UploadException"/> was thrown while 
		/// receiving or processing the upload.
		/// </summary>
		Failed
	}
}
