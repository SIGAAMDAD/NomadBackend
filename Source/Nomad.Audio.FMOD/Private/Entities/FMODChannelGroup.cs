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
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Entities {
	/*
	===================================================================================

	FMODChannelGroup

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODChannelGroup : IDisposable {
		public readonly SoundCategory Category;

		public float Volume {
			get => _volume;
			set {
				if ( _volume == value ) {
					return;
				}
				_volume = value;
				FMODValidator.ValidateCall( _group.setVolume( value ) );
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
				FMODValidator.ValidateCall( _group.setPitch( value ) );
			}
		}
		private float _pitch = 1.0f;

		public bool Muted {
			get => _muted;
			set {
				if ( _muted == value ) {
					return;
				}
				_muted = value;
				FMODValidator.ValidateCall( _group.setMute( value ) );
			}
		}
		private bool _muted = false;

		/// <summary>
		/// Gets the memory usage of the bus.
		/// </summary>
		public FMOD.Studio.MEMORY_USAGE MemoryUsage {
			get {
				FMODValidator.ValidateCall( _bus.getMemoryUsage( out var memoryUsage ) );
				return memoryUsage;
			}
		}

		private readonly FMOD.ChannelGroup _group;
		private readonly FMOD.Studio.Bus _bus;

		private bool _isDisposed = false;

		/*
		===============
		FMODChannelGroup
		===============
		*/
		/// <summary>
		/// Creates an FMODChannelGroup.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="system">The core FMOD system.</param>
		public FMODChannelGroup( SoundCategory category, FMOD.Studio.System system ) {
			system.getBus( category.Name, out _bus );
			FMODValidator.ValidateCall( _bus.getChannelGroup( out _group ) );
		}

		/*
		===============
		~FMODChannelGroup
		===============
		*/
		~FMODChannelGroup() {
			Dispose();
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _isDisposed ) {
				return;
			}

			_isDisposed = true;

			if ( _bus.isValid() ) {
				FMODValidator.ValidateCall( _bus.stopAllEvents( FMOD.Studio.STOP_MODE.IMMEDIATE ) );
				FMODValidator.ValidateCall( _group.release() );
				_group.clearHandle();
				_bus.clearHandle();
			}

			GC.SuppressFinalize( this );
		}
	};
};
