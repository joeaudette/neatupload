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

function NeatUploadMultiFileCreate(clientID, appPath, uploadScript, progressBar)
{
	NeatUploadMultiFile.prototype.Controls[clientID] 
		= new NeatUploadMultiFile(clientID, appPath, uploadScript, progressBar);
	return NeatUploadMultiFile.prototype.Controls[clientID];
}

function NeatUploadMultiFile(clientID, appPath, uploadScript, progressBar)
{
	// Only use SWFUpload in non-Mozilla browsers because bugs in the Firefox Flash 9 plugin cause it to
	// crash the browser on Linux and send IE cookies on Windows.  
	// TODO: Workaround cookies issue.
	if (navigator.plugins && navigator.mimeTypes && navigator.mimeTypes.length) 
		return null;
	this.ClientID = clientID;
	this.AppPath = appPath;
	this.UploadScript = uploadScript;
	this.ProgressBar = progressBar;
	var numf = this;
	window.onload = function() {
		numf.Swfu = new SWFUpload({
				flash_path : numf.AppPath + '/NeatUpload/SWFUpload.swf',
				upload_script : numf.UploadScript,
				allowed_filesize: 2097151,
				upload_file_start_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].DisplayProgress',
				upload_file_queued_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].FileQueued',
				flash_loaded_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].FlashLoaded'
		});
	};	
}

NeatUploadMultiFile.prototype.Controls = new Object();

NeatUploadMultiFile.prototype.DisplayProgress = function () {
	// If no bar was specified, use the first one.
	if (!this.ProgressBar)
	{
		this.ProgressBar = NeatUploadPB.prototype.FirstBarID;
	}
	if (this.ProgressBar)
	{
		NeatUploadPB.prototype.Bars[this.ProgressBar].Display();
	}
};

NeatUploadMultiFile.prototype.FlashLoaded = function () {
	// TODO: Hookup the upload trigger.
	// Make clicking 'Browse...' on the <input type='file'> call SWFUpload.browse().
	var inputFile = document.getElementById(this.ClientID);
	var swfUpload = this.Swfu;
	inputFile.onclick = function() {
		swfUpload.browse();
		window.event.returnValue = false;
		return false;
	};
	
	swfUpload.flashLoaded(true);
};

NeatUploadMultiFile.prototype.FileQueued = function (file) {
	var inputFile = document.getElementById(this.ClientID);
	var span = document.createElement('span');
	span.innerHTML = file.name + '<br/>';
	inputFile.parentNode.insertBefore(span, inputFile);
};
