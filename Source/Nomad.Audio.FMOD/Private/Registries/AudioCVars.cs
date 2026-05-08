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

using Nomad.Core.CVars;
using Nomad.Core;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Registries
{
	/*
	===================================================================================
	
	AudioCVars
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal static class AudioCVars
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
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.MASTER_VOLUME,
					DefaultValue = 80.0f,
					Description = "The maximum volume output of the game.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
					Validator = value => value >= 0.0f && value <= 100.0f
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME,
					DefaultValue = 50.0f,
					Description = "Sets sound effects volume.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
					Validator = value => value >= 0.0f && value <= 100.0f
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.EngineUtils.Audio.EFFECTS_ON,
					DefaultValue = true,
					Description = "Enables sound effects.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.MUSIC_VOLUME,
					DefaultValue = 50.0f,
					Description = "Sets music volume.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
					Validator = value => value >= 0.0f && value <= 100.0f
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<bool> {
					Name = Constants.CVars.EngineUtils.Audio.MUSIC_ON,
					DefaultValue = true,
					Description = "Enables music.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.EngineUtils.Audio.OUTPUT_DEVICE_INDEX,
					DefaultValue = 0,
					Description = "The device index of the output device to use for audio.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.EngineUtils.Audio.AUDIO_DRIVER,
					DefaultValue = 0,
					Description = "The active audio driver in use by the Audio system.",
					Flags = CVarFlags.Archive,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<SpeakerMode> {
					Name = Constants.CVars.EngineUtils.Audio.SPEAKER_MODE,
					DefaultValue = SpeakerMode.Headphones,
					Description = "The speaker configuration.",
					Flags = CVarFlags.Archive,
					Group = "Audio"
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS,
					DefaultValue = 256,
					Description = "The maximum number of audio channels that can be allocated at a time",
					Flags = CVarFlags.Archive,
					Group = "Audio",
					Validator = value => value >= Constants.Audio.MIN_AUDIO_CHANNELS && value <= Constants.Audio.MAX_AUDIO_CHANNELS
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = Constants.CVars.EngineUtils.Audio.AUDIO_MUSIC_BUS_GROUP_NAME,
					DefaultValue = "bus:/Music",
					Description = "The bus name of the music group. For linking music configurables to the underlying audio middleware.",
					Flags = CVarFlags.Archive | CVarFlags.Init,
					Group = "Audio"
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = Constants.CVars.EngineUtils.Audio.AUDIO_SOUND_EFFECTS_BUS_GROUP_NAME,
					DefaultValue = "bus:/SoundEffects",
					Description = "The bus name of the sound effects group. For linking sound effects configurables to the underlying audio middleware.",
					Flags = CVarFlags.Archive | CVarFlags.Init,
					Group = "Audio"
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<int> {
					Name = Constants.CVars.EngineUtils.Audio.MAX_CHANNELS,
					DefaultValue = 512,
					Description = "The maximum number of audio channels that can be created.",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
					Validator = value => value >= Constants.Audio.MIN_AUDIO_CHANNELS && value <= Constants.Audio.MAX_AUDIO_CHANNELS
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.DISTANCE_FALLOFF_START,
					DefaultValue = 600.0f,
					Description = " ",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.DISTANCE_FALLOFF_END,
					DefaultValue = 2000.0f,
					Description = " ",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.MIN_TIME_BETWEEN_CHANNEL_STEALS,
					DefaultValue = 0.1f,
					Description = " ",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.FREQUENCY_PENALTY,
					DefaultValue = 0.4f,
					Description = " ",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.VOLUME_WEIGHT,
					DefaultValue = 0.2f,
					Description = " ",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
				}
			);
			cvarSystem.Register(
				new CVarCreateInfo<float> {
					Name = Constants.CVars.EngineUtils.Audio.DISTANCE_WEIGHT,
					DefaultValue = 0.3f,
					Description = " ",
					Flags = CVarFlags.Init | CVarFlags.ReadOnly,
					Group = "Audio",
				}
			);
		}
	};
};
