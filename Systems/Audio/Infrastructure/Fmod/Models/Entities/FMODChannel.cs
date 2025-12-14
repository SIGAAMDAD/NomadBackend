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

using Godot;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities {
	/*
	===================================================================================
	
	FMODChannel
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class FMODChannel {
		public int Timestamp { get; set; }

		private FMOD.Studio.EventInstance _instance;

		/*
		===============
		HasInstance
		===============
		*/
		public bool HasInstance() {
			return _instance.hasHandle();
		}

		/*
		===============
		ReleaseInstance
		===============
		*/
		public void ReleaseInstance() {
			if ( _instance.hasHandle() ) {
				FMODValidator.ValidateCall( _instance.release() );
				Timestamp = DateTime.UtcNow.Millisecond;
			}
		}

		/*
		===============
		GetPlaybackState
		===============
		*/
		public FMOD.Studio.PLAYBACK_STATE GetPlaybackState() {
			FMODValidator.ValidateCall( _instance.getPlaybackState( out var state ) );
			return state;
		}

		/*
		===============
		AllocateInstance
		===============
		*/
		public void AllocateInstance( FMOD.Studio.EventDescription description ) {
			FMODValidator.ValidateCall( description.createInstance( out _instance ) );
			Timestamp = DateTime.UtcNow.Millisecond;
		}

		/*
		===============
		SetVolume
		===============
		*/
		public void SetVolume( float volume ) {
			if ( _instance.hasHandle() ) {
				FMODValidator.ValidateCall(_instance.setVolume( volume ) );
			}
		}

		/*
		===============
		SetPosition
		===============
		*/
		public void SetPosition( Vector2 position ) {
			if ( _instance.hasHandle() ) {
				FMODValidator.ValidateCall( _instance.set3DAttributes( new FMOD.ATTRIBUTES_3D{ position = new FMOD.VECTOR{ x = position.X, y = position.Y, z = 0.0f } } ) );
			}
		}
	};
};