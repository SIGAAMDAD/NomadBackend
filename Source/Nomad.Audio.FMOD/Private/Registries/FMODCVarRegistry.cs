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

using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Core;
using Nomad.Core.CVars;

namespace Nomad.Audio.Fmod.Private.Registries
{
	/*
	===================================================================================

	FMODCVarRegistry

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class FMODCVarRegistry
	{
		/*
		===============
		Register
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="cvarSystem"></param>
		public static void Register( ICVarSystemService cvarSystem )
		{
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.EngineUtils.Audio.FMOD.STREAM_BUFFER_SIZE,
					DefaultValue = 12,
					Description = "The size of FMOD's stream buffer in milliseconds.",
					Flags = CVarFlags.Archive | CVarFlags.Init,
					Group = "Audio.FMOD",
					Validator = value => value > 100 && value <= 5000
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<uint> {
					Name = Constants.CVars.EngineUtils.Audio.FMOD.DSP_BUFFER_SIZE,
					DefaultValue = 32,
					Description = "The size of FMOD's dsp buffer.",
					Flags = CVarFlags.Archive | CVarFlags.Init,
					Group = "Audio.FMOD",
					Validator = value => value > 10 && value < 48
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.EngineUtils.Audio.FMOD.DSP_BUFFER_COUNT,
					DefaultValue = 4,
					Description = "The number of FMOD dsp buffers.",
					Flags = CVarFlags.Archive | CVarFlags.Init,
					Group = "Audio.FMOD",
					Validator = value => value >= 2 && value <= 8
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.EngineUtils.Audio.FMOD.LOGGING,
#if DEBUG
					DefaultValue = true,
#else
					DefaultValue = false,
#endif
					Description = "Enables a dedicated FMOD debug log.",
					Flags = CVarFlags.Developer | CVarFlags.ReadOnly,
					Group = "Audio.FMOD",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<FMODBankLoadingStrategy> {
					Name = Constants.CVars.EngineUtils.Audio.FMOD.BANK_LOADING_STRATEGY,
					DefaultValue = FMODBankLoadingStrategy.Streaming,
					Description = "Sets the loading policy for how FMOD banks are handled in memory.",
					Flags = CVarFlags.ReadOnly | CVarFlags.Archive,
					Group = "Audio.FMOD",
					Validator = value => value >= FMODBankLoadingStrategy.Streaming && value < FMODBankLoadingStrategy.Compressed
				}
			);
		}
	};
};
