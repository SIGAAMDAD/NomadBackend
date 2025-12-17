/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Microsoft.Extensions.Configuration.Ini;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using System;
using System.Collections.Generic;
using System.IO;

namespace NomadCore.Systems.ConsoleSystem.CVars.Infrastructure {
	/*
	===================================================================================
	
	IniLoader
		
	===================================================================================
	*/
	/// <summary>
	/// Loads key-value pairs from .ini files, specifically configuration files
	/// </summary>

	internal sealed class IniLoader {
		private readonly IDictionary<string, string?> IniData;

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
		public IniLoader( string configFile, ILoggerService logger ) {
			ArgumentException.ThrowIfNullOrEmpty( configFile );

			try {
				using FileStream fileStream = new FileStream( FilePath.FromResourcePath( configFile ).OSPath, FileMode.Open, FileAccess.Read );

				IniData = IniStreamConfigurationProvider.Read( fileStream );
				if ( IniData == null ) {
					logger.PrintError( $"IniLoader: error parsing ini data from configuration file '{configFile}'" );
				}
			} catch ( Exception e ) {
				logger.PrintError( $"IniLoader: Error opening configuration file '{configFile}: {e.Message}" );
			}
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
		public bool LoadConfigValue( string name, out int value ) {
			ArgumentNullException.ThrowIfNull( IniData );
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( IniData.TryGetValue( name, out string? data ) ) {
				ArgumentException.ThrowIfNullOrEmpty( data );
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
		/// Loads a configuration value in the form of a float
		/// </summary>
		/// <param name="name">The name of the key of the configuration value</param>
		/// <param name="value">The output value</param>
		public bool LoadConfigValue( string name, out float value ) {
			ArgumentNullException.ThrowIfNull( IniData );
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( IniData.TryGetValue( name, out string? data ) ) {
				ArgumentException.ThrowIfNullOrEmpty( data );
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
		public bool LoadConfigValue( string name, out bool value ) {
			ArgumentNullException.ThrowIfNull( IniData );
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( IniData.TryGetValue( name, out string? data ) ) {
				ArgumentException.ThrowIfNullOrEmpty( data );
				value = data.Trim() switch {
					"1" or "true" or "True" or "TRUE" or "yes" or "on" => true,
					"0" or "false" or "False" or "FALSE" or "no" or "off" => false,
					_ => bool.TryParse( data, out bool result ) && result
				};
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
		public bool LoadConfigValue( string name, out string value ) {
			ArgumentNullException.ThrowIfNull( IniData );
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( IniData.TryGetValue( name, out string? data ) ) {
				ArgumentException.ThrowIfNullOrEmpty( data );
				value = data;
				return true;
			}
			value = String.Empty;
			return false;
		}
	};
};