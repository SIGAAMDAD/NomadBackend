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

using NomadCore.Interfaces.Common;
using System;
using Godot;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects {
	/*
	===================================================================================
	
	FMODChannelResource
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal readonly record struct FMODChannelResource : IDisposable, IValueObject<FMODChannelResource> {
		private readonly FMOD.Studio.EventInstance _instance;

		public FMODChannelResource( FMOD.Studio.EventInstance instance ) {
			_instance = instance;
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			if ( _instance.isValid() ) {
				_instance.release();
				_instance.clearHandle();
			}
		}

		/*
		===============
		GetPlaybackState
		===============
		*/
		public FMOD.Studio.PLAYBACK_STATE GetPlaybackState() {
			if ( !_instance.isValid() ) {
				return FMOD.Studio.PLAYBACK_STATE.STOPPED;
			}
			FMODValidator.ValidateCall( _instance.getPlaybackState( out var state ) );
			return state;
		}

		/*
		===============
		SetPosition
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetPosition( Vector2 position ) {
			_instance.set3DAttributes( new FMOD.ATTRIBUTES_3D{ position = new FMOD.VECTOR{ x = position.X, y = position.Y, z = 0.0f } } );
		}

		/*
		===============
		SetVolume
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetVolume( float volume ) {
			_instance.setVolume( volume );
		}
	};
};