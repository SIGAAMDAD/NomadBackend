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
using Nomad.Core.OnlineServices;
using Nomad.Networking.Session;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Network
{
	/*
	===================================================================================

	SteamNetConnection

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamNetConnection
	{
		public NetworkConnectionState Status => _status;
		private NetworkConnectionState _status;

		public DateTime LastStateChangeUtc { get; private set; } = DateTime.UtcNow;

		public CSteamID? RemoteSteamId => _remoteSteamId;
		private readonly CSteamID? _remoteSteamId;

		public HSteamNetConnection Connection => _connection;
		private readonly HSteamNetConnection _connection = HSteamNetConnection.Invalid;

		public SteamNetworkingIdentity Identity => _identity;
		private readonly SteamNetworkingIdentity _identity;

		/*
		===============
		SteamNetConnection
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="remoteIdentity"></param>
		public SteamNetConnection( HSteamNetConnection connection, SteamNetworkingIdentity remoteIdentity )
		{
			_connection = connection;
			_identity = remoteIdentity;

			if ( remoteIdentity.GetSteamID64() != 0 ) {
				_remoteSteamId = new CSteamID( remoteIdentity.GetSteamID64() );
			}
		}

		public void SetStatus( NetworkConnectionState state )
		{
			if ( _status == state ) {
				return;
			}
			_status = state;
			LastStateChangeUtc = DateTime.UtcNow;
		}
	};
};
