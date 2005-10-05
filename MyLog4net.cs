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

#warning LOGGING DISABLED.  To enable logging, add a reference to log4net and define USE_LOG4NET.

using System;
using System.Collections;

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

	internal class LogManager
	{
		internal static ILog GetLogger(Type type)
		{
			return new NullLogger();
		}
	}

	internal class ThreadContext
	{
		internal static Hashtable Properties = new Hashtable();
	}
}

#endif

