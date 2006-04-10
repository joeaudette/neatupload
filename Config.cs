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
using System.Resources;
using System.Security.Cryptography;

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
				{
					config = HttpContext.Current.Items["NeatUpload_config"] as Config;
					if (config == null)
					{
						config = HttpContext.Current.GetConfig("brettle.web/neatUpload") as Config;
					}
					if (config == null)
					{
						config = HttpContext.Current.GetConfig("system.web/neatUpload") as Config;
					}
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
					// If 2 threads try to create a new config simultaneously, only use the first one.
					lock (HttpContext.Current.Items.SyncRoot)
					{
						if (HttpContext.Current.Items["NeatUpload_config"] == null)
						{
							HttpContext.Current.Items["NeatUpload_config"] = config;
						}
						else
						{
							config = (Config) HttpContext.Current.Items["NeatUpload_config"];
						}
					}
				}
				return config;
			}
		}

		private Config() 
		{
            try
            {
                this.ResourceManager = new ResourceManager("Brettle.Web.NeatUpload.Strings",
                                                            System.Reflection.Assembly.GetExecutingAssembly());
                // Force an exception if the resources aren't there because...
                string test = this.ResourceManager.GetString("UploadTooLargeMessageFormat");
            }
            catch (MissingManifestResourceException)
            {
                // ...the namespace qualifier was not used until VS2005, and the assembly might have been built
                // with VS2003.
                this.ResourceManager = new ResourceManager("NeatUpload.Strings",
                                                            System.Reflection.Assembly.GetExecutingAssembly());
            }
        }

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
				UploadStorage.LastResortProvider.TempDirectory = new DirectoryInfo(tmpDir);
			}
			return config;
		}

		internal static Config CreateFromConfigSection(Config parent, System.Xml.XmlNode section)
		{
			if (log.IsDebugEnabled) log.Debug("In CreateFromConfigSection");
			Config config = new Config();
			if (parent != null)
			{
				config.MaxNormalRequestLength = parent.MaxNormalRequestLength;
				config.MaxRequestLength = parent.MaxRequestLength;
				config.MaxUploadRate = parent.MaxUploadRate;
				config.UseHttpModule = parent.UseHttpModule;
				config.Providers = parent.Providers.Clone();
				config.DefaultProviderName = parent.DefaultProviderName;
				config.ResourceManager = parent.ResourceManager;
				config.DebugDirectory = parent.DebugDirectory;
				config.ValidationKey = parent.ValidationKey;
				config.EncryptionKey = parent.EncryptionKey;
				config.PostBackIDQueryParam = parent.PostBackIDQueryParam;
			}
			foreach (XmlAttribute attr in section.Attributes)
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
				else if (name == "maxUploadRate")
				{
					config.MaxUploadRate = Int32.Parse(val) * 1024;
				}
				else if (name == "useHttpModule")
				{
					config.UseHttpModule = bool.Parse(val) && UploadHttpModule.IsInited;
				}
				else if (name == "defaultProvider")
				{
					config.DefaultProviderName = val;
				}
				else if (name == "debugDirectory")
				{
					if (HttpContext.Current != null)
					{
						val = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, 
												val);
					}
					config.DebugDirectory = new DirectoryInfo(val);
					if (!config.DebugDirectory.Exists)
					{
						config.DebugDirectory.Create();
					}
				}
				else if (name == "validationKey")
				{
					if (val != "AutoGenerate")
					{
						config.ValidationKey = FromHexString(val);
					}
				}
				else if (name == "encryptionKey")
				{
					if (val != "AutoGenerate")
					{
						config.EncryptionKey = FromHexString(val);
					}
				}
				else if (name == "postBackIDQueryParam")
				{
					config.PostBackIDQueryParam = val;
				}
				else
				{
					throw new XmlException("Unrecognized attribute: " + name);
				}
			}
			XmlNode providersElem = section.SelectSingleNode("providers");
			if (providersElem != null)
			{
				foreach (XmlNode providerActionElem in providersElem.ChildNodes)
				{
					string tagName = providerActionElem.LocalName;
					string providerName = providerActionElem.Attributes["name"].Value;
					if (tagName == "add")
					{
						config.Providers.Add(UploadStorage.CreateProvider(providerActionElem));
					}
					else if (tagName == "remove")
					{
						config.Providers.Remove(providerName);
					}
					else if (tagName == "clear")
					{
						config.Providers.Clear();
					}
					else
					{
						throw new XmlException("Unrecognized tag name: " + tagName);
					}
				}
			}
			return config;
		}

		private static byte[] FromHexString(string s)
		{
			byte[] result = new byte[s.Length/2];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = byte.Parse(s.Substring(i*2, 2), System.Globalization.NumberStyles.HexNumber);
			}
			return result;
		}
		
		internal string DefaultProviderName = null;
		internal UploadStorageProviderCollection Providers = new UploadStorageProviderCollection();
		internal long MaxNormalRequestLength = 4096 * 1024;
		internal long MaxRequestLength = 2097151 * 1024;
		internal int MaxUploadRate = -1;
		internal bool UseHttpModule = UploadHttpModule.IsInited;
		internal ResourceManager ResourceManager = null;
		internal DirectoryInfo DebugDirectory = null;
		internal byte[] ValidationKey = Config.DefaultValidationKey;
		internal byte[] EncryptionKey = Config.DefaultEncryptionKey;
		internal string PostBackIDQueryParam = "NeatUpload_PostBackID";
		
		private static byte[] DefaultValidationKey = null;
		private static byte[] DefaultEncryptionKey = null;
		
		static Config()
		{
			using (KeyedHashAlgorithm macAlg = KeyedHashAlgorithm.Create())
			{
				DefaultValidationKey = new byte[(macAlg.HashSize+7)/8];
				RandomNumberGenerator rng = RandomNumberGenerator.Create();
				rng.GetBytes(DefaultValidationKey);
			}
			
			using (SymmetricAlgorithm cipher = SymmetricAlgorithm.Create())
			{
				cipher.GenerateKey();
				DefaultEncryptionKey = cipher.Key;
			}
		}
	}
}
