/*
NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005-2007  Dean Brettle

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
if (!Array.prototype.push)
{
	Array.prototype.push = function() {
		for (var i = 0; i < arguments.length; i++)
			this[this.length] = arguments[i];
		return this.length;
	};
}

if (!Array.prototype.unshift)
{
	Array.prototype.unshift = function() {
		this.reverse();
		for (var i = 0; i < arguments.length; i++)
			this[this.length] = arguments[i];
		this.reverse();
		return this.length;
	};
}

if (!Function.prototype.call)
{
	Function.prototype.call = function() {
		var obj = arguments[0];
		obj._NeatUpload_tmpFunc = this;
		var argList = '';
		for (var i = 1; i < arguments.length; i++)
		{
			argList += 'arguments[' + i + ']';
			if (i < arguments.length - 1)
				argList += ',';
		}
		var result = eval('obj._NeatUpload_tmpFunc(' + argList + ')');
		obj._NeatUpload_tmpFunc = null;
		return result;
	};
}

function NeatUploadForm(formElem, postBackID)
{
	var f = this;
	this.PostBackID = postBackID;
	this.SubmitCount = 0;
	this.FormElem = formElem;
	this.TriggerIDs = new Object();
	this.TriggerIDs.NeatUpload_length = 0;
	this.OnNonuploadHandlers = new Array();
	this.GetFileCountCallbacks = new Array();
	
	// Add a hook to call our own unload handler(s) which do things like restore the original on submit handlers
	this.OnUnloadHandlers = new Array();	
	var origOnUnload = window.onunload;
	this.OnUnloadHandlers.push(function () { window.onunload = origOnUnload; });
	window.onunload = this.CombineHandlers(window.onunload, function() { return f.OnUnload(); });

	// Add a hook to call our own onsubmit handlers, but restore the original onsubmit handler during unload
	this.OnSubmitForm = function()
	{
		return formElem.NeatUpload_OnSubmit();
	}
	var origOnSubmit = formElem.onsubmit;
	formElem.onsubmit = this.CombineHandlers(formElem.onsubmit, this.OnSubmitForm);
	this.OnUnloadHandlers.push(function () { formElem.onsubmit = origOnSubmit; });

	var eventsThatCouldTriggerPostBack = ['click', 'keypress', 'drop', 'mousedown', 'keydown'];
							
	for (var i = 0; i < eventsThatCouldTriggerPostBack.length; i++)
	{
		var eventName = eventsThatCouldTriggerPostBack[i];
		this.AddHandler(f.FormElem, eventName, function (ev) {
			ev = ev || window.event;
			if (!ev)
			{
				return true;
			}
			var src = ev.srcElement || ev.target;
			if (!src)
			{
				return true;
			}
			NeatUpload_LastEventType = ev.type;
			NeatUpload_LastEventSource = src;
			NeatUploadForm.prototype.EventData = new Object();
			if (ev.type != 'click' && ev.type != 'keypress')
			{
				return true;
			}
			var tagName = src.tagName;
			if (!tagName)
			{
				return true;
			}
			tagName = tagName.toLowerCase();
			if (tagName == 'input' || tagName == 'button')
			{
				var inputType = src.getAttribute('type');
				if (inputType) inputType = inputType.toLowerCase();
				if (!inputType || inputType == 'submit' || inputType == 'image')
				{
					f.FormElem.NeatUpload_OnSubmitting();
				}
			}
			return true;
		}, true);
	}
	
	this.AddSubmittingHandler(function () {
		f.SubmitCount++;
		var url = f.FormElem.getAttribute('action');
		url = f.ChangePostBackIDInUrl(url, NeatUploadForm.prototype.PostBackIDQueryParam);
		f.FormElem.setAttribute('action', url);
		
		if (!NeatUpload_LastEventSource)
		{
			return;
		}
		if (NeatUploadForm.prototype.IsElemWithin(NeatUpload_LastEventSource, f.TriggerIDs))
		{
			return;
		}
		if (f.TriggerIDs.NeatUpload_length)
		{
			f.OnNonupload(f.FormElem);
		}
	});
}

NeatUpload_LastEventSource = null;
NeatUpload_LastEventType = null;
NeatUploadForm.prototype.EventData = new Object();

NeatUploadForm.prototype.ChangePostBackIDInUrl = function(url, queryParam)
{
		var qp = queryParam + '=';
		var postBackIDStart = url.indexOf('?' + qp);
		if (postBackIDStart == -1)
		{
			postBackIDStart = url.indexOf('&' + qp);
		}
		if (postBackIDStart == -1)
		{
			return url;
		}
		postBackIDStart += qp.length;
		var postBackIDEnd = url.indexOf('&', postBackIDStart);
		if (postBackIDEnd == -1)
		{
			postBackIDEnd = url.length;
		}
		url = url.substring(0, postBackIDStart) 
			+ this.GetPostBackID()
			+ url.substring(postBackIDEnd, url.length);
		return url;
}

NeatUploadForm.prototype.GetPostBackID = function()
{
	return this.PostBackID + this.SubmitCount.toString();
}
 
NeatUploadForm.prototype.IsElemWithin = function(elem, assocArray)
{
	while (elem)
	{
		if (elem.id && assocArray[elem.id])
		{
			return true;
		}
		elem = elem.parentNode;
	}
};

NeatUploadForm.prototype.AddTrigger = function (id)
{
	this.TriggerIDs[id] = ++this.TriggerIDs.NeatUpload_length;
};

NeatUploadForm.prototype.CombineHandlers = function(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined')
		return newHandler;
	return function(e) 
	{ 
		if (origHandler(e) == false)
			return false;
		return newHandler(e); 
	};
};

NeatUploadForm.prototype.AddHandler = function(elem, eventName, handler, useCapture)
{
	if (typeof(useCapture) == 'undefined')
		useCapture = false;
	if (elem.addEventListener)
	{
		elem.addEventListener(eventName, handler, useCapture);
		this.OnUnloadHandlers.push(function () { elem.removeEventListener(eventName, handler, useCapture); });
	}
	else if (elem.attachEvent)
	{
		elem.attachEvent("on" + eventName, handler);
		this.OnUnloadHandlers.push(function () { elem.detachEvent("on" + eventName, handler); });
	}
	else
	{
		var origHandler = elem["on" + eventName];
		elem["on" + eventName] = this.CombineHandlers(elem["on" + eventName], handler);
		this.OnUnloadHandlers.push(function () { elem["on" + eventName] = origHandler; });
	}
};

NeatUploadForm.prototype.AddSubmitHandler = function(prepend, handler)
{
	var elem = this.FormElem;
	if (!elem.NeatUpload_OnSubmitHandlers) 
	{
		elem.NeatUpload_OnSubmitHandlers = new Array();
		elem.NeatUpload_OrigSubmit = elem.submit;
		elem.NeatUpload_OnSubmit = this.OnSubmit;
		try
		{
			elem.submit = function () {
				elem.NeatUpload_OnSubmitting();
				elem.NeatUpload_OrigSubmit();
				elem.NeatUpload_OnSubmit();
			};
			this.OnUnloadHandlers.push(function() 
			{
				elem.submit = elem.NeatUpload_OrigSubmit;
				elem.NeatUpload_OnSubmitHandlers = null;
				elem.NeatUpload_OnSubmit = null;
			});
		}
		catch (ex)
		{
			// We can't override the submit method.  That means NeatUpload won't work 
			// when the form is submitted programmatically.  This occurs in Mac IE.
		}			
	}
	if (prepend)
	{
		elem.NeatUpload_OnSubmitHandlers.unshift(handler);
	}
	else
	{
		elem.NeatUpload_OnSubmitHandlers.push(handler);
	}	
};

NeatUploadForm.prototype.AddSubmittingHandler = function(handler)
{
	var elem = this.FormElem;
	if (!elem.NeatUpload_OnSubmittingHandlers) 
	{
		elem.NeatUpload_OnSubmittingHandlers = new Array();
		elem.NeatUpload_OnSubmitting = this.OnSubmitting;
		this.OnUnloadHandlers.push(function() 
		{
			elem.NeatUpload_OnSubmittingHandlers = null;
			elem.NeatUpload_OnSubmitting = null;
		});
	}
	elem.NeatUpload_OnSubmittingHandlers.push(handler);
};

NeatUploadForm.prototype.OnSubmitting = function()
{
	for (var i=0; i < this.NeatUpload_OnSubmittingHandlers.length; i++)
	{
		this.NeatUpload_OnSubmittingHandlers[i].call(this);
	}
	return true;
};

NeatUploadForm.prototype.OnSubmit = function()
{
	for (var i=0; i < this.NeatUpload_OnSubmitHandlers.length; i++)
	{
		if (!this.NeatUpload_OnSubmitHandlers[i].call(this))
		{
			return false;
		}
	}
	return true;
};

NeatUploadForm.prototype.OnUnload = function()
{
	for (var i=0; i < this.OnUnloadHandlers.length; i++)
	{
		this.OnUnloadHandlers[i].call(this);
	}
	return true;
};

NeatUploadForm.prototype.AddNonuploadHandler = function(handler)
{
	this.OnNonuploadHandlers.push(handler);
};

NeatUploadForm.prototype.OnNonupload = function(elem)
{
	// Other file controls (e.g. MultiFile) can use OnNonuploadHandlers to clear themselves.
	for (var i=0; i < this.OnNonuploadHandlers.length; i++)
	{
		this.OnNonuploadHandlers[i].call(elem);
	}
	return true;
};

NeatUploadForm.prototype.AddGetFileCountCallback = function(callback)
{
	this.GetFileCountCallbacks.push(callback);
};

NeatUploadForm.prototype.GetFileCount = function(elem)
{
	var fileCount = 0;
	for (var i=0; i < this.GetFileCountCallbacks.length; i++)
	{
		fileCount += this.GetFileCountCallbacks[i].call(elem);
	}

	var inputElems = this.FormElem.getElementsByTagName("input");
	for (i = 0; i < inputElems.length; i++)
	{
		var inputElem = inputElems.item(i);
		if (inputElem && inputElem.type && inputElem.type.toLowerCase() == "file")
		{
			if (inputElem.value && inputElem.value.length > 0)
			{
				fileCount++;

				// If the browser really is IE on Windows, return false if the path is not absolute because
				// IE will not actually submit the form if any file value is not an absolute path.  If IE doesn't
				// submit the form, any progress bars we start will never finish.  
				if (navigator && navigator.userAgent)
				{
					var ua = navigator.userAgent.toLowerCase();
					var msiePosition = ua.indexOf('msie');
					if (msiePosition != -1 && typeof(ActiveXObject) != 'undefined' && ua.indexOf('mac') == -1
					    && ua.charAt(msiePosition + 5) < 7)
					{
						var re = new RegExp('^(\\\\\\\\[^\\\\]|([a-zA-Z]:)?\\\\).*');
						var match = re.exec(inputElem.value);
						if (match == null || match[0] == '')
						{
							if (typeof(NeatUpload_HandleIE6InvalidPath) != 'undefined'
							    && NeatUpload_HandleIE6InvalidPath != null)
								NeatUpload_HandleIE6InvalidPath(inputElem);
							return 0;
						}
					}
				}
			}
		}
	}
	return fileCount;
};

NeatUploadForm.prototype.GetFor = function (elem, postBackID)
{
	var formElem = elem;
	while (formElem && formElem.tagName.toLowerCase() != "form")
	{
		formElem = formElem.parentNode;
	}
	if (!formElem)
	{
		return null;
	}
	if (!formElem.NeatUploadForm)
	{
		formElem.NeatUploadForm = new NeatUploadForm(formElem, postBackID);
	}
	return formElem.NeatUploadForm;
};

function NeatUploadPB(id, postBackID, uploadProgressPath, inline, popupWidth, popupHeight, triggerIDs, autoStartCondition)
{
	if (!document.getElementById)
		return;
	var pb = this;
	pb.ClientID = id;
	pb.UploadProgressPath = uploadProgressPath;
	pb.PopupWidth = popupWidth;
	pb.PopupHeight = popupHeight;
	if (!document.getElementById)
		return null;
	var elem = document.getElementById(id);
	if (!elem)
		elem = document.getElementById(id + '_NeatUpload_dummyspan');
	this.UploadForm = NeatUploadForm.prototype.GetFor(elem, postBackID);
		
	var displayFunc;
	if (inline)
	{
		displayFunc = function () {
								setTimeout( 
									function () {
										frames[pb.ClientID].location.href 
											= pb.UploadProgressPath + '&postBackID=' + pb.UploadForm.GetPostBackID() + '&refresher=client&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel();
										},
								0);
							};
		if (frames[pb.ClientID]) 
		{
			frames[pb.ClientID].location.replace(pb.UploadProgressPath + '&postBackID=' + pb.UploadForm.GetPostBackID() + '&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel());
		}
	}
	else
	{
		displayFunc = function () {
			window.open(pb.UploadProgressPath + '&postBackID=' + pb.UploadForm.GetPostBackID() + '&refresher=client&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel(),
				pb.UploadForm.GetPostBackID(), 'width=' + pb.PopupWidth + ',height=' + pb.PopupHeight
				+ ',directories=no,location=no,menubar=no,resizable=yes,scrollbars=auto,status=no,toolbar=no');
		}
	}
	
	var fallbackLink = document.getElementById(id + '_fallback_link');
	if (fallbackLink)
		fallbackLink.setAttribute('href', 'javascript:' + popupDisplayStatement);
	this.TriggerIDs = new Object();
	this.TriggerIDs.NeatUpload_length = 0;
	this.Display = displayFunc;
	this.AutoStartCondition = autoStartCondition;

	this.UploadForm.AddNonuploadHandler(function () { pb.ClearFileInputs(pb.UploadForm.FormElem); });

	this.UploadForm.AddSubmitHandler(!inline, function () {
		// If there are files to upload and either no trigger controls were specified for this progress bar or
		// a specified trigger control was triggered, then start the progress display.
		if (pb.EvaluateAutoStartCondition()
			&& (!pb.TriggerIDs.NeatUpload_length
			    || NeatUploadForm.prototype.IsElemWithin(NeatUpload_LastEventSource, pb.TriggerIDs)))
		{
			pb.Display();
		}
		return true;
	});
						
	for (var i = 0; i < triggerIDs.length; i++)
	{
		this.UploadForm.AddTrigger(triggerIDs[i]);
		this.TriggerIDs[triggerIDs[i]] = ++this.TriggerIDs.NeatUpload_length;
	}
}

NeatUploadPB.prototype.Bars = new Object();

NeatUploadPB.prototype.CanCancel = function()
{
	try
	{
		if (window.stop || window.document.execCommand)
			return true;
		else
			return false;
	}
	catch (ex)
	{
		return false;
	}
};

NeatUploadPB.prototype.EvaluateAutoStartCondition = function()
{
	with (this)
	{
		return eval(AutoStartCondition);
	}
};

NeatUploadPB.prototype.IsFilesToUpload = function()
{
	var isFilesToUpload = (this.UploadForm.GetFileCount() > 0);
	return isFilesToUpload; 
};

NeatUploadPB.prototype.ClearFileInputs = function(elem)
{
	var inputFiles = elem.getElementsByTagName('input');
	for (var i=0; i < inputFiles.length; i++ )
	{
		var inputFile = inputFiles.item(i);
		// NOTE: clearing (by removing and recreating) empty file inputs confuses IE6 when the document is
		// in both the top-level window and in an iframe.  ExpertTree uses such an iframe to do AJAX-style
		// callbacks.
		if (inputFile.type == 'file' && inputFile.value && inputFile.value.length > 0)
		{
			try
			{
				var newInputFile = document.createElement('input');
				for (var a=0; a < inputFile.attributes.length; a++)
				{
					var attr = inputFile.attributes.item(a); 
					if (! attr.specified)
						continue;
					var attrName = attr.name.toLowerCase();
					if (attrName != 'type' && attrName != 'value')
					{
						if (attrName == 'style' && newInputFile.style && newInputFile.style.cssText)
							newInputFile.style.cssText = attr.value;
						else if (attrName == 'class') // Needed for IE because 'class' is a JS keyword
							newInputFile.className = attr.value;
						else if (attrName == 'for') // Needed for IE because 'for' is a JS keyword
							newInputFile.htmlFor = attr.value;
						else
							newInputFile.setAttribute(attr.name, attr.value);
					}
				}
				newInputFile.setAttribute('type', 'file');
				inputFile.parentNode.replaceChild(newInputFile, inputFile);
			}
			catch (ex)
			{
				// I don't know of any other way to clear the file inputs, so on browser where we get an error
				// (eg Mac IE), we just give the user a warning.
				if (inputFile.value != null && inputFile.value != '')
				{
					if (!NeatUploadForm.prototype.EventData.NeatUploadPBAlertShown)
					{
						window.alert(this.ClearFileNamesAlert);
						NeatUploadForm.prototype.EventData.NeatUploadPBAlertShown = true;
					}
					return false;
				}
			}
		}
	}
	return true;
};

NeatUploadForm.prototype.EventData.NeatUploadPBAlertShown = false;

/* ******************************************************************************************* */
/* NeatUploadInputFile - JS support for NeatUpload's InputFile control
/* ******************************************************************************************* */

function NeatUploadInputFileCreate(clientID, postBackID)
{
	NeatUploadInputFile.prototype.Controls[clientID] 
		= new NeatUploadInputFile(clientID, postBackID);
	return NeatUploadInputFile.prototype.Controls[clientID];
}

function NeatUploadInputFile(clientID, postBackID)
{
	this.ClientID = clientID;
	var nuif = this;
	// Use the latest postback ID when the form is submitted.
	var nuf = NeatUploadForm.prototype.GetFor(document.getElementById(this.ClientID), postBackID);
	nuf.AddSubmittingHandler(function () {
		var inputFile = document.getElementById(nuif.ClientID);
		inputFile.setAttribute('name', 'NeatUpload_' + nuf.GetPostBackID() + '-' + inputFile.getAttribute('id'));
	});
}


NeatUploadInputFile.prototype.Controls = new Object();


/* ******************************************************************************************* */
/* NeatUploadMultiFile - JS support for NeatUpload's MultiFile control
/* ******************************************************************************************* */

function NeatUploadMultiFileCreate(clientID, postBackID, appPath, uploadScript)
{
	NeatUploadMultiFile.prototype.Controls[clientID] 
		= new NeatUploadMultiFile(clientID, postBackID, appPath, uploadScript);
	return NeatUploadMultiFile.prototype.Controls[clientID];
}

function NeatUploadMultiFile(clientID, postBackID, appPath, uploadScript)
{
	// Only use SWFUpload in non-Mozilla browsers because bugs in the Firefox Flash 9 plugin cause it to
	// crash the browser on Linux and send IE cookies on Windows.  
	// TODO: Workaround cookies issue.
	if (navigator.plugins && navigator.mimeTypes && navigator.mimeTypes.length) 
		return null;
	this.ClientID = clientID;
	this.AppPath = appPath;
	this.UploadScript = uploadScript;
	var numf = this;
	window.onload = function() {
		numf.Swfu = new SWFUpload({
				flash_path : numf.AppPath + '/NeatUpload/SWFUpload.swf',
				upload_script : numf.UploadScript,
				allowed_filesize: 2097151,
				upload_file_start_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].DisplayProgress',
				upload_file_queued_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].FileQueued',
				upload_file_cancel_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].FileCancelled',
				upload_queue_cancel_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].QueueCancelled',
				flash_loaded_callback : 'NeatUploadMultiFile.prototype.Controls["' + numf.ClientID + '"].FlashLoaded'
		});
	};
	
	// Use the latest postback ID when the form is submitted.
	var nuf = NeatUploadForm.prototype.GetFor(document.getElementById(this.ClientID), postBackID);
	nuf.AddSubmittingHandler(function () {
		var inputFile = document.getElementById(numf.ClientID);
		inputFile.setAttribute('name', 'NeatUpload_' + nuf.GetPostBackID() + '-' + inputFile.getAttribute('id'));
	});
}


NeatUploadMultiFile.prototype.Controls = new Object();

NeatUploadMultiFile.prototype.DisplayProgress = function () {
	// TODO: This shouldn't be necessary once the UploadContext survives across upload requests.
	if (NeatUploadPB.prototype.FirstBarID)
	{
		NeatUploadPB.prototype.Bars[NeatUploadPB.prototype.FirstBarID].Display();
	}
};

NeatUploadMultiFile.prototype.FlashLoaded = function () {
	var numf = this;
	var swfUpload = this.Swfu;
	var inputFile = document.getElementById(this.ClientID);
	numf.NumAsyncFilesField = inputFile.nextSibling;
	var nuf = NeatUploadForm.prototype.GetFor(inputFile);

	// Hookup the upload trigger.
	nuf.AddSubmitHandler(true, function () {
		swfUpload.upload();
		return true;
	});
	
	// Hookup the non-upload handler.
	nuf.AddNonuploadHandler(function () {
		swfUpload.cancelQueue();			
	});

	// Add the GetFileCount callback.
	nuf.AddGetFileCountCallback(function () {
		return numf.NumAsyncFilesField.value;
	});

	// Make clicking 'Browse...' on the <input type='file'> call SWFUpload.browse().
	inputFile.onclick = function() {
		swfUpload.browse();
		if (window.event)
			window.event.returnValue = false;
		return false;
	};
	
	swfUpload.flashLoaded(true);
};

NeatUploadMultiFile.prototype.FileQueued = function (file) {
	this.NumAsyncFilesField.value++;
	var inputFile = document.getElementById(this.ClientID);
	var span = document.createElement('span');
	span.innerHTML = file.name + '<br/>';
	inputFile.parentNode.insertBefore(span, inputFile);
};

NeatUploadMultiFile.prototype.QueueCancelled = function (file) {
	this.NumAsyncFilesField.value = 0;
};

NeatUploadMultiFile.prototype.FileCancelled = function (file) {
	this.NumAsyncFilesField.value = this.NumAsyncFilesField.value - 1;
};
