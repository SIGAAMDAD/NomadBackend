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
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Lobby;
using Nomad.OnlineServices.Steam.Private.Network;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamNetworkSessionService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamNetworkSessionService : INetworkSessionService
	{
		public bool IsSessionActive => _lobbyService.IsInLobby;
		public bool IsHost => _lobbyService.IsLobbyLeader;
		public bool IsClient => !IsHost;

		public NetworkSessionInfo? CurrentSession => _currentSession;
		private NetworkSessionInfo? _currentSession = null;

		public IGameEvent<EmptyEventArgs> SessionChanged {
			get {
				throw new NotImplementedException();
			}
		}

		public IGameEvent<PeerConnectedEventArgs> PeerConnected => _peerConnected;
		private readonly IGameEvent<PeerConnectedEventArgs> _peerConnected = default;

		public IGameEvent<PeerDisconnectedEventArgs> PeerDisconnected => _peerDisconnected;
		private readonly IGameEvent<PeerDisconnectedEventArgs> _peerDisconnected = default;

		private readonly Dictionary<PeerId, HSteamNetConnection> _peerToConnection = new();

		private readonly SteamLobbyService _lobbyService;
		private readonly SteamNetDriver _netDriver;

		public SteamNetworkSessionService( SteamLobbyService lobbyService, SteamNetDriver netDriver )
		{
			_lobbyService = lobbyService ?? throw new ArgumentNullException( nameof( lobbyService ) );
			_netDriver = netDriver ?? throw new ArgumentNullException( nameof( netDriver ) );

			_netDriver.ConnectionClosed += OnConnectionClosed;
			_netDriver.ConnectionEstablished += OnConnectionCreated;

			_lobbyService.LobbyJoined.Subscribe( OnLobbyJoined );
			_lobbyService.LobbyLeft.Subscribe( OnLobbyLeft );
			_lobbyService.LobbyStarted.Subscribe( OnLobbyStarted );
		}

		private void OnConnectionCreated( SteamNetConnection connection )
		{
		}

		private void OnConnectionClosed( SteamNetConnection connection )
		{
		}

		private void OnLobbyStarted( in LobbyStartResultEventArgs args )
		{
		}

		private void OnLobbyLeft( in LobbyLeaveResultEventArgs args )
		{
		}

		private void OnLobbyJoined( in LobbyJoinedResultEventArgs args )
		{
		}

		public void Broadcast( ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			var current = _lobbyService.ActiveLobby ?? throw new InvalidOperationException();

			for ( int i = 0; i < _currentSession.Peers.Count; i++ ) {
				if ( !current.Members.TryGetValue( _currentSession.Peers[i].PeerId, out var sessionPeer ) ) {
					continue;
				}
				if ( !_netDriver.TryGetConnection( sessionPeer.SteamId, out var connection ) ) {
					continue;
				}
				_netDriver.Send(
					connection,
					payload,
					NetworkPacketType.Payload,
					mode
				);
			}
		}

		public void SendToHost( ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			var current = _lobbyService.ActiveLobby;
			if ( current == null ) {
				return;
			}

			if ( !current.Members.TryGetValue( _currentSession.HostPeerId, out var sessionPeer ) ) {
				return;
			}
			if ( !_netDriver.TryGetConnection( sessionPeer.SteamId, out var connection ) ) {
				return;
			}
			_netDriver.Send(
				connection,
				payload,
				NetworkPacketType.Payload,
				mode
			);
		}

		public void SendToPeer( PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			if ( !_lobbyService.ActiveLobby.Members.TryGetValue( peerId, out var sessionPeer ) ) {
				return;
			}
			if ( !_netDriver.TryGetConnection( sessionPeer.SteamId, out var connection ) ) {
				return;
			}
			_netDriver.Send(
				connection,
				payload,
				NetworkPacketType.Payload,
				mode
			);
		}

		public async Task<bool> StartHostAsync( LobbyCreateInfo info, CancellationToken ct = default )
		{
			var lobby = await _lobbyService.CreateLobby( info, ct );
			if ( lobby.IsFailure ) {
				return false;
			}
			CreateSession( lobby.Lobby, true );
			return true;
		}

		public async Task<bool> JoinAsClientAsync( LobbyId id, CancellationToken ct = default )
		{
			var result = await _lobbyService.JoinLobby( id, ct );
			if ( !result.IsSuccess ) {
				return false;
			}
			CreateSession( result.LobbyData, false );
			return false;
		}

		public async Task StopAsync( CancellationToken ct = default )
		{
			await _lobbyService.LeaveLobby( ct );
		}

		public bool TryReceive( Span<byte> destination, out NetworkPacketInfo info )
		{
			info = new NetworkPacketInfo();
			foreach ( var connection in _peerToConnection ) {
			}
			return true;
		}

		private void CreateSession( LobbyInfo lobby, bool isHost )
		{
			_currentSession = new NetworkSessionInfo {
				SessionId = Guid.NewGuid(),
				Mode = isHost ? NetworkSessionMode.Host : NetworkSessionMode.Client,
				State = isHost ? NetworkConnectionState.StartingHost : NetworkConnectionState.Connecting,
				LobbyId = lobby.Id,
				PeerCount = lobby.PlayerCount
			};
		}
	};
};
