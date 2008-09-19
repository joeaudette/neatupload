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

#if ! USE_LOG4NET


// This class is only used when not using log4net for logging.
// To use log4net for logging, add a reference to log4net and define USE_LOG4NET.

using System;
using System.Collections;
using System.IO;
using System.Web;
using System.Configuration;

namespace Brettle.Web.NeatUpload.log4net
{
	internal interface ILog
	{
		void Debug(string message);
		void DebugFormat(string format, params object[] args);
		bool IsDebugEnabled { get; }

		void Error(string message, Exception ex);
	}

	internal class NullLogger : ILog
	{
		public void Debug(string message) {}
		public void DebugFormat(string format, params object[] args) {}
		public bool IsDebugEnabled { get { return false; } }

		public void Error(string message, Exception ex) {}
	}
	
	internal class AppStateLogger : ILog
	{
		static AppStateLogger()
		{
			StringWriter writer = new StringWriter();
			HttpContext.Current.Application["NeatUpload_AppStateLogger"] = writer;			
			writer.WriteLine("Log started");
		}
		
		internal AppStateLogger()
		{
			Writer = HttpContext.Current.Application["NeatUpload_AppStateLogger"] as TextWriter;
		}
		
		private TextWriter Writer;

		public void Debug(string message) { Writer.WriteLine(message); }
		public void DebugFormat(string format, params object[] args) { Writer.WriteLine(format, args); }
		public bool IsDebugEnabled { get { return true; } }

		public void Error(string message, Exception ex) { Writer.WriteLine(message + ": " + ex); }
	}

	internal class ConsoleLogger : ILog
	{
		internal ConsoleLogger()
		{
			Writer = Console.Out;
		}
		
		private TextWriter Writer;

		public void Debug(string message) { Writer.WriteLine(message); }
		public void DebugFormat(string format, params object[] args) { Writer.WriteLine(format, args); }
		public bool IsDebugEnabled { get { return true; } }

		public void Error(string message, Exception ex) { Writer.WriteLine(message + ": " + ex); }
	}

	internal class LogManager
	{
		internal static ILog GetLogger(Type type)
		{
			// Don't access Config.Current because doing so will trigger some logging which we don't yet have
			// a logger.
			string logger = null;
			if (ConfigurationSettings.AppSettings != null)
			{
				logger = ConfigurationSettings.AppSettings["NeatUpload.Logger"];
			}
			if (logger == "AppStateLogger")
			{
				return new AppStateLogger();
			}
			else if (logger == "ConsoleLogger")
			{
				return new ConsoleLogger();
			}
			else
			{
				return new NullLogger();
			}
		}
	}

	internal class ThreadContext
	{
		internal static Hashtable Properties = new Hashtable();
	}
}

#endif

