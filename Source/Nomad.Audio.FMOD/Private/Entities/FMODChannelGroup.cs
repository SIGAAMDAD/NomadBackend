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
using System.Security;
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
		public readonly ChannelHandle Hash;

		public float Volume {
			get => _volume;
			set {
				if ( _volume == value ) {
					return;
				}
				_volume = value;
				_handle.setVolume( value );
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
				_handle.setPitch( value );
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
				_handle.setMute( value );
			}
		}
		private bool _muted = false;

		private readonly FMOD.ChannelGroup _handle;

		private bool _isDisposed = false;

		/*
		===============
		FMODChannelGroup
		===============
		*/
		/// <summary>
		/// Creates an FMODChannelGroup.
		/// </summary>
		/// <param name="name">The name of the group.</param>
		/// <param name="system">The core FMOD system.</param>
		public FMODChannelGroup( string name, FMOD.System system ) {
			FMODValidator.ValidateCall( system.createChannelGroup( name, out _handle ) );
			Hash = new( name.GetHashCode() );
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
			FMODValidator.ValidateCall( _handle.release() );
			_handle.clearHandle();

			GC.SuppressFinalize( this );
		}
	};
};
