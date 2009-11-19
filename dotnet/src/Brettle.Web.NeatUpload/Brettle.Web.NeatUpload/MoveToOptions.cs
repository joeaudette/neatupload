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

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Options controlling how an uploaded file is moved to a permanent location.
	/// This class only specifies whether a file already at the location can be
	/// overwritten, but subclasses could specify additional options.
	/// </summary>
	public class MoveToOptions
	{
		/// <summary>
		/// Constructs a <see cref="MoveToOptions"/> given whether the file can
		/// replace any file already at the destination location.
		/// </summary>
		/// <param name="canOverwrite">
		/// true to indicate that any file at the destination location can be
		/// overwritten.
		/// </param>
		protected MoveToOptions(bool canOverwrite) { _canOverwrite = canOverwrite; }

		/// <summary>
		/// A <see cref="MoveToOptions"/> that indicates that the any file at the
		/// destination location must not be overwritten.
		/// </summary>
		/// <value>
		/// A <see cref="MoveToOptions"/> that indicates that the any file at the
		/// destination location must not be overwritten.
		/// </value>
		public static readonly MoveToOptions None = new MoveToOptions(false);

		
		/// <summary>
		/// A <see cref="MoveToOptions"/> that indicates that the any file at the
		/// destination location can be overwritten.
		/// </summary>
		/// <value>
		/// A <see cref="MoveToOptions"/> that indicates that the any file at the
		/// destination location can be overwritten.
		/// </value>
		public static readonly MoveToOptions Overwrite = new MoveToOptions(true);

		/// <summary>
		/// true iff any file at the destination location can be overwritten.
		/// </summary>
		/// <value>
		/// true iff any file at the destination location can be overwritten.
		/// </value>
		public virtual bool CanOverwrite {
			get { return _canOverwrite;	}
		}

		private bool _canOverwrite = false;
	}
}