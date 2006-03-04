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
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace Brettle.Web.NeatUpload
{
	public class UploadStorage
	{
		public static UploadedFile CreateUploadedFile(UploadContext context, string controlUniqueID, string fileName,
		                                              string contentType, UploadStorageConfig storageConfig)
		{
			
			if (storageConfig == null || storageConfig.Count == 0)
			{
				return Provider.CreateUploadedFile(context, controlUniqueID, fileName, contentType);
			}
		                                              		
		    UploadedFile file = Provider.CreateUploadedFile(context, controlUniqueID, fileName, contentType, storageConfig);
			
			return file;
		}

		public static UploadedFile CreateUploadedFile(UploadContext context, string controlUniqueID, string fileName,
		                                              string contentType)
		{
			return Provider.CreateUploadedFile(context, controlUniqueID, fileName, contentType);
		}

		public static UploadStorageConfig CreateUploadStorageConfig()
		{
			return Provider.CreateUploadStorageConfig();
		}
		
		internal static FieldNameTranslator CreateFieldNameTranslator()
		{
			return Provider.CreateFieldNameTranslator();
		}
		
		public static UploadStorageProvider Provider
		{
			get
			{
				Config config = Config.Current;
				if (config.DefaultProviderName == null)
				{
					return LastResortProvider;
				}
				return config.Providers[config.DefaultProviderName];
			}
		}

		public static UploadStorageProviderCollection Providers
		{
			get
			{
				return Config.Current.Providers;
			}
		}

		private static FilesystemUploadStorageProvider _lastResortProvider = null;
		private static object _lock = new object();
		internal static FilesystemUploadStorageProvider LastResortProvider 
		{
			get
			{
				lock (_lock)
				{
					if (_lastResortProvider == null)
					{
						_lastResortProvider = new FilesystemUploadStorageProvider();
						_lastResortProvider.Initialize("FilesystemUploadStorageProvider", new NameValueCollection());
					}
				}
				return _lastResortProvider;
			}
		}

		internal static UploadStorageProvider CreateProvider(System.Xml.XmlNode providerActionElem)
		{
			NameValueCollection configAttrs = new NameValueCollection();
			string providerName = providerActionElem.Attributes["name"].Value;
			string providerTypeName = providerActionElem.Attributes["type"].Value;
			foreach (System.Xml.XmlAttribute attr in providerActionElem.Attributes)
			{
				string name = attr.Name;
				string val = attr.Value;
				if (name != "name" && name != "type")
				{
					configAttrs[name] = val;
				}
			}
			
			Type providerType = Type.GetType(providerTypeName);
			ConstructorInfo constructor = providerType.GetConstructor(new Type[0]);
			UploadStorageProvider provider = (UploadStorageProvider)constructor.Invoke(new object[0]);
			provider.Initialize(providerName, configAttrs);
			return provider;
		}
	}
}
