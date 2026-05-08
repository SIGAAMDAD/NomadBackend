/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Nomad.Audio.Interfaces;

namespace Nomad.Audio.Fmod.Private.Repositories
{
	/*
	===================================================================================

	FMODGuidRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODGuidRepository : IGuidRepository<FMOD.GUID, FMOD.GUID>
	{
		private sealed class GUIDCache<TGuid, TId>
			where TId : notnull
		{
			private readonly Dictionary<TGuid, TId> _guids = new Dictionary<TGuid, TId>();
			private readonly Dictionary<TId, TGuid> _reverseLookup = new Dictionary<TId, TGuid>();

			public TGuid this[TId id] => _reverseLookup[id];
			public TId this[TGuid guid] => _guids[guid];

			/*
			===============
			Add
			===============
			*/
			public void Add( TId path, TGuid guid )
			{
				_guids[guid] = path;
				_reverseLookup[path] = guid;
			}

			/*
			===============
			Clear
			===============
			*/
			public void Clear()
			{
				_guids.Clear();
				_reverseLookup.Clear();
			}
		};

		private readonly GUIDCache<FMOD.GUID, string> _eventGuids = new GUIDCache<FMOD.GUID, string>();
		private readonly GUIDCache<FMOD.GUID, string> _bankGuids = new GUIDCache<FMOD.GUID, string>();

		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		private bool _isDisposed = false;

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_lock?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
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
		public void AddEventId( string path, FMOD.GUID guid )
		{
			_lock.EnterWriteLock();
			try {
				_eventGuids.Add( path, guid );
			} finally {
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
		public void AddBankId( string path, FMOD.GUID guid )
		{
			_lock.EnterWriteLock();
			try {
				_bankGuids.Add( path, guid );
			} finally {
				_lock.ExitWriteLock();
			}
		}

		/*
		===============
		GetEventId
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		public string GetEventId( FMOD.GUID guid )
		{
			_lock.EnterUpgradeableReadLock();
			try {
				return _eventGuids[guid];
			} finally {
				_lock.ExitUpgradeableReadLock();
			}
		}

		/*
		===============
		GetEventGuid
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public FMOD.GUID GetEventGuid( string id )
		{
			_lock.EnterUpgradeableReadLock();
			try {
				return _eventGuids[id];
			} finally {
				_lock.ExitUpgradeableReadLock();
			}
		}

		/*
		===============
		GetBankId
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		public string GetBankId( FMOD.GUID guid )
		{
			_lock.EnterUpgradeableReadLock();
			try {
				return _bankGuids[guid];
			} finally {
				_lock.ExitUpgradeableReadLock();
			}
		}

		/*
		===============
		GetBankGuid
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public FMOD.GUID GetBankGuid( string id )
		{
			_lock.EnterUpgradeableReadLock();
			try {
				return _bankGuids[id];
			} finally {
				_lock.ExitUpgradeableReadLock();
			}
		}
	};
};
