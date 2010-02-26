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
using System.IO;
using System.Web.UI.Design;
using System.Web.UI;

namespace Brettle.Web.NeatUpload
{
	public class ProgressBarBaseDesigner : ControlDesigner
	{
		
		public override string GetDesignTimeHtml()
		{
			StringWriter sw = new StringWriter();
			HtmlTextWriter writer = new HtmlTextWriter(sw);
			writer.Write("<i>ProgressBar - no-Javascript fallback = {</i><a href='#'>");
			ProgressBarBase control = (ProgressBarBase)Component;
			if (!control.HasControls())
				writer.Write("Check Upload Progress");
			else
			{
				control.RenderChildControls(writer);
			}
						
			writer.Write("</a><i>}</i>");
			writer.Close();
			return sw.ToString();
		}
	}
}
