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
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using System.IO;
using System.Xml;
using System.Resources;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Reflection;

namespace Brettle.Web.NeatUpload.Internal.Module
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

        [SecurityPermission(SecurityAction.Assert, SerializationFormatter=true)]
		private Config()
		{
			this.ResourceManager = ResourceManagerSingleton.ResourceManager;
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
				config._UseHttpModule = parent.UseHttpModule;
				config.StorageProviders = parent.StorageProviders.Clone();
				config.DefaultStorageProviderName = parent.DefaultStorageProviderName;
                config.StateStoreProviders = parent.StateStoreProviders.Clone();
                config.DefaultStateStoreProviderName = parent.DefaultStateStoreProviderName;
                config.ResourceManager = parent.ResourceManager;
				config.DebugDirectory = parent.DebugDirectory;
				config.ValidationKey = parent.ValidationKey;
				config.EncryptionKey = parent.EncryptionKey;
				config.PostBackIDQueryParam = parent.PostBackIDQueryParam;
				config.MergeIntervalSeconds = parent.MergeIntervalSeconds;
				config.StateStaleAfterSeconds = parent.StateStaleAfterSeconds;
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
					config._UseHttpModule = bool.Parse(val) && UploadHttpModule.IsInited;
				}
                else if (name == "defaultStorageProvider" || name == "defaultProvider")
				{
					config.DefaultStorageProviderName = val;
				}
                else if (name == "defaultStateStoreProvider")
                {
                    config.DefaultStateStoreProviderName = val;
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
				else if (name == "stateMergeIntervalSeconds")
				{
					config.MergeIntervalSeconds = Double.Parse(val);
				}
				else if (name == "stateStaleAfterSeconds")
				{
					config.StateStaleAfterSeconds = Double.Parse(val);
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
                    if (providerActionElem.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }
					string tagName = providerActionElem.LocalName;
					if (tagName == "add")
					{
                        string providerName = providerActionElem.Attributes["name"].Value;
                        object provider = CreateProvider(providerActionElem);
                        NameValueCollection configAttrs = GetProviderAttrs(providerActionElem);
                        UploadStorageProvider storageProvider = provider as UploadStorageProvider;
                        UploadStateStoreProvider stateStoreProvider = provider as UploadStateStoreProvider;
                        if (storageProvider != null)
                        {
                            storageProvider.Initialize(providerName, configAttrs);
                            config.StorageProviders.Add(storageProvider);
                        }
                        else if (stateStoreProvider != null)
                        {
                            stateStoreProvider.Initialize(providerName, configAttrs);
                            config.StateStoreProviders.Add(stateStoreProvider);
                        }
                        else
                        {
                            throw new Exception(String.Format("Provider {0} must be either an UploadStorageProvider or and UploadStateStoreProvider"));
                        }
					}
					else if (tagName == "remove")
					{
						string providerName = providerActionElem.Attributes["name"].Value;
                        config.StorageProviders.Remove(providerName);
                        config.StateStoreProviders.Remove(providerName);
                    }
					else if (tagName == "clear")
					{
                        config.StorageProviders.Clear();
                        config.StateStoreProviders.Clear();
                    }
					else
					{
						throw new XmlException("Unrecognized tag name: " + tagName);
					}
				}
			}
			return config;
		}

        private static object CreateProvider(System.Xml.XmlNode providerActionElem)
        {
            string providerTypeName = providerActionElem.Attributes["type"].Value;

            Type providerType = Type.GetType(providerTypeName);
            ConstructorInfo constructor = providerType.GetConstructor(new Type[0]);
            return constructor.Invoke(new object[0]);
        }

        private static NameValueCollection GetProviderAttrs(System.Xml.XmlNode providerActionElem)
        {
            NameValueCollection configAttrs = new NameValueCollection();
            foreach (System.Xml.XmlAttribute attr in providerActionElem.Attributes)
            {
                string name = attr.Name;
                string val = attr.Value;
                if (name != "name" && name != "type")
                {
                    configAttrs[name] = val;
                }
            }
            return configAttrs;
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

        internal string DefaultStorageProviderName = null;
        internal UploadStorageProviderCollection StorageProviders = new UploadStorageProviderCollection();
        internal string DefaultStateStoreProviderName = null;
        internal UploadStateStoreProviderCollection StateStoreProviders = new UploadStateStoreProviderCollection();
        internal long MaxNormalRequestLength = 4096 * 1024;
		internal long MaxRequestLength = 2097151 * 1024;
		internal int MaxUploadRate = -1;
        private bool _UseHttpModule = UploadHttpModule.IsInited;
        internal bool UseHttpModule
        {
//            set { _UseHttpModule = value; }
            get { return _UseHttpModule && CanGetWorkerRequest; }
        }

		private ResourceManager ResourceManager = null;
		
		internal string GetResourceString(string key)
		{
			if (this.ResourceManager == null)
			{
				return "NeatUpload resources are unavailable.  Either increase trust level or build NeatUpload against .NET 2.0.";
			}
			return this.ResourceManager.GetString(key);
		}
		
		internal DirectoryInfo DebugDirectory = null;
		internal byte[] ValidationKey = Config.DefaultValidationKey;
		internal byte[] EncryptionKey = Config.DefaultEncryptionKey;
		internal string PostBackIDQueryParam = "NeatUpload_PostBackID";
		internal double MergeIntervalSeconds = 1.0;
		internal double StateStaleAfterSeconds = 60.0;
		
		private static byte[] DefaultValidationKey = null;
		private static byte[] DefaultEncryptionKey = null;

        private bool CanGetWorkerRequestInited = false;
        private bool _CanGetWorkerRequest = false;
        private bool CanGetWorkerRequest
        {
            get
            {
                if (CanGetWorkerRequestInited)
                {
                    return _CanGetWorkerRequest;
                }
                if (HttpContext.Current == null) return false;
                HttpApplicationState appState = HttpContext.Current.Application;
                if (appState == null) return false;
                object canGetWorkerRequest = appState["NeatUpload_CanGetWorkerRequest"];
                if (canGetWorkerRequest != null)
                {
                    _CanGetWorkerRequest = (bool)canGetWorkerRequest;
                    CanGetWorkerRequestInited = true;
                }
                else
                {
                    appState["NeatUpload_CanGetWorkerRequest"] = _CanGetWorkerRequest = false;
                    CanGetWorkerRequestInited = true;
                    try
                    {
                        UploadHttpModule.GetCurrentWorkerRequest();
                        appState["NeatUpload_CanGetWorkerRequest"] = _CanGetWorkerRequest = true;
                    }
                    catch (System.Security.SecurityException secEx)
                    {
                        // Trust level does not allow access to HttpWorkerRequest.
                        // Prior to .NET 2.0, ASP.NET stored requests in memory
                        // so just disabling the module could open the site to denial of service attacks.
                        // To avoid that we throw an explanatory exception in that case.
                        if (System.Environment.Version.Major < 2)
                        {
                            throw new System.Configuration.ConfigurationException("Can't use NeatUpload's UploadHttpModule at this trust level outside the GAC.  Either install NeatUpload in the GAC, or use full trust, or disable the UploadHttpModule.  If you disable the UploadHttpModule, ASP.NET will hold uploads in memory and the ProgressBar will not display.", secEx);
                        }
                    }
                }
                return _CanGetWorkerRequest;
            }
        }


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
