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
using NomadCore.Abstractions.Services;
using NomadCore.Infrastructure;

namespace NomadCore.Systems.ConsoleSystem.CVars.Infrastructure {
	/*
	===================================================================================
	
	CVarGroup
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class CVarGroup( string? name ) {
		public readonly string Name = name;
		public readonly HashSet<string> Cvars = new HashSet<string>();

		private readonly ILoggerService Logger = ServiceRegistry.Get<ILoggerService>();
		private readonly ICVarSystemService CVarSystem = ServiceRegistry.Get<ICVarSystemService>();

		/*
		===============
		Add
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public void Add( string? name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			if ( !CVarSystem.CVarExists( name ) ) {
				Logger?.PrintError( $"CVarGroup.Add: cvar '{name}' doesn't exist" );
			} else if ( !Cvars.Add( name ) ) {
				Logger?.PrintError( $"CVarGroup.Add: cvar '{name}' already added to group '{Name}'" );
			}
		}
	};
};