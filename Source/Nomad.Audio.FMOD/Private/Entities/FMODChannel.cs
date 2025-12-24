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

using Godot;
using System;
using FMOD.Studio;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Entities {
	/*
	===================================================================================

	FMODChannel

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODChannel : IDisposable {
		public EventInstance Instance;
		public Vector2 Position;
		public EventId Path;
		public float BasePriority;
		public float CurrentPriority;
		public float StartTime;
		public int ChannelId;
		public SoundCategory Category;
		public float LastStolenTime = 0.0f;

		public ChannelGroupHandle Group;

		public float Volume {
			get => _volume;
			set {
				if ( _volume == value ) {
					return;
				}
				_volume = value;
				Instance.setVolume( value );
			}
		}
		private float _volume = 1.0f;

		public float Pitch {
			get => _pitch;
			set {
				if ( _pitch == value ) {
					return;
				}
				_pitch = value;
				Instance.setPitch( value );
			}
		}
		private float _pitch = 1.0f;

		public int PlayCount = 0;
		public bool IsEssential = false;

		public float Age => Time.GetTicksMsec() / 1000.0f - StartTime;
		public bool IsPlaying => GetPlaybackState() == PLAYBACK_STATE.PLAYING;

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			ReleaseInstance();
		}

		/*
		===============
		HasInstance
		===============
		*/
		public bool HasInstance() {
			return Instance.hasHandle();
		}

		/*
		===============
		ReleaseInstance
		===============
		*/
		public void ReleaseInstance() {
			if ( Instance.hasHandle() ) {
				FMODValidator.ValidateCall( Instance.release() );
			}
		}

		/*
		===============
		GetPlaybackState
		===============
		*/
		public FMOD.Studio.PLAYBACK_STATE GetPlaybackState() {
			if ( !Instance.isValid() ) {
				return FMOD.Studio.PLAYBACK_STATE.STOPPED;
			}
			FMODValidator.ValidateCall( Instance.getPlaybackState( out var state ) );
			return state;
		}

		/*
		===============
		AllocateInstance
		===============
		*/
		public void AllocateInstance( FMOD.Studio.EventDescription description ) {
			FMODValidator.ValidateCall( description.createInstance( out Instance ) );
			StartTime = Time.GetTicksMsec();
		}

		/*
		===============
		SetVolume
		===============
		*/
		public void SetVolume( float volume ) {
			if ( Instance.hasHandle() ) {
				FMODValidator.ValidateCall( Instance.setVolume( volume ) );
			}
		}

		/*
		===============
		SetPosition
		===============
		*/
		public void SetPosition( Vector2 position ) {
			if ( Instance.hasHandle() ) {
				FMODValidator.ValidateCall( Instance.set3DAttributes( new FMOD.ATTRIBUTES_3D { position = new FMOD.VECTOR { x = position.X, y = position.Y, z = 0.0f } } ) );
			}
		}
	};
};
