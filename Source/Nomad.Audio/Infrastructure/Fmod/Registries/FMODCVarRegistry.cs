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

using NomadCore.Domain;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;

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
					Name: new( Constants.CVars.Audio.FMOD.STREAM_BUFFER_SIZE ),
					DefaultValue: 12,
					Description: new( "The size of FMOD's stream buffer in milliseconds." ),
					Flags: CVarFlags.Archive | CVarFlags.Init,
					Validator: value => value > 100 && value <= 5000
				)
			);
			cvarSystem.Register(
				new CVarCreateInfo<int>(
					Name: new( Constants.CVars.Audio.FMOD.DSP_BUFFER_SIZE ),
					DefaultValue: 12,
					Description: new( "The size of FMOD's dsp buffer in MB." ),
					Flags: CVarFlags.Archive | CVarFlags.Init,
					Validator: value => value > 10 && value < 48
				)
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool>(
					Name: new( Constants.CVars.Audio.FMOD.LOGGING ),
#if DEBUG
					DefaultValue: true,
#else
					DefaultValue: false,
#endif
					Description: new( "Enables a dedicated FMOD debug log." ),
					Flags: CVarFlags.Developer | CVarFlags.ReadOnly
				)
			);
			cvarSystem.Register(
				new CVarCreateInfo<FMODBankLoadingStrategy>(
					Name: new( Constants.CVars.Audio.FMOD.BANK_LOADING_STRATEGY ),
					DefaultValue: FMODBankLoadingStrategy.Streaming,
					Description: new( "Sets the loading policy for how FMOD banks are handled in memory." ),
					Flags: CVarFlags.ReadOnly | CVarFlags.Archive,
					Validator: value => value >= FMODBankLoadingStrategy.Streaming && value < FMODBankLoadingStrategy.Compressed
				)
			);
		}
	};
};