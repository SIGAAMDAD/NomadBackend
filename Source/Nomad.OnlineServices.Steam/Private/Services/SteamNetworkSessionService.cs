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
using Nomad.Core.Compatibility.Guards;
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

	internal sealed class SteamNetworkSessionService : INetworkSessionService, IDisposable
	{
		public bool IsSessionActive => _currentSession != null;
		public bool IsHost => _currentSession.Mode == NetworkSessionMode.Client;
		public bool IsClient => _currentSession.Mode == NetworkSessionMode.Host;

		public NetworkSessionInfo? CurrentSession => _currentSession;
		private NetworkSessionInfo? _currentSession = null;

		public IGameEvent<NetworkSessionChangedEventArgs> SessionChanged => _sessionChanged;
		private readonly IGameEvent<NetworkSessionChangedEventArgs> _sessionChanged = default;

		public IGameEvent<PeerConnectedEventArgs> PeerConnected => _peerConnected;
		private readonly IGameEvent<PeerConnectedEventArgs> _peerConnected = default;

		public IGameEvent<PeerDisconnectedEventArgs> PeerDisconnected => _peerDisconnected;
		private readonly IGameEvent<PeerDisconnectedEventArgs> _peerDisconnected = default;

		private readonly SteamLobbyService _lobbyService;
		private readonly SteamNetDriver _netDriver;
		private readonly SteamDataCache _steamData;

		private bool _isDisposed = false;

		/*
		===============
		SteamNetworkSessionService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobbyService"></param>
		/// <param name="netDriver"></param>
		/// <param name="eventFactory"></param>
		/// <param name="steamData"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamNetworkSessionService(
			SteamLobbyService lobbyService,
			SteamNetDriver netDriver,
			IGameEventRegistryService eventFactory,
			SteamDataCache steamData
		)
		{
			ArgumentGuard.ThrowIfNull( eventFactory, nameof( eventFactory ) );

			_lobbyService = lobbyService ?? throw new ArgumentNullException( nameof( lobbyService ) );
			_netDriver = netDriver ?? throw new ArgumentNullException( nameof( netDriver ) );
			_steamData = steamData ?? throw new ArgumentNullException( nameof( steamData ) );

			_netDriver.ConnectionClosed += OnConnectionClosed;
			_netDriver.ConnectionEstablished += OnConnectionCreated;

			_lobbyService.LobbyJoined.Subscribe( OnLobbyJoined );
			_lobbyService.LobbyLeft.Subscribe( OnLobbyLeft );
			_lobbyService.LobbyStarted.Subscribe( OnLobbyStarted );

			_sessionChanged = eventFactory.GetEvent<NetworkSessionChangedEventArgs>(
				NetworkSessionChangedEventArgs.Name,
				NetworkSessionChangedEventArgs.NameSpace
			);

			_peerConnected = eventFactory.GetEvent<PeerConnectedEventArgs>(
				PeerConnectedEventArgs.Name,
				PeerConnectedEventArgs.NameSpace
			);

			_peerDisconnected = eventFactory.GetEvent<PeerDisconnectedEventArgs>(
				PeerDisconnectedEventArgs.Name,
				PeerDisconnectedEventArgs.NameSpace
			);
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
			if ( _isDisposed ) {
				return;
			}

			_peerConnected.Dispose();
			_peerDisconnected.Dispose();
			_sessionChanged.Dispose();

			_netDriver.ConnectionClosed -= OnConnectionClosed;
			_netDriver.ConnectionEstablished -= OnConnectionCreated;

			_lobbyService.LobbyJoined.Unsubscribe( OnLobbyJoined );
			_lobbyService.LobbyLeft.Unsubscribe( OnLobbyLeft );
			_lobbyService.LobbyStarted.Unsubscribe( OnLobbyStarted );

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Broadcast
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <exception cref="InvalidOperationException"></exception>
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

		/*
		===============
		SendToHost
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
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

		/*
		===============
		SendToPeer
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="peerId"></param>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
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

		/*
		===============
		StartHostAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="info"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<bool> StartHostAsync( LobbyCreateInfo info, CancellationToken ct = default )
		{
			var lobby = await _lobbyService.CreateLobby( info, ct );
			if ( lobby.IsFailure ) {
				return false;
			}
			CreateSession( lobby.Lobby, true );
			return true;
		}

		/*
		===============
		JoinAsClientAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<bool> JoinAsClientAsync( LobbyId id, CancellationToken ct = default )
		{
			var result = await _lobbyService.JoinLobby( id, ct );
			if ( !result.IsSuccess ) {
				return false;
			}
			CreateSession( result.LobbyData, false );
			return false;
		}

		/*
		===============
		StopAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task StopAsync( CancellationToken ct = default )
		{
			await _lobbyService.LeaveLobby( ct );
		}

		/*
		===============
		TryReceive
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public bool TryReceive( Span<byte> destination, out NetworkPacketInfo info )
		{
			info = default;

			var current = _lobbyService.ActiveLobby;
			if ( current == null ) {
				return false;
			}

			if ( !_netDriver.TryReceive( destination, out ReceivedNetworkPacket packet ) ) {
				return false;
			}

			foreach ( var member in current.Members ) {
				if ( member.Value.SteamId.m_SteamID != packet.SteamId.m_SteamID ) {
					continue;
				}

				info = new NetworkPacketInfo(
					member.Key,
					packet.Payload.Length,
					packet.Mode
				);
				return true;
			}

			return false;
		}

		/*
		===============
		CreateSession
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="lobby"></param>
		/// <param name="isHost"></param>
		private void CreateSession( LobbyInfo lobby, bool isHost )
		{
			var current = _lobbyService.ActiveLobby ?? throw new InvalidOperationException();
			if ( !current.TryGetMember( (CSteamID)current.Info.OwnerId, out var hostId ) ) {
				return;
			}
			if ( !current.TryGetMember( _steamData.LocalUserId, out var localId ) ) {
				return;
			}

			var peers = new List<NetworkPeerInfo>( current.Members.Count );
			foreach ( var peer in current.Members ) {
				peers.Add(
					new NetworkPeerInfo(
						peerId: peer.Value.Info.Id,
						displayName: peer.Value.Info.DisplayName,
						isHost: peer.Value.IsHost,
						isLocal: peer.Value.IsLocal,
						isReady: true,
						playerSlot: peer.Value.Slot,
						state: peer.Value.State
					)
				);
			}

			_currentSession = new NetworkSessionInfo {
				SessionId = Guid.NewGuid(),
				Mode = isHost ? NetworkSessionMode.Host : NetworkSessionMode.Client,
				State = isHost ? NetworkConnectionState.StartingHost : NetworkConnectionState.Connecting,
				LobbyId = lobby.Id,
				PeerCount = lobby.PlayerCount,
				LocalPeerId = localId,
				HostPeerId = hostId,
				MinPlayers = 1,
				MaxPlayers = lobby.MaxPlayers,
				Peers = peers,
				LastUpdatedUtc = DateTime.UtcNow
			};
		}

		/*
		===============
		OnConnectionCreated
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="connection"></param>
		private void OnConnectionCreated( SteamNetConnection connection )
		{
			var current = _lobbyService.ActiveLobby ?? throw new InvalidOperationException();
			if ( !current.TryGetMember( connection.RemoteSteamId.Value, out PeerId peerId ) ) {
				return;
			}
			_peerConnected.Publish(
				new PeerConnectedEventArgs( peerId )
			);
		}

		/*
		===============
		OnConnectionClosed
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="connection"></param>
		private void OnConnectionClosed( SteamNetConnection connection )
		{
			var current = _lobbyService.ActiveLobby ?? throw new InvalidOperationException();
			if ( !current.TryGetMember( connection.RemoteSteamId.Value, out PeerId peerId ) ) {
				return;
			}
			var reason = connection.Status switch {
				NetworkConnectionState.Disconnected => LobbyLeaveReason.Leave,
				NetworkConnectionState.Faulted => LobbyLeaveReason.Disconnected,
				NetworkConnectionState.Kicked => LobbyLeaveReason.Kicked,
				_ => throw new IndexOutOfRangeException( nameof( connection.Status ) )
			};
			_peerDisconnected.Publish(
				new PeerDisconnectedEventArgs( peerId, reason )
			);
		}

		/*
		===============
		OnLobbyStarted
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnLobbyStarted( in LobbyStartResultEventArgs args )
		{
			_sessionChanged.Publish(
				new NetworkSessionChangedEventArgs(
					sessionId: _currentSession.SessionId,
					lobbyId: _currentSession.LobbyId,
					mode: _currentSession.Mode,
					localPeerId: _currentSession.LocalPeerId,
					hostPeerId: _currentSession.HostPeerId
				)
			);
		}

		/*
		===============
		OnLobbyLeft
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnLobbyLeft( in LobbyLeaveResultEventArgs args )
		{
			_sessionChanged.Publish(
				new NetworkSessionChangedEventArgs(
					sessionId: _currentSession.SessionId,
					lobbyId: _currentSession.LobbyId,
					mode: _currentSession.Mode,
					localPeerId: _currentSession.LocalPeerId,
					hostPeerId: _currentSession.HostPeerId
				)
			);
		}

		/*
		===============
		OnLobbyJoined
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnLobbyJoined( in LobbyJoinedResultEventArgs args )
		{
			_sessionChanged.Publish(
				new NetworkSessionChangedEventArgs(
					sessionId: _currentSession.SessionId,
					lobbyId: _currentSession.LobbyId,
					mode: _currentSession.Mode,
					localPeerId: _currentSession.LocalPeerId,
					hostPeerId: _currentSession.HostPeerId
				)
			);
		}
	};
};
