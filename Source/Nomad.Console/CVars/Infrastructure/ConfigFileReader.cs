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

using NomadCore.GameServices;
using System;

namespace NomadCore.Systems.ConsoleSystem.CVars.Infrastructure {
	/*
	===================================================================================
	
	ConfigFileReader
	
	===================================================================================
	*/
	/// <summary>
	/// Loads configuration values from the provided .ini file
	/// </summary>

	internal readonly ref struct ConfigFileReader {
		private readonly IniLoader? Loader = null;

		/*
		===============
		ConfigFileReader
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="configFile"></param>
		public ConfigFileReader( ILoggerService logger, string configFile ) {
			ArgumentException.ThrowIfNullOrEmpty( configFile );

			logger.PrintLine( $"Loading configuration file {configFile}..." );

			Loader = new IniLoader( configFile, logger );
		}

		/*
		===============
		TryGetValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetValue( string name, out string value ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			ArgumentNullException.ThrowIfNull( Loader );

			return Loader.LoadConfigValue( name, out value );
		}
	};
};