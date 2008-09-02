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

namespace Brettle.Web.NeatUpload
{
	public class UploadStateStoreProviderCollection : Hashtable
	{
		public UploadStateStoreProvider this[string key] 
		{
            get { return (UploadStateStoreProvider)base[key]; }
		}

        public virtual void Add(UploadStateStoreProvider provider)
		{
			if (provider == null)
			{
				throw new System.ArgumentNullException("provider");
			}
			base.Add(provider.Name, provider);
		}

        public new UploadStateStoreProviderCollection Clone()
		{
            UploadStateStoreProviderCollection clone = new UploadStateStoreProviderCollection();
            foreach (UploadStateStoreProvider provider in Values)
			{
				clone.Add(provider);
			}
			return clone;
		}
	}
}
