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
using NomadCore.Interfaces.Common;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
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
	
	internal sealed class FMODChannel : IEntity<FMODChannelId> {
		public FMODChannelId Id => throw new NotImplementedException();

		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => _timestamp;
		private DateTime _timestamp;

		public int Version => _version;
		private int _version;

		public FMODChannelResource? Resource {
			get => _event;
			set => _event = value;
		}
		private FMODChannelResource? _event;

		public FMODChannel() {
		}

		public bool AllocateChannel( FMOD.Studio.EventDescription eventDescription, in DateTime timestamp ) {
			if ( _event == null ) {
				FMODValidator.ValidateCall( eventDescription.createInstance( out var instance ) );
				_event = new FMODChannelResource( instance );
			}
		}

		public void SetVolume( float volume ) {
			if ( !_event.HasValue ) {
				return;
			}
			_event.Value.SetVolume( volume );
		}

		public void SetPosition( Vector2 position ) {
			if ( !_event.HasValue ) {
				return;
			}
			_event.Value.SetPosition( position );
		}

		public bool Equals( IEntity<FMODChannelId>? other ) {
			return other?.Id == Id;
		}
	};
};