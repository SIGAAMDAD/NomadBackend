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

using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using System.Collections.Concurrent;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories {
	internal sealed class FMODGuidRepository {
		private readonly ConcurrentDictionary<FMODBankId, BankId> _bankGuids = new ConcurrentDictionary<FMODBankId, BankId>();

		private readonly ConcurrentDictionary<FMODEventId, EventId> _eventGuids = new ConcurrentDictionary<FMODEventId, EventId>();

		public EventId GetEventID( string path, FMOD.GUID guid ) {
			var eventId = new EventId( SceneStringPool.Intern( path ) );
			_eventGuids[ new FMODEventId( guid ) ] = eventId;
			return eventId;
		}

		public BankId GetBankID( string path, FMOD.GUID guid ) {
			var bankId = new BankId( SceneStringPool.Intern( path ) );
			_bankGuids[ new FMODBankId( guid ) ] = bankId;
			return bankId;
		}
	};
};