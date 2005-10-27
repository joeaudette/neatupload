/*

NeatUpload - an HttpModule and User Control for uploading large files
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
using System.Collections;
using System.Configuration;
using System.Web;
using System.IO;
using System.Xml;

namespace Brettle.Web.NeatUpload
{
	internal class Config
	{
		// Create a logger for use in this class
		private static readonly log4net.ILog log 
			= log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		internal static Config Current 
		{
			get
			{
				Config config = null;
				if (HttpContext.Current != null)
					config = HttpContext.Current.Items["NeatUpload_config"] as Config;
				if (config == null)
				{
					config = HttpContext.Current.GetConfig("brettle.web/neatUpload") as Config;
				}
				if (config == null)
				{
					config = HttpContext.Current.GetConfig("system.web/neatUpload") as Config;
				}
				if (config == null && ConfigurationSettings.AppSettings != null)
				{
					config = CreateFromAppSettings(ConfigurationSettings.AppSettings);
				}
				if (config == null)
				{
					config = new Config();
				}

				if (HttpContext.Current != null)
				{
					HttpContext.Current.Items["NeatUpload_config"] = config;
				}
				return config;
			}
		}

		private Config() {}

		private static Config CreateFromAppSettings(System.Collections.Specialized.NameValueCollection appSettings)
		{
			Config config = new Config();
			string maxNormalRequestLengthSetting 
				= appSettings["NeatUpload.MaxNormalRequestLength"];
			if (maxNormalRequestLengthSetting != null)
			{
				config.MaxNormalRequestLength = Int64.Parse(maxNormalRequestLengthSetting) * 1024;
			}

			string maxRequestLengthSetting 
				= ConfigurationSettings.AppSettings["NeatUpload.MaxRequestLength"];
			if (maxRequestLengthSetting != null)
			{
				config.MaxRequestLength = Int64.Parse(maxRequestLengthSetting) * 1024;
			}
			
			string tmpDir = appSettings["NeatUpload.DefaultTempDirectory"];
			if (tmpDir != null)
			{
				if (HttpContext.Current != null)
				{
					tmpDir = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, tmpDir);
				}
				config.DefaultTempDirectory = new DirectoryInfo(tmpDir);
			}
			return config;
		}

		internal static Config CreateFromConfigSection(Config parent, System.Xml.XmlAttributeCollection attrs)
		{
			if (log.IsDebugEnabled) log.Debug("In CreateFromConfigSection");
			Config config = new Config();
			if (parent != null)
			{
				config.DefaultTempDirectory = parent.DefaultTempDirectory;
				config.MaxNormalRequestLength = parent.MaxNormalRequestLength;
				config.MaxRequestLength = parent.MaxNormalRequestLength;
				config.UseHttpModule = parent.UseHttpModule;
			}
			foreach (XmlAttribute attr in attrs)
			{
				string name = attr.Name as string;
				string val = attr.Value as string;
				if (log.IsDebugEnabled) log.Debug("Processing attr " + name + "=" + val);
				if (name == "maxNormalRequestLength")
				{
					config.MaxNormalRequestLength = Int64.Parse(val) * 1024;
				}
				else if (name == "maxRequestLength")
				{
					config.MaxRequestLength = Int64.Parse(val) * 1024;
				}
				else if (name == "defaultTempDirectory")
				{
					if (HttpContext.Current != null)
					{
						val = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, val);
					}
					config.DefaultTempDirectory = new DirectoryInfo(val);
				}
				else if (name == "useHttpModule")
				{
					config.UseHttpModule = bool.Parse(val) && UploadHttpModule.IsInited;
				}
				else
				{
					throw new XmlException("Unrecognized attribute: " + name);
				}
			}
			return config;
		}

		internal long MaxNormalRequestLength = 4096 * 1024;
		internal long MaxRequestLength = Int64.MaxValue;
		internal DirectoryInfo DefaultTempDirectory = new DirectoryInfo(Path.GetTempPath());
		internal bool UseHttpModule = UploadHttpModule.IsInited;
	}
}
