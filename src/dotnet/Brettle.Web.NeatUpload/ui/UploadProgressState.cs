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

namespace Brettle.Web.NeatUpload.Internal.UI
{	
	internal class UploadProgressState : IUploadProgressState
	{
		internal UploadProgressState()
		{
		}

		private UploadStatus _Status = UploadStatus.Unknown;
		UploadStatus IUploadProgressState.Status {
			get { return _Status; } 
			set { _Status = value; } 
		}

		private long _BytesRead;
		long IUploadProgressState.BytesRead {
			get { return _BytesRead; }
			set { _BytesRead = value; }
		}

		private long _FileBytesRead;
		long IUploadProgressState.FileBytesRead {
			get { return _FileBytesRead; }
			set { _FileBytesRead = value; }
		}

		private long _BytesTotal;
		long IUploadProgressState.BytesTotal {
			get { return _BytesTotal; }
			set { _BytesTotal = value; }
		}

		private double _FractionComplete;
		double IUploadProgressState.FractionComplete {
			get { return _FractionComplete; }
			set { _FractionComplete = value; }
		}

		private int _BytesPerSec;
		int IUploadProgressState.BytesPerSec {
			get { return _BytesPerSec; }
			set { _BytesPerSec = value; }
		}

		private UploadException _Rejection;
		UploadException IUploadProgressState.Rejection {
			get { return _Rejection; }
			set { _Rejection = value; }
		}

		private Exception _Failure;
		Exception IUploadProgressState.Failure {
			get { return _Failure; }
			set { _Failure = value; }
		}

		private TimeSpan _TimeRemaining;
		TimeSpan IUploadProgressState.TimeRemaining {
			get { return _TimeRemaining; }
			set { _TimeRemaining = value; }
		}

		private TimeSpan _TimeElapsed;
		TimeSpan IUploadProgressState.TimeElapsed {
			get { return _TimeElapsed; }
			set { _TimeElapsed = value; }
		}

		private string _CurrentFileName;
		string IUploadProgressState.CurrentFileName {
			get { return _CurrentFileName; }
			set { _CurrentFileName = value; }
		}

		private object _ProcessingState;
		object IUploadProgressState.ProcessingState {
			get { return _ProcessingState; }
			set { _ProcessingState = value; }
		}
	}
}
