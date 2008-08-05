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

/* **********************************
    Debug Console -
    
    based on mmSWFUpload Debug Console which has the following license:
 
	mmSWFUpload 1.0: Flash upload dialog - http://profandesign.se/swfupload/
	SWFUpload is (c) 2006-2007 Lars Huring, Olov Nilzén and Mammon Media and is released under the MIT License:
	http://www.opensource.org/licenses/mit-license.php
 
    The debug console is a self contained, in page location
    for debug message to be sent.  The Debug Console adds
    itself to the body if necessary.

    The console is automatically scrolled as messages appear.
   ********************************** */

NeatUploadConsole = {};
NeatUploadConsole.debug_enabled = true;
NeatUploadConsole.InitialMessage = "";
NeatUploadConsole.debugMessage = function (message) {
    var exception_message, exception_values;
	
    if (this.debug_enabled) {
        if (typeof(message) === "object" && typeof(message.name) === "string" && typeof(message.message) === "string") {
            exception_message = "";
            exception_values = [];
            for (var key in message) {
                exception_values.push(key + ": " + message[key]);
            }
            exception_message = exception_values.join("\n");
            exception_values = exception_message.split("\n");
            exception_message = "EXCEPTION: " + exception_values.join("\nEXCEPTION: ");
            NeatUploadConsole.writeLine(exception_message);
        } else {
            NeatUploadConsole.writeLine(message);
        }
    }
};

NeatUploadConsole.writeLine = function (message) {
	var console = this.Console;
	if (console) {
		console.value += message + "\n";
		console.scrollTop = console.scrollHeight - console.clientHeight;
	} else {
		this.InitialMessage += message + "\n";
	}
}

NeatUploadConsole.open = function(message) { 
	var consoleWindow = window.open("", "NeatUploadConsole", "height=400,width=750,location=no,menubar=no,status=no", false);
	consoleWindow.onunload = function () {
		var nuc = window.opener.NeatUploadConsole;
		if (!nuc) {
			return;
		}
		nuc.Console = null;
	};
	var console = consoleWindow.document.getElementById("NeatUploadConsole");
	if (!console) {
		documentForm = consoleWindow.document.createElement("form");
		consoleWindow.document.getElementsByTagName("body")[0].appendChild(documentForm);

		console = consoleWindow.document.createElement("textarea");
		console.id = "NeatUploadConsole";
		console.style.fontFamily = "monospace";
		console.setAttribute("wrap", "off");
		console.wrap = "off";
		console.style.overflow = "auto";
		console.style.width = "700px";
		console.style.height = "350px";
		console.style.margin = "5px";
		console.value = "";
		documentForm.appendChild(console);
	}
	this.Console = console;
	if (this.InitialMessage != "")
		this.debugMessage(this.InitialMessage);
	this.InitialMessage = "";
	this.debugMessage(message);
};

// Have SWFUpload use the same console
if (typeof(SWFUpload) != "undefined" && SWFUpload && SWFUpload.prototype)
{
	SWFUpload.prototype.debugMessage = NeatUploadConsole.debugMessage;
}

function NeatUploadCloneInputFile (inputFile)
{
	var newInputFile = document.createElement('input');
	for (var a=0; a < inputFile.attributes.length; a++)
	{
		var attr = inputFile.attributes.item(a); 
		var attrName = attr.name.toLowerCase();
		// For some unknown reason IE7 (and perhaps other browsers) always set attr.specified = false for the
		// "name" attribute if it is set from script (e.g. during an earlier call to this function).  As a 
		// result, we only skip unspecified attributes other than "name".
		if (attrName != 'name' && ! attr.specified)
			continue;
		if (attrName != 'type' && attrName != 'value')
		{
			if (attrName == 'style' && newInputFile.style && inputFile.style && inputFile.style.cssText)
				newInputFile.style.cssText = inputFile.style.cssText;
			else if (attrName == 'class') // Needed for IE because 'class' is a JS keyword
				newInputFile.className = attr.value;
			else if (attrName == 'for') // Needed for IE because 'for' is a JS keyword
				newInputFile.htmlFor = attr.value;
			else
				newInputFile.setAttribute(attr.name, attr.value);
		}
	}
	newInputFile.onchange = inputFile.onchange;
	newInputFile.setAttribute('type', 'file');
	return newInputFile;
}

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
	this.GetFileSizesCallbacks = new Array();
	
	// Add a hook to call our own unload handler(s) which do things like restore the original on submit handlers
	this.OnUnloadHandlers = new Array();	
	var origOnUnload = window.onunload;
	this.OnUnloadHandlers.push(function () { window.onunload = origOnUnload; });
	window.onunload = this.CombineHandlers(window.onunload, function() { return f.OnUnload(); });

	// Override the form.submit() method to call our own handlers before and after.
	this.debugMessage("overriding form.submit()");
	f.FormElem.NeatUpload_OnSubmittingHandlers = new Array();
	f.FormElem.NeatUpload_OnSubmitting = this.OnSubmitting;
	this.OnUnloadHandlers.push(function() 
	{
		f.FormElem.NeatUpload_OnSubmittingHandlers = null;
		f.FormElem.NeatUpload_OnSubmitting = null;
	});
	f.FormElem.NeatUpload_OnSubmitHandlers = new Array();
	f.FormElem.NeatUpload_OrigSubmit = f.FormElem.submit;
	f.FormElem.NeatUpload_OnSubmit = this.OnSubmit;
	try
	{
		f.FormElem.submit = function () {
			f.debugMessage("In submit()");
			f.FormElem.NeatUpload_OnSubmitting();
			var status = f.FormElem.NeatUpload_OnSubmit();
			if (status)
				f.FormElem.NeatUpload_OrigSubmit();
			f.debugMessage("Leaving submit()");
		};
		this.OnUnloadHandlers.push(function() 
		{
			f.FormElem.submit = f.FormElem.NeatUpload_OrigSubmit;
			f.FormElem.NeatUpload_OnSubmitHandlers = null;
			f.FormElem.NeatUpload_OnSubmit = null;
		});
	}
	catch (ex)
	{
		// We can't override the submit method.  That means NeatUpload won't work 
		// when the form is submitted programmatically.  This occurs in Mac IE.
		this.debugMessage("can't override form.submit()");
	}			


	// Hook preventDefault() so we know whether it was called to prevent the upload
	try
	{
		Event.prototype.NeatUpload_OrigPreventDefault = Event.prototype.preventDefault;
		Event.prototype.preventDefault = function () {
			this.NeatUpload_PreventDefaultCalled = true;
			return this.NeatUpload_OrigPreventDefault();
		};
		this.debugMessage("Hooked preventDefault");
	}
	catch (ex)
	{
		this.debugMessage("Could not hook preventDefault: " + ex);
	}
	
	// This next bit of code needs to run after any other JS has set onsubmit or added any onsubmit handlers,
	// so we do it after a short delay after window.onload has fired
	this.debugMessage("hooking form.onsubmit()");
	this.AddHandler(window, "load", function ()	{
		window.setTimeout(function () {
			// Hook form.onsubmit so that we know whether it returned false (which would prevent the upload)
			f.FormElem.NeatUpload_OrigOnSubmit = f.FormElem.onsubmit;
			if (f.FormElem.NeatUpload_OrigOnSubmit)
			{
			    f.OnUnloadHandlers.push(function () { f.FormElem.onsubmit = f.FormElem.NeatUpload_OrigOnSubmit; });
			    f.FormElem.onsubmit = function (oev)
			    {
				    ev = NeatUploadForm.prototype.ClickEvent || oev || window.event;
				    var returnValue = this.NeatUpload_OrigOnSubmit(ev);
				    if (ev)
					    ev.NeatUpload_OrigOnSubmitReturnValue = returnValue;
				    return returnValue;
			    }
		    }
			// Add our own onsubmit handler (which will hopefully be the last one) so that it can check whether
			// another onsubmit handler prevented the upload
			f.AddHandler(f.FormElem, "submit", function (oev) {
				f.debugMessage("Checking whether another onsubmit handler prevented the upload");
				ev = NeatUploadForm.prototype.ClickEvent || oev || window.event;
				if (typeof(ev.returnValue) != "undefined" && !ev.returnValue)
				{
				    // In Opera window.event.returnValue is always false and can't be changed.
				    // Detect that case and ignore it.
				    ev.returnValue = true; // Try to change it
				    if (ev.returnValue) // If it changed, change it back and return false.
				    {
				        ev.returnValue = false;
				        return false;
				    }
				}
				if (typeof(ev.NeatUpload_OrigOnSubmitReturnValue) != "undefined" && !ev.NeatUpload_OrigOnSubmitReturnValue)
					return false;
				if (ev.NeatUpload_PreventDefaultCalled)
					return false;
				// asp:ScriptManager moves form.onsubmit into an event handler and sets form.onsubmit=null.
				// That means that we can't know the value that the orign form.onsubmit returned.  As a 
				// workaround, we check Page_IsValid which validators will set.
				if (typeof(Page_IsValid) != "undefined" && !Page_IsValid) 
				    return false;
				f.debugMessage("Calling NeatUpload_OnSubmit");
				var retVal = f.FormElem.NeatUpload_OnSubmit(ev);
				f.debugMessage("returned " + retVal);
				return retVal;
			});
		}, 1);
	});

	// Note when an event that could trigger a postback occurs so that we can check whether it is a trigger.	
	this.debugMessage("Adding handlers for possible triggers");
	var eventsThatCouldTriggerPostBack = ['click', 'keypress', 'drop', 'mousedown', 'keydown'];
	for (var i = 0; i < eventsThatCouldTriggerPostBack.length; i++)
	{
		var eventName = eventsThatCouldTriggerPostBack[i];
		this.AddHandler(f.FormElem, eventName, function (ev) {
			ev = NeatUploadForm.prototype.ClickEvent || ev || window.event;
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
			if (f.GetSubmittingElem())
				f.FormElem.NeatUpload_OnSubmitting();
			
			return true;
		}, true);
	}

	// Add a hidden field at the beginning of the form that will be used to pass the sizes of all files to be
	// uploaded (-1 for each file where that can't be determined)
	var fileSizesField = document.createElement("input");
	fileSizesField.type = "hidden"
	fileSizesField.name = "NeatUploadFileSizes";
	fileSizesField.value = "";
	f.FormElem.insertBefore(fileSizesField, f.FormElem.firstChild);
	f.FileSizesField = fileSizesField;

	this.debugMessage("Adding submitting handler");	
	this.AddSubmittingHandler(function () {
		f.FileSizesField.value = f.GetFileSizes().join(" ");
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
	this.debugMessage("Submitting handler added");	
}

NeatUploadForm.prototype.debugMessage = NeatUploadConsole.debugMessage;

NeatUpload_LastEventSource = null;
NeatUpload_LastEventType = null;
NeatUploadForm.prototype.EventData = new Object();

NeatUploadForm.prototype.GetSubmittingElem = function()
{
	var src = NeatUpload_LastEventSource;
	var evType = NeatUpload_LastEventType;
	if (evType != 'click') // Note: pressing enter or space on a button causes a click event.
	{
		return null;
	}
	var tagName = src.tagName;
	if (!tagName)
	{
		return null;
	}
	tagName = tagName.toLowerCase();
	if (tagName == 'input' || tagName == 'button')
	{
		var inputType = src.getAttribute('type');
		if (inputType) inputType = inputType.toLowerCase();
		if (!inputType || inputType == 'submit' || inputType == 'image')
		{
			return src;
		}
	}
	return null;
}

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
	NeatUploadForm.prototype.HookClick(id);
};

NeatUploadForm.prototype.HookClick = function (id)
{
    var elem = document.getElementById(id);
    if (!elem)
    {
        return;
    }
    // If a user event (e.g. pressing enter) causes elem.click() to be called, the source of the
    // original event hides the element for which click() was called (in Firefox).  As a result, we 
    // don't know whether to start the progress bar.  To avoid that, we hook the click() method so 
    // that we can note the element it was called on.
    if (!elem.click)
    {
        return;
    }
    elem.NeatUpload_OrigClick = elem.click;
    elem.click = function() {
        NeatUploadForm.prototype.ClickEvent = { target: this, type: "click" };
        var retVal;
        try {
            retVal = this.NeatUpload_OrigClick();
        }
        finally { 
            NeatUploadForm.prototype.ClickEvent = null;
        }
        return retVal;
    };
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

NeatUploadForm.prototype.AddSubmitHandler = function(handler)
{
	var elem = this.FormElem;
	elem.NeatUpload_OnSubmitHandlers.unshift(handler);
};

NeatUploadForm.prototype.AddSubmittingHandler = function(handler)
{
	var elem = this.FormElem;
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

NeatUploadForm.FINISH_HANDLERS_BUT_DONT_SUBMIT = "finish handlers but don't submit";

NeatUploadForm.prototype.OnSubmit = function(ev)
{
	// To avoid having OnSubmit() run twice for the same click
	// (once from our form.submit() and again from our onsubmit handler),
	// we set a flag to note that we've already called it, and add a timer event 
	// that will reset it once all other pending events are processed.
	var formElem = this;
	if (formElem.NeatUpload_OnSubmitCalled)
		return false;
	formElem.NeatUpload_OnSubmitCalled = true;
	window.setTimeout(function () { formElem.NeatUpload_OnSubmitCalled = false; }, 1);

	var retVal = true;
	for (var i=0; i < this.NeatUpload_OnSubmitHandlers.length; i++)
	{
		var status = this.NeatUpload_OnSubmitHandlers[i].call(this, ev);
		if (status === true)
			continue;
		if (status === false)
			return false;
		else if (status === NeatUploadForm.FINISH_HANDLERS_BUT_DONT_SUBMIT)
			retVal = false;
	}
	return retVal;
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

NeatUploadForm.prototype.AddGetFileSizesCallback = function(callback)
{
	this.GetFileSizesCallbacks.push(callback);
};

NeatUploadForm.prototype.GetFileSizes = function(elem)
{
	var fileSizes = [];
	for (var i=0; i < this.GetFileSizesCallbacks.length; i++)
	{
		fileSizes = fileSizes.concat(this.GetFileSizesCallbacks[i].call(elem));
	}

	var inputElems = this.FormElem.getElementsByTagName("input");
	for (i = 0; i < inputElems.length; i++)
	{
		var inputElem = inputElems.item(i);
		if (inputElem && inputElem.type && inputElem.type.toLowerCase() == "file")
		{
			if (inputElem.value && inputElem.value.length > 0)
			{
				fileSizes.push(-1);

				// If the browser really is IE on Windows, return false if the path is not absolute because
				// IE will not actually submit the form if any file value is not an absolute path.  If IE doesn't
				// submit the form, any progress bars we start will never finish.  
				if (navigator && navigator.userAgent)
				{
					var ua = navigator.userAgent.toLowerCase();
					var msiePosition = ua.indexOf('msie');
					if (msiePosition != -1 && typeof(ActiveXObject) != 'undefined' && ua.indexOf('mac') == -1
					    && ua.charAt(msiePosition + 5) < 8)
					{
						var re = new RegExp('^(\\\\\\\\[^\\\\]|([a-zA-Z]:)?\\\\).*');
						var match = re.exec(inputElem.value);
						if (match == null || match[0] == '')
						{
							if (typeof(NeatUpload_HandleIE6InvalidPath) != 'undefined'
							    && NeatUpload_HandleIE6InvalidPath != null)
								NeatUpload_HandleIE6InvalidPath(inputElem);
							return [];
						}
					}
				}
			}
		}
	}
	return fileSizes;
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
	if (!formElem.NeatUpload_NUForm)
	{
		formElem.NeatUpload_NUForm = new NeatUploadForm(formElem, postBackID);
		formElem.NeatUpload_NUForm.debugMessage("Constructor returned");
	}
	formElem.NeatUpload_NUForm.debugMessage("GetFor returning");
	return formElem.NeatUpload_NUForm;
};

function NeatUploadPB(id, postBackID, uploadProgressPath, popupWidth, popupHeight, triggerIDs, autoStartCondition)
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
	this.debugMessage("GetFor returned");
	
	var fallbackLink = document.getElementById(id + '_fallback_link');
	if (fallbackLink)
		fallbackLink.setAttribute('href', 'javascript:' + popupDisplayStatement);
	this.TriggerIDs = new Object();
	this.TriggerIDs.NeatUpload_length = 0;
	this.AutoStartCondition = autoStartCondition;

	this.UploadForm.AddNonuploadHandler(function () { pb.ClearFileInputs(pb.UploadForm.FormElem); });

	this.UploadForm.AddSubmitHandler(function (ev) {
		pb.debugMessage("In NeatUploadPB SubmitHandler");
		// If there are files to upload and either no trigger controls were specified for this progress bar or
		// a specified trigger control was triggered, then start the progress display.
		if (pb.EvaluateAutoStartCondition()
			&& (!pb.TriggerIDs.NeatUpload_length
			    || NeatUploadForm.prototype.IsElemWithin(NeatUpload_LastEventSource, pb.TriggerIDs)))
		{
			pb.debugMessage("Calling pb.Display()");
			pb.Display();
		}
		return true;
	});
						
	for (var i = 0; i < triggerIDs.length; i++)
	{
		this.UploadForm.AddTrigger(triggerIDs[i]);
		this.TriggerIDs[triggerIDs[i]] = ++this.TriggerIDs.NeatUpload_length;
	}
	this.debugMessage("NeatUploadPB returning");
}

NeatUploadPB.prototype.debugMessage = NeatUploadConsole.debugMessage;

NeatUploadPB.prototype.Bars = new Object();

NeatUploadPB.prototype.Display = function() {
	this.DisplayUrl(this.UploadProgressPath + '&postBackID=' + this.UploadForm.GetPostBackID() + '&refresher=client&canScript=true&canCancel=' + NeatUploadPB.prototype.CanCancel());
};

NeatUploadPB.prototype.DisplayUrl = function (progressUrl) {
	var pb = this;
	window.open(progressUrl,
		pb.UploadForm.GetPostBackID(), 'width=' + pb.PopupWidth + ',height=' + pb.PopupHeight
		+ ',directories=no,location=no,menubar=no,resizable=yes,scrollbars=auto,status=no,toolbar=no');
};
	
NeatUploadPB.prototype.EvalOnClose = "window.close();";
	
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
	var isFilesToUpload = (this.UploadForm.GetFileSizes().length > 0);
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
				var newInputFile = NeatUploadCloneInputFile(inputFile);
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
		var name = inputFile.getAttribute('name');
		name = name.replace(/^[^-]+/, 'NeatUpload_' + nuf.GetPostBackID())
		inputFile.setAttribute('name', name);
	});
}


NeatUploadInputFile.prototype.Controls = new Object();


/* ******************************************************************************************* */
/* NeatUploadMultiFile - JS support for NeatUpload's MultiFile control
/* ******************************************************************************************* */

function NeatUploadMultiFileCreate(clientID, postBackID, appPath, uploadScript, postBackIDQueryParam, uploadParams,
									useFlashIfAvailable, fileQueueControlID, 
									flashFilterExtensions, flashFilterDescription)
{
	NeatUploadMultiFile.prototype.Controls[clientID] 
		= new NeatUploadMultiFile(clientID, postBackID, appPath, uploadScript, postBackIDQueryParam, uploadParams,
									useFlashIfAvailable, fileQueueControlID, 
									flashFilterExtensions, flashFilterDescription);
	return NeatUploadMultiFile.prototype.Controls[clientID];
}

function NeatUploadMultiFile(clientID, postBackID, appPath, uploadScript, postBackIDQueryParam, uploadParams,
							useFlashIfAvailable, fileQueueControlID, 
							flashFilterExtensions, flashFilterDescription)
{
	var numf = this;
	this.ClientID = clientID;
	this.AppPath = appPath;
	this.PostBackIDQueryParam = postBackIDQueryParam;
	this.UploadScript = uploadScript;
	this.UploadParams = uploadParams;
	this.FilesToUpload = [];
	this.FileID = 0;
	this.FileQueueControlID = fileQueueControlID;

	// If no Flash, the following onchange handler will make it appear that multiple files can be selected from
	// one file input by just repeated clicking Browse... and selecting a file.
	// In reality, each time a file is selected, the file input is hidden and a new empty clone is created to
	// take its place.
	document.getElementById(numf.ClientID).onchange = function(ev) {
	    if (numf.IsFlashLoaded && numf.Swfu)
	    {
		    return true;
        }
		var newInputFile = NeatUploadCloneInputFile(this);
		this.removeAttribute("id");
		this.parentNode.insertBefore(newInputFile, this.nextSibling);
		this.style.display = 'none';
		this.style.position = 'relative';
		FileQueued({ name: this.value, size: -1, inputFileElem: this, id: numf.FileID++});		
        return true;
	};	

	// Use the latest postback ID when the form is submitted.
	var nuf = NeatUploadForm.prototype.GetFor(document.getElementById(this.ClientID), postBackID);
	nuf.AddSubmittingHandler(function () {
		var inputFile = document.getElementById(numf.ClientID);
		var oldName = inputFile.getAttribute('name');
		var newName = oldName.replace(/^[^-]+/, 'NeatUpload_' + nuf.GetPostBackID());
		for (var n = inputFile.parentNode.firstChild; n; n = n.nextSibling)
		{
			if (n.tagName && n.tagName.toLowerCase() == "input" 
				&& n.getAttribute && n.getAttribute('type') == "file" 
				&& n.getAttribute('name') == oldName)
			{
				n.setAttribute('name', newName);
			}
		}
	    if (numf.IsFlashLoaded && numf.Swfu)
	    {
			numf.UploadParams[numf.PostBackIDQueryParam] = nuf.GetPostBackID();
			numf.Swfu.setUploadParams(numf.UploadParams);
			numf.Swfu.updateUploadStrings();
		}
	});

	// Insert a default file queue control immediately before the input file control
	this.fqc = document.createElement('div');
	document.getElementById(this.ClientID).parentNode.insertBefore(this.fqc, document.getElementById(this.ClientID));
	
	// If the browser supports opacity and the div after the input file control has children,
	// then use a variant of McGrady's technique to make the input file control look like those children.
	StyleInputFile(numf.ClientID); 
	
	// Don't use SWFUpload if Flash support wasn't requested
	if (!useFlashIfAvailable)
		return;
	
	nuf.AddHandler(window, "load", function ()	{
		numf.Swfu = new SWFUpload({
				debug : numf.debug_enabled,
				flash_url : numf.AppPath + '/NeatUpload/SWFUpload.swf',
				upload_target_url : numf.UploadScript,
				upload_params : numf.UploadParams,
				file_size_limit: 2097151,
				begin_upload_on_queue : false,
				file_queued_handler : FileQueued,
				file_cancelled_handler : FileCancelled,
				queue_stopped_handler : QueueCancelled,
				queue_complete_handler : QueueCompleted,
				flash_ready_handler : FlashReady,
				file_types : flashFilterExtensions,
				file_types_description : flashFilterDescription
			});
	});
	
	// Hookup the upload trigger.
	nuf.AddSubmitHandler(function (ev) {
	    if (numf.IsFlashLoaded && numf.Swfu && numf.FilesToUpload.length > 0)
	    {
	    	numf.UploadParams["FileSizes"] = nuf.GetFileSizes().join(" ");
			numf.Swfu.setUploadParams(numf.UploadParams);
			numf.Swfu.updateUploadStrings();
		    numf.Swfu.startUpload();
		    numf.SubmittingElem = null;
	    	// Don't submit the form yet.  We do that after SWFUpload finishes uploading.
		    ev = ev || window.event;
		    if (ev)
		    {
		    	ev.returnValue = false;
		    	if (ev.preventDefault)
				    ev.preventDefault();
				// Remember the button that was clicked to submit the form so we can fake it
				// when we submit the form with the original submit().
				numf.SubmittingElem = nuf.GetSubmittingElem();
			}
			return NeatUploadForm.FINISH_HANDLERS_BUT_DONT_SUBMIT;
		}
		return true;
	});
	
	// Hookup the non-upload handler.
	nuf.AddNonuploadHandler(function () {
	    if (numf.IsFlashLoaded && numf.Swfu)
	    {
    		numf.Swfu.cancelQueue();
    	}
	});

	// Add the GetFileSizes callback.
	nuf.AddGetFileSizesCallback(function () {
	    if (numf.IsFlashLoaded && numf.Swfu)
	    {
	    	var fileSizes = [];
	    	for (var i = 0; i < numf.FilesToUpload.length; i++)
	    	{
	    		fileSizes[i] = numf.FilesToUpload[i].size;
	    	}
    		return fileSizes;
    	}
    	else
    	{
    		return []; // NeatUploadForm code handles all <input type=file> elements we might have added
    	}
	});

	// Make clicking 'Browse...' on the <input type='file'> call SWFUpload.browse().
	document.getElementById(numf.ClientID).onclick = function(ev) {
	    if (numf.IsFlashLoaded && numf.Swfu)
	    {
		    numf.Swfu.browse();
		    ev = ev || window.event;
		    ev.returnValue = false;
		    if (ev.preventDefault)
		    {
			    ev.preventDefault();
		    }
		    return false;
        }
        else
        {
            return true;
        }
	};
	
	/* PRIVATE FUNCTIONS */	
	
	function FlashReady()
	{ 
		numf.IsFlashLoaded = true;
		// Change the <input type='file'> to an innocuous <input type='button'> so that
		// clicking on it doesn't bring up an extra file selection dialog.
		var inputFileElem = document.getElementById(numf.ClientID);
		inputFileElem.type = 'button';
		inputFileElem.value = 'Pick Files...';
	}
	
	function QueueCompleted()
	{
		numf.debugMessage("QueueCompleted(): calling NeatUpload_OrigSubmit().");
		// If the form was submitted via a submit button, we need to fake that it was
		// pressed when submitting the form.  We do this be creating a hidden form
		// field with the same name and value.
		if (numf.SubmittingElem)
		{
			var submitHiddenField = document.createElement("input");
			submitHiddenField.type = "hidden"
			submitHiddenField.name = numf.SubmittingElem.name;
			var val = numf.SubmittingElem.value;
			if (typeof(val) == "undefined" || val == null || val == "")
				val = submitHiddenField.name;
			submitHiddenField.value = val;
			numf.SubmittingElem.parentNode.insertBefore(submitHiddenField, numf.SubmittingElem);
			// Remove the field after giving tiem for the original submit() to be called so it
			// doesn't stick around if the user stops the upload.
			window.setTimeout(function() {
				numf.SubmittingElem.parentNode.removeChild(submitHiddenField);
			}, 1);
		}
		nuf.FormElem.NeatUpload_OrigSubmit();
	}
	
	function FileQueued(file) {
		numf.FilesToUpload.push(file);
		file.Delete = function() {
		    if (numf.IsFlashLoaded && numf.Swfu)
		    {
				numf.Swfu.cancelUpload(this.id);
			}
			else
			{
				this.inputFileElem.parentNode.removeChild(this.inputFileElem);
				FileCancelled(this);
			}
		}
		numf.OnFileQueued(file);
	}

	function QueueCancelled(file) {
		numf.FilesToUpload = [];
	}

	function FileCancelled(file) {
		var i, fileIndex = -1;
		for (i = 0; i < numf.FilesToUpload.length; i++)
		{
			if (numf.FilesToUpload[i].id == file.id)
			{
				fileIndex = i;
				break;
			}
		}
		if (fileIndex == -1)
		{
			numf.debugMessage("WARN: FileCancelled can not find file: ");
			numf.debugMessage(file);
			return;
		}
		numf.FilesToUpload.splice(fileIndex, 1);
	}
	
	function StyleInputFile(clientID)
	{
		var inputFile = document.getElementById(clientID);
		var replacementDiv = inputFile.nextSibling;
		if (!replacementDiv || !replacementDiv.tagName || replacementDiv.tagName.toLowerCase() != "div" 
			|| !replacementDiv.firstChild)
			return;
		replacementDiv.style.display = "block";
		replacementDiv.style.height = replacementDiv.offsetHeight + "px";
		var w = 0;
		for (var n = replacementDiv.firstChild; n; n = n.nextSibling)
		{
			w = ((n.offsetLeft + n.offsetWidth > w) ? (n.offsetLeft + n.offsetWidth) : w);
		}	
		replacementDiv.style.width = w + "px";
		replacementDiv.style.overflow = "hidden";
		inputFile.style.display = "none";
		inputFile.style.position = "absolute";
		inputFile.style.textAlign = "right";
		inputFile.style.top = 0;
		inputFile.style.right = 0;
		inputFile.style.cursor = "pointer";
		var fontHeight = replacementDiv.offsetHeight;
		var fontWidth = w / 3;
		var fontSize = (fontHeight > fontWidth ? fontHeight : fontWidth);
		inputFile.style.fontSize = fontSize + "px";
		inputFile.style.filter = "alpha(opacity=0)";
		inputFile.style.opacity = 0;
		inputFile.style.MozOpacity = 0;
		inputFile.style.zIndex = 2;
		replacementDiv.insertBefore(inputFile, replacementDiv.firstChild);
		inputFile.style.display = "block";
	}

}

NeatUploadMultiFile.prototype.debugMessage = NeatUploadConsole.debugMessage;

NeatUploadMultiFile.prototype.Controls = new Object();

/* Override OnFileQueued on your page to change how queued files are displayed. */
NeatUploadMultiFile.prototype.OnFileQueued = function (file) {
	var numf = this;

	var span = document.createElement('span');
	var link = document.createElement('a');
	link.setAttribute('href', '#');
	link.onclick = function () {
		file.Delete(); 
		span.parentNode.removeChild(span); 
		return false;
	};
	link.appendChild(document.createTextNode('X'));
	span.appendChild(link);
	span.appendChild(document.createTextNode(' ' + file.name));
	span.appendChild(document.createElement('br'));

	var fqc = numf.GetFileQueueControl();
	fqc.appendChild(span);
};

NeatUploadMultiFile.prototype.GetFileQueueControl = function()
{
	if (typeof(this.FileQueueControlID) == "string" && this.FileQueueControlID.length > 0)
	{
		this.fqc = document.getElementById(this.FileQueueControlID);
	}
	return this.fqc;
};


/***************************** Debug Settings **************************/
NeatUploadForm.prototype.debug_enabled = true;
NeatUploadPB.prototype.debug_enabled = true;
NeatUploadMultiFile.prototype.debug_enabled = true;
