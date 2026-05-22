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

	SaveReadContext

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveReadContext
	{
		private readonly HashSet<string>? _manifest;

		public ISaveSectionReader Section { get; }

		public SaveReadContext( ISaveSectionReader section )
		{
			Section = section ?? throw new ArgumentNullException( nameof( section ) );
			_manifest = ReadManifest( section );
		}

		public bool HasField( string path )
		{
			// If no manifest exists, fall back to old behavior and let the core reader return defaults.
			return _manifest == null || _manifest.Contains( path );
		}

		private static HashSet<string>? ReadManifest( ISaveSectionReader section )
		{
			bool manifestPresent = section.GetField<bool>( "__NomadExt.ManifestPresent" );

			if ( !manifestPresent ) {
				return null;
			}

			int count = section.GetField<int>( "__NomadExt.Manifest.Count" );
			var fields = new HashSet<string>( StringComparer.Ordinal );

			for ( int i = 0; i < count; i++ ) {
				string path = section.GetString( $"__NomadExt.Manifest[{i}]" );

				if ( !string.IsNullOrWhiteSpace( path ) ) {
					fields.Add( path );
				}
			}

			return fields;
		}
	};
};
