/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Collections.Generic;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;
using Nomad.Core;
using Nomad.CVars;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Services {
	/*
	===================================================================================

	FMODMusicService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODMusicService : IMusicService {
		public bool IsPlaying => _musicInstance.getPlaybackState( out var state ) == FMOD.RESULT.OK && state == FMOD.Studio.PLAYBACK_STATE.PLAYING;

		private FMODEventResource _musicHandle;
		private FMOD.Studio.EventInstance _musicInstance;

		private FMOD.Studio.EventInstance _queuedTheme;

		private readonly IResourceCacheService<FMODEventResource, EventId> _eventRepository;
		private readonly Queue<FMODChannel> _loopingTracks = new Queue<FMODChannel>();

		private float _musicVolume = 0.0f;
		private bool _musicOn = true;

		/*
		===============
		FMODMusicService
		===============
		*/
		/// <summary>
		/// Creates an FMODMusicService
		/// </summary>
		/// <param name="eventRepository"></param>
		/// <param name="cvarSystem"></param>
		/// <exception cref="Exception"></exception>
		public FMODMusicService( IResourceCacheService<FMODEventResource, EventId> eventRepository, ICVarSystemService cvarSystem ) {
			_eventRepository = eventRepository;

			var musicVolume = cvarSystem.GetCVar<float>( Constants.CVars.Audio.MUSIC_VOLUME ) ?? throw new Exception( "Missing CVar 'audio.MusicVolume'" );
			musicVolume.ValueChanged.OnPublished += OnMusicVolumeChanged;
			_musicVolume = musicVolume.Value;

			var musicOn = cvarSystem.GetCVar<bool>( Constants.CVars.Audio.MUSIC_ON ) ?? throw new Exception( "Missing CVar 'audio.MusicOn'" );
			musicOn.ValueChanged.OnPublished += OnMusicOnChanged;
			_musicOn = musicOn.Value;
		}

		/*
		===============
		PlayTheme
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <exception cref="InvalidCastException"></exception>
		public void PlayTheme( EventId name ) {
			if ( !_musicOn ) {
				QueueTheme( name );
				// TODO: queue the requested theme to play if we enable music
				return;
			}

			_eventRepository.GetCached( name ).Get( out var handle );
			_musicHandle = handle;
			FMODValidator.ValidateCall( _musicHandle.Handle.createInstance( out _musicInstance ) );
			FMODValidator.ValidateCall( _musicInstance.setVolume( _musicVolume / 100.0f ) );
			FMODValidator.ValidateCall( _musicInstance.start() );
		}

		/*
		===============
		StopTheme
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void StopTheme() {
			if ( !_musicOn || !IsPlaying ) {
				return;
			}
			FMODValidator.ValidateCall( _musicInstance.stop( FMOD.Studio.STOP_MODE.ALLOWFADEOUT ) );
		}

		/*
		===============
		QueueTheme
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="eventId"></param>
		/// <exception cref="InvalidCastException"></exception>
		private void QueueTheme( EventId eventId ) {
			_eventRepository.GetCached( eventId ).Get( out var handle );
			FMODValidator.ValidateCall( handle.Handle.createInstance( out _queuedTheme ) );
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
		private void OnMusicOnChanged( in CVarValueChangedEventArgs<bool> args ) {
			_musicOn = args.NewValue;
			if ( !_musicOn ) {
				FMODValidator.ValidateCall( _musicInstance.stop( FMOD.Studio.STOP_MODE.IMMEDIATE ) );
			}
		}

		/*
		===============
		OnMusicVolumeChanged
		===============
		*/
		private void OnMusicVolumeChanged( in CVarValueChangedEventArgs<float> args ) {
			_musicVolume = args.NewValue;
			if ( IsPlaying ) {
				FMODValidator.ValidateCall( _musicInstance.setVolume( _musicVolume / 100.0f ) );
			}
		}
	};
};
