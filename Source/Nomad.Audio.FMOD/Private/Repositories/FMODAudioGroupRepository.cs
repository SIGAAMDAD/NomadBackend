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
		private const string MASTER_BUS_NAME = "bus:/";

		private readonly ConcurrentDictionary<string, IAudioGroup> _groups = new();
		private readonly FMOD.Studio.System _system;

		private readonly IAudioGroup _masterGroup;
		private readonly IAudioGroup _musicGroup;
		private readonly IAudioGroup _soundEffectsGroup;

		private readonly ISubscriptionHandle _onMasterVolumeChanged;
		private readonly ISubscriptionHandle _onSoundEffectsVolumeChanged;
		private readonly ISubscriptionHandle _onSoundEffectsOnChanged;
		private readonly ISubscriptionHandle _onMusicVolumeChanged;
		private readonly ISubscriptionHandle _onMusicOnChanged;

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

			var masterVolume = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.MASTER_VOLUME );
			_onMasterVolumeChanged = masterVolume.ValueChanged.Subscribe( OnMasterVolumeChanged );

			var soundEffectsVolume = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME );
			_onSoundEffectsVolumeChanged = soundEffectsVolume.ValueChanged.Subscribe( OnSoundEffectsVolumeChanged );

			var soundEffectsOn = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.EngineUtils.Audio.EFFECTS_ON );
			_onSoundEffectsOnChanged = soundEffectsOn.ValueChanged.Subscribe( OnSoundEffectsOnChanged );

			var musicVolume = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.MUSIC_VOLUME );
			_onMusicVolumeChanged = musicVolume.ValueChanged.Subscribe( OnMusicVolumeChanged );

			var musicOn = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.EngineUtils.Audio.MUSIC_ON );
			_onMusicOnChanged = musicOn.ValueChanged.Subscribe( OnMusicOnChanged );

			_masterGroup = FindGroup( MASTER_BUS_NAME );
			_masterGroup.Volume = masterVolume.Value;

			var musicGroupName = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.EngineUtils.Audio.AUDIO_MUSIC_BUS_GROUP_NAME ).Value;
			_musicGroup = FindGroup( musicGroupName );
			_musicGroup.Volume = musicVolume.Value;
			_musicGroup.Muted = !musicOn.Value;

			var soundEffectsGroupName = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.EngineUtils.Audio.AUDIO_SOUND_EFFECTS_BUS_GROUP_NAME ).Value;
			_soundEffectsGroup = FindGroup( soundEffectsGroupName );
			_soundEffectsGroup.Volume = soundEffectsVolume.Value;
			_soundEffectsGroup.Muted = !soundEffectsOn.Value;
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
				_onMasterVolumeChanged?.Dispose();
				_onMusicVolumeChanged?.Dispose();
				_onMusicOnChanged?.Dispose();
				_onSoundEffectsVolumeChanged?.Dispose();
				_onSoundEffectsOnChanged?.Dispose();
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
				_groups[name] = group;
			}
			return group;
		}

		/*
		===============
		OnMasterVolumeChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMasterVolumeChanged( in CVarValueChangedEventArgs<float> args )
		{
			_masterGroup.Volume = args.NewValue;
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
