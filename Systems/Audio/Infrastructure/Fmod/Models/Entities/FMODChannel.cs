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
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;

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
		public FMOD.Studio.EventInstance Instance;
		public Vector2 Position;
		public EventId Path;
		public float BasePriority;
		public float CurrentPriority;
		public float StartTime;
		public int ChannelId;
		public SoundCategory Category;
		public float LastStolenTime = 0.0f;
		public float Volume = 1.0f;
		public int PlayCount = 0;
		public bool IsEssential = false;

		public float Age => Time.GetTicksMsec() / 1000.0f - StartTime;
		public bool IsPlaying => GetPlaybackState() == FMOD.Studio.PLAYBACK_STATE.PLAYING;

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
				StartTime = Time.GetTicksMsec();
			}
		}

		/*
		===============
		GetPlaybackState
		===============
		*/
		public FMOD.Studio.PLAYBACK_STATE GetPlaybackState() {
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
				FMODValidator.ValidateCall(Instance.setVolume( volume ) );
			}
		}

		/*
		===============
		SetPosition
		===============
		*/
		public void SetPosition( Vector2 position ) {
			if ( Instance.hasHandle() ) {
				FMODValidator.ValidateCall( Instance.set3DAttributes( new FMOD.ATTRIBUTES_3D{ position = new FMOD.VECTOR{ x = position.X, y = position.Y, z = 0.0f } } ) );
			}
		}
	};
};