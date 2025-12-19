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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Registries {
	/*
	===================================================================================
	
	FMODCVarRegistry
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public static class FMODCVarRegistry {
		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvarSystem"></param>
		public static void Register( ICVarSystemService cvarSystem ) {
			cvarSystem.Register(
				new CVarCreateInfo<int>(
					Name: new( "audio.fmod.StreamBufferSize" ),
					DefaultValue: 12,
					Description: new( "The size of FMOD's stream buffer size in MB." ),
					Flags: CVarFlags.Archive | CVarFlags.Init,
					Validator: value => value > 10 && value < 48
				)
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool>(
					Name: new( "audio.fmod.LoggingEnabled" ),
#if DEBUG
					DefaultValue: true,
#else
					DefaultValue: false,
#endif
					Description: new( "Enables a dedicated FMOD debug log." ),
					Flags: CVarFlags.Developer | CVarFlags.ReadOnly
				)
			);
		}
	};
};