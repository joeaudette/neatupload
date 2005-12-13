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
using System.Text.RegularExpressions;

namespace Brettle.Web.NeatUpload
{
	internal class UploadView
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		private Regex Pattern = new Regex(@"{(\w+)(?:\:(\w+))?}"); // Matches "{variableName:resourceName}"
		
		private delegate string FormatVariableCallBack(string resourceName, GetResourceCallBack getResource);
		private Hashtable Formatters = new Hashtable(); // of FormatVariableCallBacks
		
		private UploadDetails Details;
		
		internal UploadView(UploadContext context)
		{
			Details = context.Details;
			Formatters["BytesRead"] = new FormatVariableCallBack(this.FormatBytesRead);
			Formatters["BytesTotal"] = new FormatVariableCallBack(this.FormatBytesTotal);
			Formatters["FractionComplete"] = new FormatVariableCallBack(this.FormatFractionComplete);
			Formatters["BytesPerSec"] = new FormatVariableCallBack(this.FormatBytesPerSec);
			Formatters["TimeRemaining"] = new FormatVariableCallBack(this.FormatTimeRemaining);
			Formatters["TimeElapsed"] = new FormatVariableCallBack(this.FormatTimeElapsed);
			Formatters["Rejection"] = new FormatVariableCallBack(this.FormatRejection);
			Formatters["Error"] = new FormatVariableCallBack(this.FormatError);
		}
		
		internal bool ReplaceInAttribute(ref string attrValue, GetResourceCallBack getResource)
		{
			AttributeReplacer replacer = new AttributeReplacer(this, getResource);
			attrs[attrName] = Pattern.Replace(attrValueTemplate, new MatchEvaluator(replacer.Replace));
			if (View.ReplacementCount > 0)
			{
				View.AddAttributeReplacement(clientID, attrName, attrValueTemplate);
			}
		}
		
		private class AttributeReplacer
		{
			int ReplacementCount;
			private UploadView View;
			private GetResourceCallBack GetResource;
			
			AttributeReplacer(UploadView view, GetResourceCallBack getResource)
			{
				View = view;
				GetResource = getResource;
			}
			
			string Replace(Match match)
			{
				string variableName = match.Groups[1].Value;
				string resourceName = match.Groups[2].Value;
				FormatVariableCallBack formatVariable = view.Formatters[variableName] as FormatVariableCallBack;
				string newValue = match.Value;
				if (formatter != null)
				{
					newValue = formatVariable(resourceName, GetResource);
					ReplacementCount++;
				}
				return newValue;
			}
		}
		
		private string FormatBytesRead(string resourceName, GetResourceCallBack getResource)
		{
			return FormatBytes(Details.BytesRead, resourceName, getResource);
		}
		
		private string FormatBytesTotal(string resourceName, GetResourceCallBack getResource)
		{
			return FormatBytes(Details.BytesTotal, resourceName, getResource);
		}
		
		private string FormatBytes(long numBytes, string resourceName, GetResourceCallBack getResource)
		{
			if (resourceName == null)
				resourceName = "BytesWithUnitsFormat";
			string format;
			if (Details.UnitSelector < 1000)
				format = getResource(resourceName + ".0");
			else if (Details.UnitSelector < 1000*1000)
				format = getResource(resourceName + ".1");
			else
				format = getResource(resourceName + ".2");
			return String.Format(format, numBytes);
		}
		
		private string FormatFractionComplete(string resourceName, GetResourceCallBack getResource)
		{
			if (resourceName == null)
				resourceName = "PercentFormat";
			
			return String.Format(getResource(resourceName), Details.FractionComplete);
		}
		
		private string FormatBytesPerSec(string resourceName, GetResourceCallBack getResource)
		{
			if (resourceName == null)
				resourceName = "RateFormat";
			string format;
			if (Details.BytesPerSec < 1000)
				format = getResource(resourceName + ".0");
			else if (Details.BytesPerSec < 1000*1000)
				format = getResource(resourceName + ".1");
			else
				format = getResource(resourceName + ".2");
			return String.Format(format, Details.BytesPerSec);
		}
		
		private string FormatTimeRemaining(string resourceName, GetResourceCallBack getResource)
		{
			return FormatTimeSpan(Details.TimeRemaining, resourceName, getResource);
		}
		
		private string FormatTimeElapsed(string resourceName, GetResourceCallBack getResource)
		{
			return FormatTimeSpan(Details.TimeElapsed, resourceName, getResource);
		}
		
		private string FormatTimeSpan(TimeSpan ts, string resourceName, GetResourceCallBack getResource)
		{
			if (resourceName == null)
				resourceName = "TimeSpanFormat";
			string format;
			if (TimeElapsed.TotalSeconds < 60)
				format = getResource(resourceName + ".0");
			else if (TimeElapsed.TotalSeconds < 60*60)
				format = getResource(resourceName + ".1");
			else
				format = getResource(resourceName + ".2");
			return String.Format(format,
			                          (int)Math.Floor(ts.TotalHours),
			                          (int)Math.Floor(ts.TotalMinutes),
			                          ts.Seconds,
			                          ts.TotalHours,
			                          ts.TotalMinutes);
		}
		
		private string FormatRejection(Exception ex, string resourceName, GetResourceCallBack getResource)
		{
			return FormatException(Details.Rejection);
		}
			
		private string FormatError(Exception ex, string resourceName, GetResourceCallBack getResource)
		{
			return FormatException(Details.Error);
		}
			
		private string FormatException(Exception ex, string resourceName, GetResourceCallBack getResource)
		{
			if (resourceName == null)
				resourceName = "ExceptionFormat";
			
			return String.Format(getResource(resourceName),
			                        ex.Message, ex.GetType(), ex.StackTrace, ex.HelpLink);
		}
		
	}
}
