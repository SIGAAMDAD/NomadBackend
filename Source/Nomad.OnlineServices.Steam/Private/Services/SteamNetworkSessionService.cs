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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Repositories;
using Nomad.OnlineServices.Steam.Services.NetworkServices;

namespace Nomad.OnlineServices.Steam.Private.Services
{
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

		public IGameEvent<PeerId> PeerConnected {
			get {
				throw new NotImplementedException();
			}
		}

		public IGameEvent<PeerId> PeerDisconnected {
			get {
				throw new NotImplementedException();
			}
		}

		private readonly ILobbyService _lobbyService;
		private readonly SteamLobbyRepository _lobbyRepository;
		private readonly SteamNetworkPacketSender _packetSender;
		private readonly SteamNetworkPacketReceiver _packetReceiver;

		public SteamNetworkSessionService( ILobbyService lobbyService, SteamLobbyRepository repository, SteamConnectionRepository connectionRepository )
		{
			_lobbyService = lobbyService ?? throw new ArgumentNullException( nameof( lobbyService ) );
			_lobbyRepository = repository ?? throw new ArgumentNullException( nameof( repository ) );


			_lobbyService.LobbyJoined.Subscribe( OnLobbyJoined );
			_lobbyService.LobbyLeft.Subscribe( OnLobbyLeft );
			_lobbyService.LobbyStarted.Subscribe( OnLobbyStarted );
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

		public async ValueTask BroadcastAsync<TMessage>( TMessage message, CancellationToken ct = default )
			where TMessage : struct
		{
		}

		public async ValueTask SendToHostAsync<TMessage>( TMessage message, CancellationToken ct = default )
			where TMessage : struct
		{
		}

		public async ValueTask SendToPeerAsync<TMessage>( PeerId peerId, TMessage message, CancellationToken ct = default )
			where TMessage : struct
		{
		}

		public async Task<bool> StartHostAsync( LobbyInfo info, CancellationToken ct = default )
		{
			var lobby = await _lobbyService.CreateLobby( info, ct );
			if ( lobby == Guid.Empty ) {
				return false;
			}
			_currentSession = new NetworkSessionInfo {
				SessionId = lobby,
				Mode = NetworkSessionMode.Host,
				State = NetworkConnectionState.StartingHost,
			};
			return true;
		}

		public async Task<bool> JoinAsClientAsync( Guid id, CancellationToken ct = default )
		{
			var result = await _lobbyService.JoinLobby( id, ct );
			if ( !result ) {
				return false;
			}
			_currentSession = new NetworkSessionInfo { };
			return false;
		}

		private void CreateSession( Guid lobbyId, bool isHost )
		{
			_currentSession = new NetworkSessionInfo {
				SessionId = Guid.NewGuid(),
				Mode = isHost ? NetworkSessionMode.Host : NetworkSessionMode.Client,
				State = isHost ? NetworkConnectionState.StartingHost : NetworkConnectionState.Connecting,
			};
		}

		public async Task StopAsync( CancellationToken ct = default )
		{
			await _lobbyService.LeaveLobby( ct );
		}
	};
};
