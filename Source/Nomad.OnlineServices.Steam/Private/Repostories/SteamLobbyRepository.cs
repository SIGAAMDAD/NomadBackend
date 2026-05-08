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
using System.Threading;
using Nomad.CVars;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Repositories
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
		public SteamLobbyRepository( ICVarSystemService cvarSystem )
		{
			var lobbyPurgeTimeout = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.LOBBY_PURGE_INTERVAL );
			_lobbyPurgeTimeout = lobbyPurgeTimeout.Value;

			_purgeTimer = new Timer( _ => RemoveStaleLobbies(), null, TimeSpan.FromSeconds( _lobbyPurgeTimeout ), TimeSpan.FromSeconds( _lobbyPurgeTimeout ) );
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
				_purgeTimer?.Change( Timeout.Infinite, Timeout.Infinite );
				_purgeTimer?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
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
				_lobbyList.TryAdd( id, new SteamLobbyData( id.Id, SteamLobbyData.GetInfo( id.Id ), id.Guid ) );
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
				_lobbyList.TryRemove( toRemove[i], out _ );
			}
		}
	};
};
