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
using System.Collections.Concurrent;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Interfaces;
using Nomad.Core;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.Repositories
{
	/*
	===================================================================================
	
	FMODAudioGroupRepository
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODAudioGroupRepository : IDisposable
	{
		private readonly ConcurrentDictionary<string, IAudioGroup> _groups = new();
		private readonly FMOD.Studio.System _system;

		private readonly IAudioGroup _musicGroup;
		private readonly IAudioGroup _soundEffectsGroup;

		private readonly ISubscriptionHandle _onSoundEffectsVolumeChanged;
		private readonly ISubscriptionHandle _onMusicVolumeChanged;

		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;

		/*
		===============
		FMODAudioGroupRepository
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="system"></param>
		/// <param name="cvarSystem"></param>
		public FMODAudioGroupRepository( FMOD.Studio.System system, ILoggerCategory category, ICVarSystemService cvarSystem )
		{
			_system = system;
			_category = category;

			var soundEffectsVolume = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME );
			soundEffectsVolume.ValueChanged.Subscribe( OnSoundEffectsVolumeChanged );

			var soundEffectsOn = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.EngineUtils.Audio.EFFECTS_ON );
			soundEffectsOn.ValueChanged.Subscribe( OnSoundEffectsOnChanged );

			var musicVolume = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.MUSIC_VOLUME );
			musicVolume.ValueChanged.Subscribe( OnMusicVolumeChanged );

			var musicOn = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.EngineUtils.Audio.MUSIC_ON );
			musicOn.ValueChanged.Subscribe( OnMusicOnChanged );

			var musicGroupName = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.EngineUtils.Audio.AUDIO_MUSIC_BUS_GROUP_NAME ).Value;
			_musicGroup = FindGroup( musicGroupName );
			_musicGroup.Volume = musicVolume.Value;

			var soundEffectsGroupName = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.EngineUtils.Audio.AUDIO_SOUND_EFFECTS_BUS_GROUP_NAME ).Value;
			_soundEffectsGroup = FindGroup( soundEffectsGroupName );
			_soundEffectsGroup.Volume = soundEffectsVolume.Value;
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_onMusicVolumeChanged?.Dispose();
				_onSoundEffectsVolumeChanged?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		FindGroup
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private IAudioGroup FindGroup( string name )
		{
			if ( !_groups.TryGetValue( name, out var group ) ) {
				_category.PrintLine( $"Fetching bus group '{name}'..." );
				FMODValidator.ValidateCall( _system.getBus( name, out var bus ) );
				group = new FMODAudioGroup( bus, name );
			}
			return group;
		}

		/*
		===============
		OnMusicOnChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnMusicOnChanged( in CVarValueChangedEventArgs<bool> args )
		{
			_musicGroup.Muted = !args.NewValue;
		}

		/*
		===============
		OnSoundEffectsOnChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnSoundEffectsOnChanged( in CVarValueChangedEventArgs<bool> args )
		{
			_soundEffectsGroup.Muted = !args.NewValue;
		}

		/*
		===============
		OnSoundEffectsVolumeChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnSoundEffectsVolumeChanged( in CVarValueChangedEventArgs<float> args )
		{
			_soundEffectsGroup.Volume = args.NewValue;
		}

		/*
		===============
		OnMusicVolumeChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnMusicVolumeChanged( in CVarValueChangedEventArgs<float> args )
		{
			_musicGroup.Volume = args.NewValue;
		}
	};
};
