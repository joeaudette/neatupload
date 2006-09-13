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

function NeatUploadPB(id, popupDisplayStatement, inline, displayFunc, triggerIDs, autoStartCondition)
{
	if (!document.getElementById)
		return;
	var pb = this;

	this.OnUnloadHandlers = new Array();
	var origOnUnload = window.onunload;
	this.OnUnloadHandlers.push(function () { window.onunload = origOnUnload; });
	window.onunload = this.CombineHandlers(window.onunload, function() { return pb.OnUnload(); });

	var elem = document.getElementById(id);
	if (!elem)
		elem = document.getElementById(id + '_NeatUpload_dummyspan');
	var formElem = elem;
	while (formElem && formElem.tagName.toLowerCase() != "form")
	{
		formElem = formElem.parentNode;
	}
	this.OnSubmitForm = function()
	{
		return formElem.NeatUpload_OnSubmit();
	}
	var origOnSubmit = formElem.onsubmit;
	this.OnUnloadHandlers.push(function () { formElem.onsubmit = origOnSubmit; });
	formElem.onsubmit = this.CombineHandlers(formElem.onsubmit, this.OnSubmitForm);
	
	var fallbackLink = document.getElementById(id + '_fallback_link');
	if (fallbackLink)
		fallbackLink.setAttribute('href', 'javascript:' + popupDisplayStatement);
	this.TriggerIDs = new Object();
	this.TriggerIDs.length = 0;
	this.Display = displayFunc;
	this.FormElem = formElem;
	this.AutoStartCondition = autoStartCondition;
	this.AddSubmitHandler(pb.FormElem, !inline, function () {
		// If trigger controls were specified for this progress bar and the trigger is not 
		// specified for *any* progress bar, then clear the filenames.
		if (NeatUpload_LastEventSource
		    && pb.TriggerIDs.length
		    && !pb.IsElemWithin(NeatUpload_LastEventSource, NeatUpload_TriggerIDs))
		{
			return pb.ClearFileInputs(pb.FormElem);
		}
		// If there are files to upload and either no trigger controls were specified for this progress bar or
		// a specified trigger control was triggered, then start the progress display.
		if (pb.EvaluateAutoStartCondition()
			&& (!pb.TriggerIDs.length
			    || pb.IsElemWithin(NeatUpload_LastEventSource, pb.TriggerIDs)))
		{
			pb.Display();
		}
		return true;
	});
						
	var eventsThatCouldTriggerPostBack = ['click', 'keypress', 'drop', 'mousedown', 'keydown'];
							
	for (var i = 0; i < eventsThatCouldTriggerPostBack.length; i++)
	{
		var eventName = eventsThatCouldTriggerPostBack[i];
		this.AddHandler(pb.FormElem, eventName, function (ev) {
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
			NeatUpload_AlertShown = false;
			if (ev.type != 'click' && ev.type != 'keypress')
			{
				return true;
			}
			if (pb.IsElemWithin(src, NeatUpload_TriggerIDs)
			      || NeatUpload_TriggerIDs.NeatUpload_length == 0)
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
					pb.ClearFileInputs(pb.FormElem);
				}
			}
			return true;
		}, true);
	}

	this.AddSubmittingHandler(pb.FormElem, function () {
		if (!NeatUpload_LastEventSource)
		{
			return;
		}
		if (pb.IsElemWithin(NeatUpload_LastEventSource, NeatUpload_TriggerIDs))
		{
			return;
		}
		if (NeatUpload_TriggerIDs.NeatUpload_length)
		{
			pb.ClearFileInputs(pb.FormElem);
		}
	});

	for (var i = 0; i < triggerIDs.length; i++)
	{
		NeatUpload_TriggerIDs[triggerIDs[i]] = ++NeatUpload_TriggerIDs.NeatUpload_length;
		this.TriggerIDs[triggerIDs[i]] = ++this.TriggerIDs.length;
	}
}

NeatUploadPB.prototype.Bars = new Object();

NeatUploadPB.prototype.IsElemWithin = function(elem, assocArray)
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
}

NeatUploadPB.prototype.CombineHandlers = function(origHandler, newHandler) 
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

NeatUploadPB.prototype.AddHandler = function(elem, eventName, handler, useCapture)
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

NeatUploadPB.prototype.EvaluateAutoStartCondition = function()
{
	with (this)
	{
		return eval(AutoStartCondition);
	}
}

NeatUploadPB.prototype.IsFilesToUpload = function()
{
	var formElem = this.FormElem;
	while (formElem && formElem.tagName.toLowerCase() != "form")
	{
		formElem = formElem.parentNode;
	}
	if (!formElem) 
	{
		return false;
	}
	var inputElems = formElem.getElementsByTagName("input");
	var foundFileInput = false;
	var isFilesToUpload = false;
	for (i = 0; i < inputElems.length; i++)
	{
		var inputElem = inputElems.item(i);
		if (inputElem && inputElem.type && inputElem.type.toLowerCase() == "file")
		{
			foundFileInput = true;
			if (inputElem.value && inputElem.value.length > 0)
			{
				isFilesToUpload = true;

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
							return false;
						}
					}
				}
			}
		}
	}
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
					if (attr.specified && attr.name != 'type' && attr.name != 'value')
					{
						if (attr.name == 'style' && newInputFile.style && newInputFile.style.cssText)
							newInputFile.style.cssText = attr.value;
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
					if (!NeatUpload_AlertShown)
					{
						window.alert(this.ClearFileNamesAlert);
						NeatUpload_AlertShown = true;
					}
					return false;
				}
			}
		}
	}
	return true;
};

NeatUploadPB.prototype.AddSubmitHandler = function(elem, isPopup, handler)
{
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
	if (isPopup)
	{
		elem.NeatUpload_OnSubmitHandlers.unshift(handler);
	}
	else
	{
		elem.NeatUpload_OnSubmitHandlers.push(handler);
	}	
};

NeatUploadPB.prototype.AddSubmittingHandler = function(elem, handler)
{
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

NeatUploadPB.prototype.OnSubmitting = function()
{
	for (var i=0; i < this.NeatUpload_OnSubmittingHandlers.length; i++)
	{
		this.NeatUpload_OnSubmittingHandlers[i].call(this);
	}
	return true;
};

NeatUploadPB.prototype.OnSubmit = function()
{
	for (var i=0; i < this.NeatUpload_OnSubmitHandlers.length; i++)
	{
		if (!this.NeatUpload_OnSubmitHandlers[i].call(this))
		{
			return false;
		}
	}
	return true;
}	

NeatUploadPB.prototype.OnUnload = function()
{
	for (var i=0; i < this.OnUnloadHandlers.length; i++)
	{
		this.OnUnloadHandlers[i].call(this);
	}
	return true;
}	

NeatUpload_TriggerIDs = new Object();
NeatUpload_TriggerIDs.NeatUpload_length = 0;
NeatUpload_LastEventSource = null;
NeatUpload_LastEventType = null;
NeatUpload_AlertShown = true;



