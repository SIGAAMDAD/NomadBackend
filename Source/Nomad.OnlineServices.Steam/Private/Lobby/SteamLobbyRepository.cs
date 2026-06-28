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
using System.Collections.Concurrent;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Nomad.Core.CVars;
using System.Timers;
using Nomad.CVars;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Lobby
{
	/*
	===================================================================================

	SteamLobbyRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamLobbyRepository : IDisposable
	{
		public ICollection<SteamLobbyData> Lobbies => _lobbyList.Values;
		private readonly ConcurrentDictionary<SteamLobbyKey, SteamLobbyData> _lobbyList = new();
		private readonly ConcurrentDictionary<Guid, CSteamID> _idToSteam = new();
		private readonly ConcurrentDictionary<CSteamID, Guid> _steamId64ToId = new();

		private readonly int _lobbyPurgeTimeout = 0;
		private readonly Timer _purgeTimer;

		private bool _isDisposed = false;

		/*
		===============
		SteamLobbyRepository
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="cvarSystem"></param>
		public SteamLobbyRepository( ICVarSystemService cvarSystem )
		{
			var lobbyPurgeTimeout = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.LOBBY_PURGE_INTERVAL );
			_lobbyPurgeTimeout = lobbyPurgeTimeout.Value;

			_purgeTimer = new Timer() {
				AutoReset = true,
				Enabled = true,
				Interval = TimeSpan.FromSeconds( _lobbyPurgeTimeout ).TotalMilliseconds
			};
			_purgeTimer.Elapsed += (sender, args) => RemoveStaleLobbies();
			_purgeTimer.Start();
		}

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
				_purgeTimer?.Stop();
				_purgeTimer?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		SetPollingEnabled
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="enabled"></param>
		public void SetPollingEnabled( bool enabled )
		{
			if ( enabled ) {
				_purgeTimer.Start();
			} else {
				_purgeTimer.Stop();
			}
		}

		/*
		===============
		AddLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		public void AddLobby( SteamLobbyKey id )
		{
			if ( !_lobbyList.TryGetValue( id, out SteamLobbyData? value ) ) {
				var lobby = new SteamLobbyData( id.Id, SteamLobbyData.GetInfo( id.Id ), id.Guid );
				_idToSteam[id.Guid] = id.Id;
				_steamId64ToId[id.Id] = id.Guid;
				_lobbyList.TryAdd( id, lobby );
			} else {
				lock ( value ) {
					value.Update();
				}
			}
		}

		/*
		===============
		AddLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobby"></param>
		public void AddLobby( SteamLobbyData lobby )
		{
			var key = new SteamLobbyKey( lobby.Id, lobby.Guid );
			if ( !_lobbyList.TryGetValue( key, out SteamLobbyData? value ) ) {
				_idToSteam[lobby.Guid] = lobby.Id;
				_steamId64ToId[lobby.Id] = lobby.Guid;
				_lobbyList.TryAdd( key, lobby );
			} else {
				lock ( value ) {
					value.Update();
				}
			}
		}

		/*
		===============
		TryGetLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobbyId"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public bool TryGetLobby( Guid lobbyId, out SteamLobbyData? info )
		{
			if ( !_idToSteam.TryGetValue( lobbyId, out var steamID ) ) {
				info = null;
				return false;
			}
			return _lobbyList.TryGetValue( new SteamLobbyKey( steamID, lobbyId ), out info );
		}

		/*
		===============
		TryGetLobby
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobbyId"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public bool TryGetLobby( CSteamID lobbyId, out SteamLobbyData? info )
		{
			if ( !_steamId64ToId.TryGetValue( lobbyId, out var guid ) ) {
				info = null;
				return false;
			}
			return TryGetLobby( guid, out info );
		}

		/*
		===============
		RemoveStaleLobbies
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void RemoveStaleLobbies()
		{
			if ( _isDisposed ) {
				return;
			}
			var now = DateTime.UtcNow;
			var toRemove = new List<SteamLobbyKey>();

			foreach ( var lobby in _lobbyList ) {
				DateTime lastSeen;
				lock ( lobby.Value ) {
					lastSeen = lobby.Value.LastSeenUtc;
				}
				if ( now - lastSeen > TimeSpan.FromSeconds( _lobbyPurgeTimeout ) ) {
					toRemove.Add( lobby.Key );
				}
			}
			for ( int i = 0; i < toRemove.Count; i++ ) {
				if ( _lobbyList.TryRemove( toRemove[i], out _ ) ) {
					_idToSteam.TryRemove( toRemove[i].Guid, out _ );
					_steamId64ToId.TryRemove( toRemove[i].Id, out _ );
				}
			}
		}
	};
};
