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
using System.Runtime.CompilerServices;
using System.Threading;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.CVars;
using Nomad.Networking.Session;
using Nomad.OnlineServices.Steam.Private.Network;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Lobby
{
	/*
	===================================================================================

	SteamLobbyInstance

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamLobbyInstance : IDisposable
	{
		public SortedDictionary<PeerId, SteamSessionPeer> Members => _members;
		private readonly SortedDictionary<PeerId, SteamSessionPeer> _members = new();

		private readonly Dictionary<CSteamID, PeerId> _steam64ToPeer = new();

		public SteamLobbyData Info => _info;
		private readonly SteamLobbyData _info;

		private readonly SteamNetDriver _netDriver;
		private readonly Timer _updateTimer;
		private readonly Callback<LobbyChatUpdate_t> _lobbyMemberStatusChanged;
		private readonly Callback<LobbyKicked_t> _lobbyKicked;

		private bool _isDisposed = false;

		/*
		===============
		SteamLobbyInstance
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="info"></param>
		/// <param name="netDriver"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="eventFactory"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamLobbyInstance(
			SteamLobbyData info,
			SteamNetDriver netDriver,
			ICVarSystemService cvarSystem,
			IGameEventRegistryService eventFactory
		)
		{
			_info = info ?? throw new ArgumentNullException( nameof( info ) );
			_netDriver = netDriver ?? throw new ArgumentNullException( nameof( netDriver ) );

			var updateInterval = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.LOBBY_METADATA_FETCH_INTERVAL );
			_updateTimer = new Timer( OnUpdateTimerTimeout, null, TimeSpan.FromSeconds( updateInterval.Value ), TimeSpan.FromSeconds( updateInterval.Value ) );

			_lobbyMemberStatusChanged = Callback<LobbyChatUpdate_t>.Create( OnLobbyMemberStatusChanged );
			_lobbyKicked = Callback<LobbyKicked_t>.Create( OnLobbyKicked );

			UpdateMembers();
		}

		private void OnLobbyKicked( LobbyKicked_t param )
		{
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
				_updateTimer?.Dispose();
				_lobbyMemberStatusChanged?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		TryGetMember
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="steamId"></param>
		/// <param name="peerId"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryGetMember( CSteamID steamId, out PeerId peerId )
		{
			return _steam64ToPeer.TryGetValue( steamId, out peerId );
		}

		/*
		===============
		OnUpdateTimerTimeout
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="state"></param>
		private void OnUpdateTimerTimeout( object? state )
		{
			lock ( _info ) {
				_info.Update();
			}
		}

		/*
		===============
		UpdateMembers
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void UpdateMembers()
		{
			int memberCount = SteamMatchmaking.GetNumLobbyMembers( _info.Id );
			_members.Clear();
			_steam64ToPeer.Clear();
			for ( int i = 0; i < memberCount; i++ ) {
				CSteamID userId = SteamMatchmaking.GetLobbyByIndex( i );
				PeerId peerId = new PeerId( Guid.NewGuid() );
				_steam64ToPeer[userId] = peerId;
				_members[peerId] = new SteamSessionPeer {
					Info = new LobbyMemberInfo {
						Id = peerId,
						DisplayName = SteamFriends.GetFriendPersonaName( userId ),
						Status = LobbyMemberState.Connected,
						IsOwner = _info.OwnerId == userId.m_SteamID,
						IsLocal = _info.OwnerId == SteamUser.GetSteamID().m_SteamID,
					},
					SteamId = userId,
					Connection = _netDriver.ConnectP2P( userId, 0 ),
					State = NetworkConnectionState.Connected,
					IsHost = _info.OwnerId == userId.m_SteamID,
					IsLocal = _info.OwnerId == SteamUser.GetSteamID().m_SteamID,
					Slot = (byte)_members.Count
				};
				_netDriver.BindPeer( peerId, userId );
			}
		}

		/*
		===============
		OnLobbyMemberStatusChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		private void OnLobbyMemberStatusChanged( LobbyChatUpdate_t pCallback )
		{
			CSteamID userChangedId = (CSteamID)pCallback.m_ulSteamIDUserChanged;
			switch ( (EChatMemberStateChange)pCallback.m_rgfChatMemberStateChange ) {
				case EChatMemberStateChange.k_EChatMemberStateChangeBanned:
				case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
				case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
				case EChatMemberStateChange.k_EChatMemberStateChangeKicked:
					_members.Remove( _steam64ToPeer[userChangedId] );
					break;
				case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
					PeerId peerId = new PeerId( Guid.NewGuid() );
					_steam64ToPeer[userChangedId] = peerId;

					_members[peerId] = new SteamSessionPeer {
						Info = new LobbyMemberInfo {
							Id = peerId,
							DisplayName = SteamFriends.GetFriendPersonaName( userChangedId ),
							Status = LobbyMemberState.Connected,
							IsOwner = _info.OwnerId == userChangedId.m_SteamID,
							IsLocal = _info.OwnerId == SteamUser.GetSteamID().m_SteamID,
						},
						SteamId = userChangedId,
						Connection = _netDriver.ConnectP2P( userChangedId, 0 ),
						State = NetworkConnectionState.Connected,
						IsHost = _info.OwnerId == userChangedId.m_SteamID,
						IsLocal = _info.OwnerId == SteamUser.GetSteamID().m_SteamID,
						Slot = (byte)_members.Count
					};
					_netDriver.BindPeer( peerId, userChangedId );
					break;
			}
		}
	};
};
