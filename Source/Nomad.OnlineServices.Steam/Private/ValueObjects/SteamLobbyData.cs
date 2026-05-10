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
using Nomad.Core.OnlineServices;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	/*
	===================================================================================

	SteamLobbyData

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamLobbyData
	{
		public string? Name => _info.Name;
		public string? GameMode => _info.GameMode;
		public string? Map => _info.Map;
		public int MaxPlayers => _info.MaxPlayers;
		public ulong OwnerId => _info.OwnerId;
		public LobbyVisibility Visibility => _info.Visibility;
		public IReadOnlyDictionary<string, string> Metadata => _info.Metadata;

		public LobbyInfo Info => _info;
		private LobbyInfo _info;

		public DateTime LastSeenUtc => _lastSeenTime;
		private DateTime _lastSeenTime = DateTime.UtcNow;

		public CSteamID Id => _id;
		private readonly CSteamID _id;

		public Guid Guid => _guid;
		private readonly Guid _guid = Guid.NewGuid();

		/*
		===============
		SteamLobbyData
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="info"></param>
		/// <param name="guid"></param>
		public SteamLobbyData( CSteamID id, LobbyCreateInfo info, Guid guid )
		{
			_id = id;
			_info = new LobbyInfo() {
				Id = new LobbyId( guid ),
				Name = info.Name,
				Map = info.Map,
				GameMode = info.GameMode,
				MaxPlayers = info.MaxPlayers,
				PlayerCount = info.PlayerCount,
				Visibility = info.Visibility,
				Metadata = info.Metadata
			};
			_guid = guid;
		}

		/*
		===============
		SteamLobbyData
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="info"></param>
		/// <param name="guid"></param>
		public SteamLobbyData( CSteamID id, LobbyInfo info, Guid guid )
		{
			_id = id;
			_info = info;
			_guid = guid;
		}

		/*
		===============
		Update
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Update()
		{
			_info = _info with {
				Name = SteamMatchmaking.GetLobbyData( _id, nameof( LobbyInfo.Name ) ),
				Map = SteamMatchmaking.GetLobbyData( _id, nameof( LobbyInfo.Map ) ),
				GameMode = SteamMatchmaking.GetLobbyData( _id, nameof( LobbyInfo.GameMode ) ),
				OwnerId = (ulong)SteamMatchmaking.GetLobbyOwner( _id )
			};
			_lastSeenTime = DateTime.UtcNow;
		}

		/*
		===============
		GetInfo
		===============
		*/
		/// <summary>
		/// Fetches a lobby's metadata from steam servers.
		/// </summary>
		/// <param name="id">The lobby's unique <see cref="CSteamID"/>.</param>
		/// <returns></returns>
		public static LobbyInfo GetInfo( CSteamID id )
		{
			return new LobbyInfo {
				Name = SteamMatchmaking.GetLobbyData( id, nameof( LobbyInfo.Name ) ),
				Map = SteamMatchmaking.GetLobbyData( id, nameof( LobbyInfo.Map ) ),
				GameMode = SteamMatchmaking.GetLobbyData( id, nameof( LobbyInfo.GameMode ) ),
				MaxPlayers = SteamMatchmaking.GetLobbyMemberLimit( id ),
				OwnerId = (ulong)SteamMatchmaking.GetLobbyOwner( id )
			};
		}
	};
};
