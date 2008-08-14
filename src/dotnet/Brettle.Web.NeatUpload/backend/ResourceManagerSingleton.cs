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
using System.Resources;

namespace Brettle.Web.NeatUpload
{		
	internal class ResourceManagerSingleton
	{
		internal static readonly ResourceManager ResourceManager;

		internal static string GetResourceString(string key)
		{
			if (ResourceManager == null)
			{
				return "NeatUpload resources are unavailable.  Either increase trust level or build NeatUpload against .NET 2.0.";
			}
			return ResourceManager.GetString(key);
		}
		
		
		static ResourceManagerSingleton()
		{
			try
			{
				try
				{
					ResourceManager = new ResourceManager("Brettle.Web.NeatUpload.Strings",
					                                      System.Reflection.Assembly.GetExecutingAssembly());
					// Force an exception if the resources aren't there because...
					ResourceManager.GetString("UploadTooLargeMessageFormat");
				}
				catch (MissingManifestResourceException)
				{
					// ...the namespace qualifier was not used until VS2005, and the assembly might have been built
					// with VS2003.
					ResourceManager = new ResourceManager("NeatUpload.Strings",
					                                      System.Reflection.Assembly.GetExecutingAssembly());
					ResourceManager.GetString("UploadTooLargeMessageFormat");
				}
			}
			catch (System.Security.SecurityException)
			{
				// This happens when running with medium trust outside the GAC under .NET 2.0, because
				// NeatUpload is compiled against .NET 1.1.  In that environment we almost never need the
				// ResourceManager so we set it to null which will cause GetResourceString() to return a
				// message indicating what the developer needs to do.
				ResourceManager = null;
			}
		}
	}
}
