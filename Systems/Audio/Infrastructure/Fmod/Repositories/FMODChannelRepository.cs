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
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories {
	internal sealed class FMODChannelRepository {
		private readonly FMODChannel[] _channels;
		private int _maxChannels;

		public FMODChannelRepository( ICVarSystemService cvarSystem ) {
			var maxActiveChannels = cvarSystem.GetCVar<int>( "audio.MaxActiveChannels" ) ?? throw new Exception( "Missing CVar 'audio.MaxActiveChannels'" );
			var maxChannels = cvarSystem.GetCVar<int>( "audio.MaxChannels" ) ?? throw new Exception( "Missing CVar 'audio.MaxChannels'" );

			_maxChannels = maxActiveChannels.Value;
			_channels = new FMODChannel[ maxChannels.Value ];

			maxActiveChannels.ValueChanged.Subscribe( this, OnMaxChannelsValueChanged );
		}

		public FMODChannel AllocateChannel( FMODAudioSource source, EventId id ) {
			int current = DateTime.UtcNow.Millisecond;
			FMODChannel? oldest = null;

			for ( int i = 0; i < _maxChannels; i++ ) {
				var channel = _channels[ i ];
				if ( !channel.HasInstance() ) {
					channel.AllocateInstance();
					return channel;
				}
				if ( channel.GetPlaybackState() != FMOD.Studio.PLAYBACK_STATE.STOPPED ) {
					if ( oldest == null ) {
						oldest = channel;
					} else if ( current - oldest.Timestamp > channel.Timestamp ) {
						oldest = channel;
					}
				} else {
					// we have a free event, take it
					channel.AllocateInstance();
					return channel;
				}
			}

			oldest?.ReleaseInstance();
			oldest.AllocateInstance();
			return oldest;
		}

		private void OnMaxChannelsValueChanged( in CVarValueChangedEventData<int> args ) {
			_maxChannels = args.Value;
		}
	};
};