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

using System.Collections.Generic;
using System;
using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;

namespace NomadCore.Systems.ConsoleSystem.CVars.Infrastructure {
	/*
	===================================================================================
	
	CVarGroup
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class CVarGroup( InternString name, ILoggerService logger, ICVarSystemService cvarSystem ) {
		public readonly InternString Name = name;
		public readonly HashSet<InternString> Cvars = new HashSet<InternString>();

		private readonly ILoggerService _logger = logger;
		private readonly ICVarSystemService _cvarSystem = cvarSystem;

		/*
		===============
		Add
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public void Add( InternString name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( !_cvarSystem.CVarExists( name ) ) {
				_logger.PrintError( $"CVarGroup.Add: cvar '{name}' doesn't exist!" );
			} else if ( !Cvars.Add( name ) ) {
				_logger.PrintError( $"CVarGroup.Add: cvar '{name}' already added to group '{Name}'!" );
			}
		}
	};
};