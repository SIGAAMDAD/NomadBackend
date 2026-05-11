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
using Nomad.Networking.Driver;
using Nomad.Networking.Messaging;
using Nomad.Networking.Session;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Private.Session
{
	internal sealed class NetworkSessionService : INetworkSessionService, IDisposable
	{
		private readonly ILobbyService _lobbyService;
		private readonly INetDriver _netDriver;

		private readonly IGameEvent<NetworkSessionChangedEventArgs> _sessionChanged;
		private readonly IGameEvent<PeerConnectedEventArgs> _peerConnected;
		private readonly IGameEvent<PeerDisconnectedEventArgs> _peerDisconnected;

		private NetworkSessionInfo? _currentSession;
		private bool _isDisposed;

		public bool IsSessionActive => _currentSession != null;
		public bool IsHost => _currentSession?.Mode == NetworkSessionMode.Host;
		public bool IsClient => _currentSession?.Mode == NetworkSessionMode.Client;
		public NetworkSessionInfo? CurrentSession => _currentSession;

		public IGameEvent<NetworkSessionChangedEventArgs> SessionChanged => _sessionChanged;
		public IGameEvent<PeerConnectedEventArgs> PeerConnected => _peerConnected;
		public IGameEvent<PeerDisconnectedEventArgs> PeerDisconnected => _peerDisconnected;

		public NetworkSessionService( ILobbyService lobbyService, INetDriver netDriver, IGameEventRegistryService eventFactory )
		{
			_lobbyService = lobbyService ?? throw new ArgumentNullException( nameof( lobbyService ) );
			_netDriver = netDriver ?? throw new ArgumentNullException( nameof( netDriver ) );
			if ( eventFactory == null ) {
				throw new ArgumentNullException( nameof( eventFactory ) );
			}

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

			_lobbyService.LobbyJoined.Subscribe( OnLobbyJoined );
			_lobbyService.LobbyLeft.Subscribe( OnLobbyLeft );
			_lobbyService.LobbyStarted.Subscribe( OnLobbyStarted );

			_netDriver.ConnectionEstablished += OnConnectionEstablished;
			_netDriver.ConnectionClosed += OnConnectionClosed;
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			_lobbyService.LobbyJoined.Unsubscribe( OnLobbyJoined );
			_lobbyService.LobbyLeft.Unsubscribe( OnLobbyLeft );
			_lobbyService.LobbyStarted.Unsubscribe( OnLobbyStarted );

			_netDriver.ConnectionEstablished -= OnConnectionEstablished;
			_netDriver.ConnectionClosed -= OnConnectionClosed;

			_sessionChanged.Dispose();
			_peerConnected.Dispose();
			_peerDisconnected.Dispose();

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		public async Task<bool> StartHostAsync( LobbyCreateInfo info, CancellationToken ct = default )
		{
			if ( !_netDriver.Listen() ) {
				return false;
			}

			LobbyCreateResult result = await _lobbyService.CreateLobby( info, ct );
			if ( result.IsFailure || _lobbyService.Current == null ) {
				return false;
			}

			CreateSession( _lobbyService.Current, NetworkSessionMode.Host, NetworkConnectionState.StartingHost );
			PublishSessionChanged();
			return true;
		}

		public async Task<bool> JoinAsClientAsync( LobbyId lobbyId, CancellationToken ct = default )
		{
			LobbyJoinResult result = await _lobbyService.JoinLobby( lobbyId, ct );
			if ( !result.IsSuccess || _lobbyService.Current == null ) {
				return false;
			}

			CreateSession( _lobbyService.Current, NetworkSessionMode.Client, NetworkConnectionState.Connecting );
			if ( _currentSession != null ) {
				_netDriver.Connect( _currentSession.HostPeerId );
			}

			PublishSessionChanged();
			return true;
		}

		public async Task StopAsync( CancellationToken ct = default )
		{
			_netDriver.CloseAll( "Network session stopped" );
			await _lobbyService.LeaveLobby( ct );
			PublishSessionChanged();
			_currentSession = null;
		}

		public void SendToHost( ReadOnlySpan<byte> payload, NetworkSendMode mode = NetworkSendMode.Unreliable )
		{
			if ( _currentSession == null ) {
				return;
			}

			_netDriver.Send( _currentSession.HostPeerId, payload, mode );
		}

		public void SendToPeer( PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode = NetworkSendMode.Unreliable )
		{
			_netDriver.Send( peerId, payload, mode );
		}

		public void Broadcast( ReadOnlySpan<byte> payload, NetworkSendMode mode = NetworkSendMode.Unreliable )
		{
			if ( _currentSession == null ) {
				return;
			}

			for ( int i = 0; i < _currentSession.Peers.Count; i++ ) {
				PeerId peerId = _currentSession.Peers[i].PeerId;
				if ( peerId.Equals( _currentSession.LocalPeerId ) ) {
					continue;
				}
				_netDriver.Send( peerId, payload, mode );
			}
		}

		public bool TryReceive( Span<byte> destination, out NetworkPacketInfo info )
		{
			return _netDriver.TryReceive( destination, out info );
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
		/// <param name="mode"></param>
		/// <param name="state"></param>
		private void CreateSession( LobbyInfo lobby, NetworkSessionMode mode, NetworkConnectionState state )
		{
			IReadOnlyList<LobbyMemberInfo> members = _lobbyService.GetMembers();
			var peers = new List<NetworkPeerInfo>( members.Count );
			PeerId localPeerId = default;
			PeerId hostPeerId = default;

			for ( int i = 0; i < members.Count; i++ ) {
				LobbyMemberInfo member = members[i];
				if ( member.IsLocal ) {
					localPeerId = member.Id;
				}
				if ( member.IsOwner ) {
					hostPeerId = member.Id;
				}

				peers.Add(
					new NetworkPeerInfo(
						member.Id,
						member.DisplayName ?? string.Empty,
						member.IsOwner,
						member.IsLocal,
						true,
						i,
						NetworkConnectionState.Connected
					)
				);
			}

			_currentSession = new NetworkSessionInfo {
				SessionId = Guid.NewGuid(),
				LobbyId = lobby.Id,
				Mode = mode,
				State = state,
				MinPlayers = 1,
				MaxPlayers = lobby.MaxPlayers,
				PeerCount = peers.Count,
				LocalPeerId = localPeerId,
				HostPeerId = hostPeerId,
				Peers = peers,
				LastUpdatedUtc = DateTime.UtcNow
			};
		}

		/*
		===============
		PublishSessionChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void PublishSessionChanged()
		{
			if ( _currentSession == null ) {
				return;
			}

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
		OnConnectionEstablished
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="connection"></param>
		private void OnConnectionEstablished( NetConnection connection )
		{
			_peerConnected.Publish( new PeerConnectedEventArgs( connection.PeerId ) );
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
		private void OnConnectionClosed( NetConnection connection )
		{
			_peerDisconnected.Publish( new PeerDisconnectedEventArgs( connection.PeerId, LobbyLeaveReason.Disconnected ) );
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
			PublishSessionChanged();
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
			PublishSessionChanged();
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
			PublishSessionChanged();
		}
	};
};
