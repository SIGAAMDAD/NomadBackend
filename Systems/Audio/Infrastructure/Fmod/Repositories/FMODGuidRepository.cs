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
using NomadCore.Systems.Audio.Application.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories {
	/*
	===================================================================================
	
	FMODGuidRepository
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODGuidRepository : IGuidRepository<FMODEventId, FMODBankId> {
		private sealed class GUIDCache<TGuid, TId>( Func<string, TId> factory )
			where TGuid : IValueObject<TGuid>
			where TId : IValueObject<TId>
		{
			private readonly Dictionary<TGuid, TId> _guids = new Dictionary<TGuid, TId>();
			private readonly Dictionary<TId, TGuid> _reverseLookup = new Dictionary<TId, TGuid>();
			private readonly Func<string, TId> _factory = factory;

			public TGuid this[ TId id ] => _reverseLookup[ id ];
			public TId this[ TGuid guid ] => _guids[ guid ];

			/*
			===============
			Add
			===============
			*/
			public void Add( string path, TGuid guid ) {
				var id = _factory.Invoke( path );

				_guids[ guid ] = id;
				_reverseLookup[ id ] = guid;
			}

			/*
			===============
			Clear
			===============
			*/
			public void Clear() {
				_guids.Clear();
				_reverseLookup.Clear();
			}
		};

		private readonly GUIDCache<FMODEventId, EventId> _eventGuids = new GUIDCache<FMODEventId, EventId>( e => new EventId( new( e ) ) );
		private readonly GUIDCache<FMODBankId, BankId> _bankGuids = new GUIDCache<FMODBankId, BankId>( b => new BankId( new( b ) ) );

		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_eventGuids.Clear();
			_bankGuids.Clear();
		}

		/*
		===============
		AddEventId
		===============
		*/
		/// <summary>
		/// Adds an event's id to the guid cache.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="guid"></param>
		public void AddEventId( string path, FMODEventId guid ) {
			_lock.EnterWriteLock();
			try {
				_eventGuids.Add( path, guid );
			}
			finally {
				_lock.ExitWriteLock();
			}
		}

		/*
		===============
		AddBankId
		===============
		*/
		/// <summary>
		/// Adds a bank's id to the guid cache.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="guid"></param>
		public void AddBankId( string path, FMODBankId guid ) {
			_lock.EnterWriteLock();
			try {
				_bankGuids.Add( path, guid );
			}
			finally {
				_lock.ExitWriteLock();
			}
		}

		/*
		===============
		GetEventId
		===============
		*/
		public EventId GetEventId( FMODEventId guid ) {
			_lock.EnterUpgradeableReadLock();
			try {
				return _eventGuids[ guid ];
			}
			finally {
				_lock.ExitUpgradeableReadLock();
			}
		}

		/*
		===============
		GetEventGuid
		===============
		*/
		public FMODEventId GetEventGuid( EventId id ) {
			_lock.EnterUpgradeableReadLock();
			try {
				return _eventGuids[ id ];
			}
			finally {
				_lock.ExitUpgradeableReadLock();
			}
		}

		/*
		===============
		GetBankId
		===============
		*/
		public BankId GetBankId( FMODBankId guid ) {
			_lock.EnterUpgradeableReadLock();
			try {
				return _bankGuids[ guid ];
			}
			finally {
				_lock.ExitUpgradeableReadLock();
			}
		}

		/*
		===============
		GetBankGuid
		===============
		*/
		public FMODBankId GetBankGuid( BankId id ) {
			_lock.EnterUpgradeableReadLock();
			try {
				return _bankGuids[ id ];
			}
			finally {
				_lock.ExitUpgradeableReadLock();
			}
		}
	};
};