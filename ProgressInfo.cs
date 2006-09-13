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
using System.Collections;
using System.Collections.Specialized;
using System.Net;

namespace Brettle.Web.NeatUpload
{
	[Serializable]
	public class ProgressInfo
	{
		// Create a logger for use in this class
/*		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
*/
		public ProgressInfo(long maximum, string units)
		{
			Maximum = maximum;
			Units = units;
		}
		
		protected ProgressInfo()
		{
		}

		private long _Maximum;
		public virtual long Maximum { get { return _Maximum; } set { _Maximum = value; SyncWithSession(); } }
		
		private long _Value;
		public virtual long Value { get { return _Value; } set { _Value = value; SyncWithSession(); } }
		
		private string _Text;
		public virtual string Text { get { return _Text; } set { _Text = value; SyncWithSession(); } }
		
		private string _Units;
		public virtual string Units { get { return _Units; } set { _Units = value; SyncWithSession(); } }
		
		public virtual string ToHtml()
		{
			if (Text != null)
			{
				return System.Web.HttpUtility.HtmlEncode(Text);
			}
			return String.Format(Config.Current.ResourceManager.GetString("ProgressInfoFormat"), 
								Value, Maximum, Units);
		}
		
		private DateTime TimeOfLastSync = DateTime.MinValue;

		protected void SyncWithSession()
		{
			if (TimeOfLastSync.AddSeconds(1) > DateTime.Now)
			{
				return;
			}
			UploadContext ctx = UploadContext.Current;
			if (ctx != null)
			{
				UploadHttpModule.AccessSession(new SessionAccessCallback(ctx.SyncWithSession));
			}
			TimeOfLastSync = DateTime.Now;
		}
	}
}
