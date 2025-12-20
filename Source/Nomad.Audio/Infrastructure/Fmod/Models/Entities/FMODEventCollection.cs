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

using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using System.Collections.Generic;
using NomadCore.GameServices;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities {
	/*
	===================================================================================
	
	FMODEventCollection
	
	===================================================================================
	*/
	/// <summary>
	/// Contains a collection of <see cref="FMODEvent"/> objects, most usually for a bank.
	/// </summary>

	internal record FMODEventCollection : IEventCollection {
		public int EventCount => _events.Length;

		private readonly FMOD.Studio.EventDescription[] _events;
		private readonly HashSet<EventId> _eventIds;

		public FMODEventCollection( FMOD.Studio.EventDescription[] events, FMODGuidRepository guidRepository, ILoggerService logger ) {
			_events = events;
			_eventIds = new HashSet<EventId>( _events.Length );
			for ( int i = 0; i < _events.Length; i++ ) {
				FMODValidator.ValidateCall( _events[ i ].getID( out var guid ) );
				FMODValidator.ValidateCall( _events[ i ].getPath( out var path ) );
				guidRepository.AddEventId( path, new FMODEventId( guid ) );
			}
		}

		public bool ContainsEvent( EventId eventId ) {
			return _eventIds.Contains( eventId );
		}
		public HashSet<EventId> GetEventIds() {
			return _eventIds;
		}
	};
};