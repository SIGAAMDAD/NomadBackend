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

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Nomad.Audio.Fmod.Private.Repositories
{
	/*
	===================================================================================

	FMODDriverRepository

	===================================================================================
	*/
	/// <summary>
	/// Holds the supported FMOD output backends and tracks the currently selected one.
	/// </summary>

	internal sealed class FMODDriverRepository
	{
		/// <summary>
		/// A list of all audio driver APIs available for usage with FMOD.
		/// </summary>
		public IReadOnlyList<string> Drivers => _drivers;
		private readonly ImmutableArray<string> _drivers;

		/// <summary>
		/// The currently selected FMOD output backend.
		/// </summary>
		public FMOD.OUTPUTTYPE AudioDriver => _audioDriver;
		private FMOD.OUTPUTTYPE _audioDriver;

		/// <summary>
		/// The human-readable name of the current output backend.
		/// </summary>
		public string Driver => GetDriverName( _audioDriver );

		private readonly ImmutableDictionary<FMOD.OUTPUTTYPE, string> _supportedAudioDrivers;

		/*
		===============
		FMODDriverRepository
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public FMODDriverRepository()
		{
			_supportedAudioDrivers = new Dictionary<FMOD.OUTPUTTYPE, string>() {
				[FMOD.OUTPUTTYPE.AUTODETECT] = "Auto Detect",
#if WINDOWS
				[FMOD.OUTPUTTYPE.ASIO] = "ASIO",
				[FMOD.OUTPUTTYPE.WASAPI] = "WasAPI",
				[FMOD.OUTPUTTYPE.WINSONIC] = "WinSonic",
#elif LINUX
				[FMOD.OUTPUTTYPE.ALSA] = "ALSA",
				[FMOD.OUTPUTTYPE.PULSEAUDIO] = "PulseAudio"
#endif
			}.ToImmutableDictionary();

			_drivers = _supportedAudioDrivers.Values.ToImmutableArray();
			_audioDriver = FMOD.OUTPUTTYPE.AUTODETECT;
		}

		/*
		===============
		SetCurrentDriver
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="audioDriver"></param>
		public void SetCurrentDriver( FMOD.OUTPUTTYPE audioDriver )
		{
			_audioDriver = audioDriver;
		}

		/*
		===============
		GetDriverName
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="audioDriver"></param>
		/// <returns></returns>
		public string GetDriverName( FMOD.OUTPUTTYPE audioDriver )
		{
			return _supportedAudioDrivers.TryGetValue( audioDriver, out string name ) ? name : audioDriver.ToString();
		}
	};
};
