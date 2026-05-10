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

using System.Collections.Generic;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Network
{
	internal sealed class SteamConnectionRepository
	{
		private readonly Dictionary<HSteamNetConnection, SteamNetConnection> _byHandle = new();
		private readonly Dictionary<ulong, HSteamNetConnection> _bySteamId64 = new();

		public bool Add( SteamNetConnection connection )
		{
			if ( connection == null ) {
				return false;
			}

			if ( connection.Connection == HSteamNetConnection.Invalid ) {
				return false;
			}

			if ( _byHandle.ContainsKey( connection.Connection ) ) {
				return false;
			}

			_byHandle.Add( connection.Connection, connection );

			if ( connection.RemoteSteamId.HasValue ) {
				_bySteamId64[connection.RemoteSteamId.Value.m_SteamID] = connection.Connection;
			}

			return true;
		}

		public bool TryGet( HSteamNetConnection handle, out SteamNetConnection connection )
		{
			return _byHandle.TryGetValue( handle, out connection );
		}

		public bool TryGet( CSteamID steamId, out SteamNetConnection connection )
		{
			connection = null;

			if ( !_bySteamId64.TryGetValue( steamId.m_SteamID, out HSteamNetConnection handle ) ) {
				return false;
			}

			return _byHandle.TryGetValue( handle, out connection );
		}

		public bool Remove( HSteamNetConnection handle )
		{
			if ( !_byHandle.TryGetValue( handle, out SteamNetConnection connection ) ) {
				return false;
			}

			_byHandle.Remove( handle );

			if ( connection.RemoteSteamId.HasValue ) {
				_bySteamId64.Remove( connection.RemoteSteamId.Value.m_SteamID );
			}

			return true;
		}

		public SteamNetConnection[] Snapshot()
		{
			SteamNetConnection[] snapshot = new SteamNetConnection[_byHandle.Count];
			_byHandle.Values.CopyTo( snapshot, 0 );
			return snapshot;
		}

		public void Clear()
		{
			_byHandle.Clear();
			_bySteamId64.Clear();
		}
	}
}
