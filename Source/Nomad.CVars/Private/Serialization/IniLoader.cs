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
using System.IO;
using Microsoft.Extensions.Configuration.Ini;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Core.Memory.Buffers;

namespace Nomad.CVars.Private.Serialization
{
	/*
	===================================================================================

	IniLoader

	===================================================================================
	*/
	/// <summary>
	/// Loads key-value pairs from .ini files, specifically configuration files
	/// </summary>

	internal readonly struct IniLoader
	{
		private readonly IDictionary<string, string?>? _iniData;

		/*
		===============
		IniLoader
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="configFile"></param>
		/// <param name="logger"></param>
		/// <param name="fileSystem"></param>
		public IniLoader( string configFile, ILoggerCategory logger, IFileSystem fileSystem )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( configFile );
			ArgumentGuard.ThrowIfNull( logger );

			_iniData = null;

			logger.PrintLine( $"Loading configuration file '{configFile}'..." );

			try {
				string resolvedPath = ResolveConfigPath( configFile, fileSystem );
				IBufferHandle? fileBuffer = fileSystem.LoadFile( resolvedPath ) ??
					throw new FileNotFoundException( $"Could not find file '{resolvedPath}'.", resolvedPath );

				using ( fileBuffer )
				using ( var fileStream = fileBuffer.AsStream( 0, fileBuffer.Length ) ) {
					_iniData = IniStreamConfigurationProvider.Read( fileStream );
					if ( _iniData == null ) {
						logger.PrintError( $"IniLoader: error parsing ini data from configuration file '{resolvedPath}'" );
					}
				}
			} catch ( Exception e ) {
				logger.PrintError( $"IniLoader: Error opening configuration file '{configFile}: {e.Message}" );
				throw;
			}
		}

		private static string ResolveConfigPath( string configFile, IFileSystem fileSystem )
		{
			ArgumentGuard.ThrowIfNull( fileSystem );

			if ( Path.IsPathRooted( configFile ) || !string.IsNullOrEmpty( Path.GetDirectoryName( configFile ) ) ) {
				return configFile;
			}

			string configDirectoryCandidate = Path.Combine( fileSystem.GetConfigPath(), configFile );
			return fileSystem.FileExists( configDirectoryCandidate ) ? configDirectoryCandidate : configFile;
		}

		/*
		===============
		LoadConfigValue
		===============
		*/
		/// <summary>
		/// Loads a configuration value in the form of an integer.
		/// </summary>
		/// <param name="name">The name of the key of the configuration value</param>
		/// <param name="value">The output value</param>
		public bool LoadConfigValue( string name, out int value )
		{
			ArgumentGuard.ThrowIfNull( _iniData );
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( _iniData.TryGetValue( name, out string? data ) ) {
				ArgumentGuard.ThrowIfNullOrEmpty( data );
				value = Convert.ToInt32( data );
				return true;
			}
			value = 0;
			return false;
		}

		/*
		===============
		LoadConfigValue
		===============
		*/
		/// <summary>
		/// Loads a configuration value in the form of an integer.
		/// </summary>
		/// <param name="name">The name of the key of the configuration value</param>
		/// <param name="value">The output value</param>
		public bool LoadConfigValue( string name, out uint value )
		{
			ArgumentGuard.ThrowIfNull( _iniData );
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( _iniData.TryGetValue( name, out string? data ) ) {
				ArgumentGuard.ThrowIfNullOrEmpty( data );
				value = Convert.ToUInt32( data );
				return true;
			}
			value = 0;
			return false;
		}

		/*
		===============
		LoadConfigValue
		===============
		*/
		/// <summary>
		/// Loads a configuration value in the form of a float
		/// </summary>
		/// <param name="name">The name of the key of the configuration value</param>
		/// <param name="value">The output value</param>
		public bool LoadConfigValue( string name, out float value )
		{
			ArgumentGuard.ThrowIfNull( _iniData );
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( _iniData.TryGetValue( name, out string? data ) ) {
				ArgumentGuard.ThrowIfNullOrEmpty( data );
				value = Convert.ToSingle( data );
				return true;
			}
			value = 0.0f;
			return false;
		}

		/*
		===============
		LoadConfigValue
		===============
		*/
		/// <summary>
		/// Loads a configuration value in the form of a boolean
		/// </summary>
		/// <param name="name">The name of the key of the configuration value</param>
		/// <param name="value">The output value</param>
		public bool LoadConfigValue( string name, out bool value )
		{
			ArgumentGuard.ThrowIfNull( _iniData );
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( _iniData.TryGetValue( name, out string? data ) ) {
				ArgumentGuard.ThrowIfNullOrEmpty( data );

				string str = data!.Trim().ToLower();
				if ( str == "1" || str == "true" || str == "yes" || str == "on" ) {
					value = true;
				} else if ( str == "0" || str == "false" || str == "no" || str == "off" ) {
					value = false;
				} else {
					value = bool.TryParse( data, out bool result ) && result;
				}
				return true;
			}
			value = false;
			return false;
		}

		/*
		===============
		LoadConfigValue
		===============
		*/
		/// <summary>
		/// Loads a configuration value in the form of a string
		/// </summary>
		/// <param name="name">The name of the key of the configuration value</param>
		/// <param name="value">The output value</param>
		public bool LoadConfigValue( string name, out string value )
		{
			ArgumentGuard.ThrowIfNull( _iniData );
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( _iniData.TryGetValue( name, out string? data ) ) {
				ArgumentGuard.ThrowIfNull( data );
				value = data;
				return true;
			}
			value = string.Empty;
			return false;
		}
	};
};
