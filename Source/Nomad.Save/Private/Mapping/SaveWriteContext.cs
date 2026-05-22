/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Collections.Generic;
using Nomad.Save.Interfaces;

namespace Nomad.Save.Private.Mapping
{
	/*
	===================================================================================

	SaveWriteContext

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveWriteContext
	{
		private readonly ISaveSectionWriter _section;
		private readonly List<string> _manifest = new();

		/*
		===============
		SaveWriteContext
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="section"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SaveWriteContext( ISaveSectionWriter section )
		{
			_section = section ?? throw new ArgumentNullException( nameof( section ) );
		}

		/*
		===============
		WriteScalar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		/// <param name="value"></param>
		/// <param name="type"></param>
		public void WriteScalar( string path, object? value, Type type )
		{
			ScalarFieldIO.Write( _section, path, value, type );
			_manifest.Add( path );
		}

		/*
		===============
		WriteManifest
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void WriteManifest()
		{
			ScalarFieldIO.Write( _section, "__NomadExt.ManifestPresent", true, typeof( bool ) );
			ScalarFieldIO.Write( _section, "__NomadExt.Manifest.Count", _manifest.Count, typeof( int ) );

			for ( int i = 0; i < _manifest.Count; i++ ) {
				ScalarFieldIO.Write(
					_section,
					$"__NomadExt.Manifest[{i}]",
					_manifest[i],
					typeof( string ) );
			}
		}
	};
};
