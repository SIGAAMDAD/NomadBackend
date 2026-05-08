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

using System.Collections.Concurrent;
using System.Collections.Generic;
using Nomad.Core.Logger;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Repositories
{
	/*
	===================================================================================
	
	SteamConnectionRepository

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class SteamConnectionRepository
	{
		private readonly ConcurrentDictionary<CSteamID, SteamNetConnection> _connections = new();

		public SteamConnectionRepository( ILoggerService logger )
		{
		}

		private readonly ConcurrentDictionary<HSteamNetConnection, SteamNetConnection> _byHandle = new();
		private readonly ConcurrentDictionary<ulong, HSteamNetConnection> _bySteamId64 = new();

		public bool Add( SteamNetConnection connection )
		{
			if ( !_byHandle.TryAdd( connection.Connection, connection ) ) {
				return false;
			}
			if ( connection.RemoteSteamId.HasValue ) {
				_bySteamId64[connection.RemoteSteamId.Value.m_SteamID] = connection.Connection;
			}
			return true;
		}

		public bool TryGet( HSteamNetConnection handle, out SteamNetConnection? connection )
		{
			return _byHandle.TryGetValue( handle, out connection );
		}

		public bool TryGet( CSteamID steamId, out SteamNetConnection? connection )
		{
			connection = null;
			if ( !_bySteamId64.TryGetValue( steamId.m_SteamID, out var handle ) ) {
				return false;
			}
			return _byHandle.TryGetValue( handle, out connection );
		}

		public bool Remove( HSteamNetConnection handle )
		{
			if ( !_byHandle.TryGetValue( handle, out var connection ) ) {
				return false;
			}
			if ( connection.RemoteSteamId.HasValue ) {
				_bySteamId64.TryRemove( connection.RemoteSteamId.Value.m_SteamID, out _ );
			}
			return true;
		}

		public ICollection<SteamNetConnection> Snapshot()
		{
			return _byHandle.Values;
		}
	};
};
