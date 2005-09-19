/*
Javascript Postback Module For NeatUpload
Copyright (C) 2005  Stefano Straus (tustena.sf.net)

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
using System.Web.UI;
using Brettle.Web.NeatUpload;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Descrizione di riepilogo per jsupload.
	/// </summary>
	public class ProgressScript : Page
	{
		private void Page_Load(object sender, EventArgs e)
		{
			Response.Clear();
			Response.ContentType = "text/javascript";
			try
			{
				string postBackID = Request.Params["postBackID"];
				UploadContext uploadContext = UploadContext.FindByID(postBackID);

				// Set the status to Cancelled if requested.
				if (uploadContext != null && Request.Params["cancelled"] == "true")
				{
					uploadContext.Status = UploadStatus.Cancelled;
				}

				string uploadBarProgress;
				string remainingTimeSpan;
				if (uploadContext != null)
				{
					uploadBarProgress = Math.Round(uploadContext.PercentComplete).ToString();
					switch (uploadContext.Status)
					{
						case UploadStatus.Cancelled:
							Response.Write("var uploadstatus='cancelled';");
							break;
						case UploadStatus.InProgress:
							TimeSpan tr = uploadContext.TimeRemaining;
							remainingTimeSpan = String.Format("{0:00}:{1:00}", (int) Math.Floor(tr.TotalMinutes), tr.Seconds);
							Response.Write("var uploadstatus='inprogress';\n");
							Response.Write("var uploadprogress='" + uploadBarProgress + "'\n");
							Response.Write("var uploadremainingtime='" + remainingTimeSpan + "'\n");
							break;
						case UploadStatus.Completed:

							if (int.Parse(uploadBarProgress) < 100)
								Response.Write("var uploadstatus='interrupted';\n");
							else
							{
								Response.Write("var uploadstatus='completed';\n");
							}
							break;
						default:
							Response.Write("var uploadstatus='unknown';\n");
							break;
					}
				}
			}
			catch
			{
				Response.Write("var uploadstatus='error';\n");
			}
			finally
			{
				Response.Write("ProcessUpload();\n");
			}
		}

		#region Codice generato da Progettazione Web Form

		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}

		private void InitializeComponent()
		{
			this.Load += new EventHandler(this.Page_Load);
		}

		#endregion
	}
}

