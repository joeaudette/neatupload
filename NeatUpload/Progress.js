function NeatUploadGetMainWindow() 
{
	var mainWindow;
	if (window.opener) 
		mainWindow = window.opener;
	else 
		mainWindow = window.parent;
	return mainWindow;
};

function NeatUploadCancel() 
{
	var mainWindow = NeatUploadGetMainWindow();
	if (mainWindow && mainWindow.stop)
		mainWindow.stop();
	else if (mainWindow && mainWindow.document && mainWindow.document.execCommand)
		mainWindow.document.execCommand('Stop');
}

function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { origHandler(e); newHandler(e); };
}

function NeatUploadCanCancel()
{
	var mainWindow = NeatUploadGetMainWindow();
	try
	{
		return (mainWindow.stop || mainWindow.document.execCommand);
	}
	catch (ex)
	{
		return false;
	}
}

function NeatUploadRemoveCancelLink()
{
	NeatUploadLinkNode = document.getElementById('cancelLink');
	if (NeatUploadLinkNode) 
		NeatUploadLinkNode.parentNode.removeChild(NeatUploadLinkNode);
}

NeatUploadReq = null;
function NeatUploadRefreshWithAjax(url) 
{
	NeatUploadReq = null;
	var req = null;
	try
	{
		req = new ActiveXObject('Microsoft.XMLHTTP');
	}
	catch (ex)
	{
		req = null;
	}
	if (!req && typeof(XMLHttpRequest) != 'undefined')
	{
		req = new XMLHttpRequest();
	}
	if (req)
	{
		NeatUploadReq = req;
	}
	if (NeatUploadReq)
	{
		NeatUploadReq.onreadystatechange = NeatUploadUpdateHtml;
		NeatUploadReq.open('GET', url);
		NeatUploadReq.send(null);
	}
	else
	{
		return false;
	}
	return true;
}

function NeatUploadUpdateHtml()
{
	if (typeof(NeatUploadReq) != 'undefined' && NeatUploadReq.readyState == 4) 
	{
		try
		{
			var responseXmlDoc = NeatUploadReq.responseXML;
			if (responseXmlDoc.parseError && responseXmlDoc.parseError.errorCode != 0)
			{
//				window.alert('parse error: ' + responseXmlDoc.parseError.reason);
			}
			var templates = responseXmlDoc.getElementsByTagName('neatUploadDetails');
			var status = templates.item(0).getAttribute('status');
			for (var t = 0; t < templates.length; t++)
			{
				var srcElem = templates.item(t);
				var innerXml = '';
				for (var i = 0; i < srcElem.childNodes.length; i++)
				{
					var childNode = srcElem.childNodes.item(i);
					var xml = childNode.xml;
					if (xml == null)
						xml = new XMLSerializer().serializeToString(childNode);
					innerXml += xml;
				}
				var id = srcElem.getAttribute('id');
				var destElem = document.getElementById(id);
				destElem.innerHTML = innerXml;
				for (var a=0; a < srcElem.attributes.length; a++)
				{
					var attr = srcElem.attributes.item(a);
					if (attr.specified)
					{
						if (attr.name == 'style' && destElem.style && destElem.style.cssText)
							destElem.style.cssText = attr.value;
						else
							destElem.setAttribute(attr.name, attr.value);
					}
				}
			}
			if (status != 'NormalInProgress' && status != 'ChunkedInProgress' && status != 'Unknown')
			{
				NeatUploadRefreshPage();
			}
			var lastMillis = NeatUploadLastUpdate.getTime();
			NeatUploadLastUpdate = new Date();
			var delay = Math.max(lastMillis + 1000 - NeatUploadLastUpdate.getTime(), 1);
			NeatUploadReloadTimeoutId = setTimeout(NeatUploadRefresh, delay);
		}
		catch (ex)
		{
//			window.alert(ex);
			NeatUploadRefreshPage();
		}
	}
}

NeatUploadLastUpdate = new Date(); 

window.onunload = NeatUpload_CombineHandlers(window.onunload, function () 
{
	if (NeatUploadReq && NeatUploadReq.readystate
		&& NeatUploadReq.readystate >= 1 && NeatUploadReq.readystate <=3)
	{
		NeatUploadReq.abort();
	}
	NeatUploadReq = null;
});

NeatUploadMainWindow = NeatUploadGetMainWindow();

if (!NeatUploadCanCancel)
{
	NeatUploadRemoveCancelLink();
}

function NeatUploadRefresh()
{
	if (!NeatUploadRefreshWithAjax(NeatUploadRefreshUrl + '&useXml=true'))
	{
		NeatUploadRefreshPage();
	}
}

function NeatUploadRefreshPage() 
{
	window.location.replace(NeatUploadRefreshUrl);
}

window.onload = NeatUpload_CombineHandlers(window.onload, function () 
{
	NeatUploadReloadTimeoutId = setTimeout(NeatUploadRefresh, 1000);
});

