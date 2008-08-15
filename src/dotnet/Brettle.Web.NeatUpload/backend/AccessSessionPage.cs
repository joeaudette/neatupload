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
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.SessionState;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload
{
	public class AccessSessionPage : Page
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
		}
		
		protected override void OnLoad(EventArgs e)
		{
			if (log.IsDebugEnabled) log.Debug("In AccessSessionPage.OnLoad()");
			Internal.SessionAccessingWorkerRequest worker 
				= (Internal.SessionAccessingWorkerRequest)UploadHttpModule.GetCurrentWorkerRequest();
			worker.Accessor(HttpContext.Current.Session);
			return;
		}
			
	}
}
