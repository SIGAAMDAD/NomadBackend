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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using Godot;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities {
	/*
	===================================================================================
	
	FMODAudioSource
	
	===================================================================================
	*/
	/// <summary>
	/// A source of audio, an "emitter" in a sense.
	/// </summary>
	
	internal sealed class FMODAudioSource( FMODChannelRepository channelRepository, string category ) : IAudioSource {
		public Vector2 Positon {
			get => _position;
			set {
				if ( _position == value ) {
					return;
				}
				_position = value;
				_channel?.SetPosition( value );
			}
		}
		private Vector2 _position = Vector2.Zero;

		public float Volume {
			get => _volume;
			set {
				if ( _volume == value ) {
					return;
				}
				_volume = value;
				_channel?.SetVolume( value );
			}
		}
		private float _volume = 0.0f;

		public string Category => category;

		public AudioSourceStatus Status => _status;
		private AudioSourceStatus _status = AudioSourceStatus.Stopped;

		private FMODChannel? _channel;

		public void PlaySound( EventId id, float priority = 0.5f ) {
			_channel = channelRepository.AllocateChannel( id, _position, category, priority, false );
		}
	};
};