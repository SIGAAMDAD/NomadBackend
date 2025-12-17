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

using NomadCore.Domain.Events;
using NomadCore.GameServices;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Services {
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

		private readonly IResourceCacheService<IEventResource, EventId> _eventRepository;

		private float _musicVolume = 0.0f;
		private bool _musicOn = true;

		public FMODMusicService( IResourceCacheService<IEventResource, EventId> eventRepository, ICVarSystemService cvarSystem ) {
			_eventRepository = eventRepository;

			var musicVolume = cvarSystem.GetCVar<float>( "audio.MusicVolume" ) ?? throw new Exception( "Missing CVar 'audio.MusicVolume'" );
			musicVolume.ValueChanged.Subscribe( this, OnMusicVolumeChanged );
			_musicVolume = musicVolume.Value;

			var musicOn = cvarSystem.GetCVar<bool>( "audio.MusicOn" ) ?? throw new Exception( "Missing CVar 'audio.MusicOn'" );
			musicOn.ValueChanged.Subscribe( this, OnMusicOnChanged );
			_musicOn = musicOn.Value;
		}

		/*
		===============
		PlayTheme
		===============
		*/
		public void PlayTheme( EventId name ) {
			if ( !_musicOn ) {
				// TODO: queue the requested theme to play if we enable music
				return;
			}

			_eventRepository.GetCached( name ).Get( out var handle );
			if ( handle is not FMODEventResource description ) {
				throw new InvalidCastException();
			}
			_musicHandle = description;
			FMODValidator.ValidateCall( _musicHandle.Handle.createInstance( out _musicInstance ) );
			FMODValidator.ValidateCall( _musicInstance.setVolume( _musicVolume / 100.0f ) );
			FMODValidator.ValidateCall( _musicInstance.start() );
		}

		/*
		===============
		Stop
		===============
		*/
		public void Stop() {
			if ( !_musicOn || !IsPlaying ) {
				return;
			}
			FMODValidator.ValidateCall( _musicInstance.stop( FMOD.Studio.STOP_MODE.ALLOWFADEOUT ) );
		}

		private void OnMusicOnChanged( in CVarValueChangedEventData<bool> args ) {
			_musicOn = args.Value;
			if ( !_musicOn ) {
				FMODValidator.ValidateCall( _musicInstance.stop( FMOD.Studio.STOP_MODE.IMMEDIATE ) );
			}
		}

		private void OnMusicVolumeChanged( in CVarValueChangedEventData<float> args ) {
			_musicVolume = args.Value;
			if ( IsPlaying ) {
				FMODValidator.ValidateCall( _musicInstance.setVolume( _musicVolume / 100.0f ) );
			}
		}
	};
};