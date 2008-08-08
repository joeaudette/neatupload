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

using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload
{
	public class AsyncUploadPage : Page
	{
		// Create a logger for use in this class
		// private static readonly log4net.ILog log 
		//	= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
			string controlID = Request.Params["NeatUpload_AsyncControlID"];
			Console.WriteLine("controlID={0}", controlID);
			string postBackID = Request.Params[Config.Current.PostBackIDQueryParam];
			Console.WriteLine("postBackID={0}", postBackID);
			UploadContext uploadContext = UploadContext.Current;
			// If we don't have an uploadContext then this is the pre-upload POST that contains the
			// storage config and file sizes.
			if (uploadContext == null && controlID != null && postBackID != null)
			{
				uploadContext = new UploadContext();
				uploadContext.RegisterPostBack(postBackID);
				Console.WriteLine("uploadContext={0}", uploadContext);
				FieldNameTranslator translator = UploadStorage.CreateFieldNameTranslator();
				Console.WriteLine("translator={0}", translator);
				string secureStorageConfigString 
					= Request.Params[translator.FormatConfigFieldName(postBackID, controlID)];
				Console.WriteLine("secureStorageConfigString={0}", secureStorageConfigString);
				if (secureStorageConfigString != null)
					uploadContext.SecureStorageConfigString = secureStorageConfigString;
				string fileSizesString = Request.Params[UploadContext.FileSizesName];
				Console.WriteLine("fileSizesString={0}", fileSizesString);
				if (fileSizesString != null && fileSizesString.Length > 0)
				{
					string[] fileSizeStrings = fileSizesString.Split(' ');
					long[] fileSizes = new long[fileSizeStrings.Length];
					for (int i = 0; i < fileSizes.Length; i++)
						fileSizes[i] = Int64.Parse(fileSizeStrings[i]);
					uploadContext.FileSizes = fileSizes;
				}
				UploadHttpModule.AccessSession(new SessionAccessCallback(uploadContext.SyncWithSession));
			}

			base.OnLoad(e);
		}		
	}
}
