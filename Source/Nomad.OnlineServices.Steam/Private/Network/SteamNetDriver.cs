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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Network
{
	/*
	===================================================================================

	SteamNetDriver

	===================================================================================
	*/
	/// <summary>
	/// Provides the Steamworks P2P transport implementation used by Nomad networking.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This type owns the low-level SteamNetworkingSockets resources required for peer
	/// communication, including the listen socket, poll group receiver, connection
	/// repository, peer-to-Steam identity maps, and native connection status callback.
	/// </para>
	/// <para>
	/// The driver is intentionally limited to transport concerns. Lobby membership,
	/// gameplay session ownership, matchmaking state, and high-level peer lifecycle
	/// decisions are expected to be handled by higher-level online-service systems.
	/// </para>
	/// </remarks>

	internal sealed class SteamNetDriver : INetDriver
	{
		private const int DEFAULT_SEND_BUFFER_SIZE = 64 * 1024;
		private const int DEFAULT_RECV_BUFFER_SIZE = 64 * 1024;
		private const int DEFAULT_SEND_RATE_MIN = 64000;
		private const int DEFAULT_NAGLE_TIME = 0;
		private const int DEFAULT_TIMEOUT_INITIAL_MS = 100000;
		private const int DEFAULT_TIMEOUT_CONNECTED_MS = 1000000;

		public bool IsListening => _listenSocket != HSteamListenSocket.Invalid;
		public bool IsInitialized => _receiver.IsOpen;

		public event Action<NetConnection> ConnectionRequested;
		public event Action<NetConnection> ConnectionEstablished;
		public event Action<NetConnection> ConnectionClosed;

		private readonly Callback<SteamNetConnectionStatusChangedCallback_t> _netConnectionStatusChanged;

		private readonly SteamConnectionRepository _repository = new SteamConnectionRepository();
		private readonly Dictionary<PeerId, CSteamID> _peerToSteam = new();
		private readonly Dictionary<CSteamID, PeerId> _steamToPeer = new();
		private readonly ILoggerCategory _category;
		private readonly SteamNetworkPacketReceiver _receiver;

		private readonly SteamNetworkingConfigValue_t[] _socketOptions;

		private HSteamListenSocket _listenSocket = HSteamListenSocket.Invalid;

		private bool _isDisposed = false;

		/*
		===============
		SteamNetDriver
		===============
		*/
		/// <summary>
		/// Initializes a new Steam networking driver and registers the Steam connection
		/// status callback.
		/// </summary>
		/// <param name="eventFactory">
		/// Event registry service required by the network-driver abstraction. The current
		/// implementation validates the dependency even though event publication is handled
		/// through driver events.
		/// </param>
		/// <param name="category">
		/// Logger category used for transport diagnostics and send/receive errors.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="eventFactory"/> or <paramref name="category"/> is <see langword="null"/>.
		/// </exception>
		public SteamNetDriver( IGameEventRegistryService eventFactory, ILoggerCategory category )
		{
			ArgumentGuard.ThrowIfNull( eventFactory, nameof( eventFactory ) );

			_category = category ?? throw new ArgumentNullException( nameof( category ) );
			_receiver = new SteamNetworkPacketReceiver( _category );

			_netConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create( OnNetConnectionStatusChanged );
			_socketOptions = CreateSocketOptions();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Releases all Steam networking resources owned by this driver.
		/// </summary>
		/// <remarks>
		/// Disposal closes all known connections, closes the listen socket when active,
		/// disposes the packet receiver, unregisters the Steam callback, and suppresses
		/// finalization. Calling this method more than once is safe.
		/// </remarks>
		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			CloseAll( "SteamNetDriver disposed" );

			if ( _listenSocket != HSteamListenSocket.Invalid ) {
				SteamNetworkingSockets.CloseListenSocket( _listenSocket );
				_listenSocket = HSteamListenSocket.Invalid;
			}

			_receiver.Dispose();
			_netConnectionStatusChanged.Dispose();

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Listen
		===============
		*/
		/// <summary>
		/// Starts listening for incoming Steam P2P connections on the specified virtual port.
		/// </summary>
		/// <param name="virtualPort">Steam virtual port to listen on. The default value uses port <c>0</c>.</param>
		/// <returns><see langword="true"/> when a listen socket is active; otherwise, <see langword="false"/>.</returns>
		public bool Listen( int virtualPort = 0 )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );

			if ( _listenSocket != HSteamListenSocket.Invalid ) {
				return true;
			}

			SteamNetworkingUtils.InitRelayNetworkAccess();

			_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P(
				virtualPort,
				_socketOptions.Length,
				_socketOptions
			);

			return _listenSocket != HSteamListenSocket.Invalid;
		}

		/*
		===============
		BindPeer
		===============
		*/
		/// <summary>
		/// Associates a Nomad peer identifier with a Steam account identifier.
		/// </summary>
		/// <param name="peerId">Nomad peer identifier used by higher-level network APIs.</param>
		/// <param name="steamId">Steam account identifier that owns the underlying P2P connection.</param>
		/// <remarks>
		/// Existing bindings for either identifier are overwritten. The mapping is used for
		/// peer-based connect, accept, close, send, receive, and lookup operations.
		/// </remarks>
		public void BindPeer( PeerId peerId, CSteamID steamId )
		{
			_peerToSteam[peerId] = steamId;
			_steamToPeer[steamId] = peerId;
		}

		/*
		===============
		Connect
		===============
		*/
		/// <summary>
		/// Opens an outgoing P2P connection to a previously bound peer.
		/// </summary>
		/// <param name="peerId">Peer identifier that must already be bound to a Steam ID.</param>
		/// <param name="virtualPort">Steam virtual port to connect to. The default value uses port <c>0</c>.</param>
		/// <returns><see langword="true"/> when Steam returns a valid connection handle; otherwise, <see langword="false"/>.</returns>
		public bool Connect( PeerId peerId, int virtualPort = 0 )
		{
			if ( !_peerToSteam.TryGetValue( peerId, out CSteamID steamId ) ) {
				return false;
			}

			return ConnectP2P( steamId, virtualPort ) != HSteamNetConnection.Invalid;
		}

		/*
		===============
		ConnectP2P
		===============
		*/
		/// <summary>
		/// Opens an outgoing Steam P2P connection to the specified remote Steam account.
		/// </summary>
		/// <param name="remoteSteamId">Remote Steam account to connect to.</param>
		/// <param name="virtualPort">Steam virtual port to connect to. The default value uses port <c>0</c>.</param>
		/// <returns>
		/// A valid Steam networking connection handle when the connection was created and
		/// assigned to the receiver poll group; otherwise, <see cref="HSteamNetConnection.Invalid"/>.
		/// </returns>
		public HSteamNetConnection ConnectP2P( CSteamID remoteSteamId, int virtualPort = 0 )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );

			if ( !remoteSteamId.IsValid() ) {
				return HSteamNetConnection.Invalid;
			}

			SteamNetworkingUtils.InitRelayNetworkAccess();

			SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
			identity.SetSteamID( remoteSteamId );

			HSteamNetConnection handle = SteamNetworkingSockets.ConnectP2P(
				ref identity,
				virtualPort,
				_socketOptions.Length,
				_socketOptions
			);

			if ( handle == HSteamNetConnection.Invalid ) {
				return HSteamNetConnection.Invalid;
			}

			if ( !_receiver.OpenConnection( handle ) ) {
				SteamNetworkingSockets.CloseConnection(
					handle,
					0,
					"Failed to assign Steam poll group",
					false
				);
				return HSteamNetConnection.Invalid;
			}

			SteamNetConnection connection = new SteamNetConnection( handle, identity );
			connection.SetStatus( NetworkConnectionState.Connecting );

			_repository.Add( connection );

			return handle;
		}

		/*
		===============
		Accept
		===============
		*/
		/// <summary>
		/// Accepts a pending inbound Steam networking connection.
		/// </summary>
		/// <param name="handle">Steam networking connection handle to accept.</param>
		/// <returns><see langword="true"/> when the connection was accepted and assigned to the receiver; otherwise, <see langword="false"/>.</returns>
		public bool Accept( HSteamNetConnection handle )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );

			if ( handle == HSteamNetConnection.Invalid ) {
				return false;
			}

			if ( !_repository.TryGet( handle, out SteamNetConnection connection ) ) {
				return false;
			}

			EResult result = SteamNetworkingSockets.AcceptConnection( handle );
			if ( result != EResult.k_EResultOK ) {
				Close( connection, "AcceptConnection failed" );
				return false;
			}

			if ( !_receiver.OpenConnection( handle ) ) {
				Close( connection, "Failed to assign Steam poll group" );
				return false;
			}

			connection.SetStatus( NetworkConnectionState.Connecting );
			return true;
		}

		/// <summary>
		/// Accepts a pending inbound connection for a previously bound peer.
		/// </summary>
		/// <param name="peerId">Peer identifier whose bound Steam ID is used to find the pending connection.</param>
		/// <returns><see langword="true"/> when the peer is bound, the connection exists, and the Steam connection is accepted.</returns>
		public bool Accept( PeerId peerId )
		{
			if ( !_peerToSteam.TryGetValue( peerId, out CSteamID steamId ) ) {
				return false;
			}
			if ( !_repository.TryGet( steamId, out SteamNetConnection connection ) ) {
				return false;
			}
			return Accept( connection.Connection );
		}

		/*
		===============
		Close
		===============
		*/
		/// <summary>
		/// Closes a known Steam network connection.
		/// </summary>
		/// <param name="connection">Connection wrapper to close.</param>
		/// <param name="reason">Human-readable close reason passed to Steam.</param>
		/// <returns><see langword="true"/> when the repository contained and removed the connection; otherwise, <see langword="false"/>.</returns>
		public bool Close( SteamNetConnection connection, string reason )
		{
			if ( connection == null ) {
				return false;
			}
			return Close( connection.Connection, reason );
		}

		/*
		===============
		Close
		===============
		*/
		/// <summary>
		/// Closes a Steam networking connection by native handle.
		/// </summary>
		/// <param name="handle">Steam networking connection handle to close.</param>
		/// <param name="reason">Human-readable close reason passed to Steam. Empty values are replaced with <c>Closed</c>.</param>
		/// <returns><see langword="true"/> when the handle was tracked and removed; otherwise, <see langword="false"/>.</returns>
		public bool Close( HSteamNetConnection handle, string reason )
		{
			if ( handle == HSteamNetConnection.Invalid ) {
				return false;
			}

			bool hadConnection = _repository.TryGet( handle, out SteamNetConnection connection );

			if ( hadConnection ) {
				connection.SetStatus( NetworkConnectionState.Disconnected );
				_repository.Remove( handle );
				ConnectionClosed?.Invoke( ToNetConnection( connection ) );
			}

			SteamNetworkingSockets.CloseConnection(
				handle,
				0,
				string.IsNullOrEmpty( reason ) ? "Closed" : reason,
				false
			);

			return hadConnection;
		}

		/// <summary>
		/// Closes the tracked connection associated with a previously bound peer.
		/// </summary>
		/// <param name="peerId">Peer identifier whose bound Steam ID is used to find the connection.</param>
		/// <param name="reason">Human-readable close reason passed to Steam.</param>
		/// <returns><see langword="true"/> when a tracked connection was found and closed; otherwise, <see langword="false"/>.</returns>
		public bool Close( PeerId peerId, string reason )
		{
			if ( !_peerToSteam.TryGetValue( peerId, out CSteamID steamId ) ) {
				return false;
			}
			if ( !_repository.TryGet( steamId, out SteamNetConnection connection ) ) {
				return false;
			}
			return Close( connection, reason );
		}

		/*
		===============
		CloseAll
		===============
		*/
		/// <summary>
		/// Closes every connection currently tracked by the repository.
		/// </summary>
		/// <param name="reason">Human-readable close reason passed to Steam for each connection.</param>
		public void CloseAll( string reason )
		{
			SteamNetConnection[] snapshot = _repository.Snapshot();

			for ( int i = 0; i < snapshot.Length; i++ ) {
				Close( snapshot[i], reason );
			}

			_repository.Clear();
		}

		/*
		===============
		Send
		===============
		*/
		/// <summary>
		/// Sends a typed Nomad network packet over a Steam networking connection.
		/// </summary>
		/// <param name="connection">Steam connection that owns the native connection handle.</param>
		/// <param name="payload">Packet payload bytes to send. Payloads larger than <see cref="ushort.MaxValue"/> are rejected.</param>
		/// <param name="type">Nomad packet type written into the packet header.</param>
		/// <param name="mode">Requested reliability and latency behavior.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown when <paramref name="payload"/> exceeds the maximum packet payload size.
		/// </exception>
		public void Send( SteamNetConnection connection, ReadOnlySpan<byte> payload, NetworkPacketType type, NetworkSendMode mode )
		{
			if ( payload.Length > ushort.MaxValue ) {
				throw new ArgumentOutOfRangeException( nameof( payload ), "Steam network packets cannot exceed 65535 payload bytes." );
			}

			int packetLength = NetworkPacketHeader.SIZE + payload.Length;
			Span<byte> buffer = stackalloc byte[packetLength];
			NetworkPacketHeader header = new NetworkPacketHeader( type, 0, (ushort)mode, (ushort)payload.Length );

			header.WriteTo( buffer );
			payload.CopyTo( buffer.Slice( NetworkPacketHeader.SIZE ) );

			unsafe {
				fixed ( byte* ptr = buffer ) {
					EResult result = SteamNetworkingSockets.SendMessageToConnection( connection.Connection, (nint)ptr, (uint)packetLength, ConvertSendMode( mode ), out long messageNumber );
					if ( result != EResult.k_EResultOK ) {
						_category.PrintError( $"Couldn't send packet on connection: {result}" );
					}
				}
			}
		}

		/// <summary>
		/// Sends a payload packet to a previously bound peer.
		/// </summary>
		/// <param name="peerId">Peer identifier whose bound Steam ID is used to find the connection.</param>
		/// <param name="payload">Payload bytes to send.</param>
		/// <param name="mode">Requested reliability and latency behavior.</param>
		/// <returns><see langword="true"/> when the peer is bound and a tracked connection exists; otherwise, <see langword="false"/>.</returns>
		public bool Send( PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			if ( !_peerToSteam.TryGetValue( peerId, out CSteamID steamId ) ) {
				return false;
			}
			if ( !_repository.TryGet( steamId, out SteamNetConnection connection ) ) {
				return false;
			}

			Send( connection, payload, NetworkPacketType.Payload, mode );
			return true;
		}

		/*
		===============
		TryReceive
		===============
		*/
		/// <summary>
		/// Attempts to receive the next Steam packet into the provided destination buffer.
		/// </summary>
		/// <param name="destination">Buffer that receives packet payload bytes.</param>
		/// <param name="packet">Receives metadata for the packet when one is available.</param>
		/// <returns><see langword="true"/> when a packet was read; otherwise, <see langword="false"/>.</returns>
		public bool TryReceive( Span<byte> destination, out ReceivedNetworkPacket packet )
		{
			packet = default;
			return _receiver != null && _receiver.TryReceive( destination, out packet );
		}

		/// <summary>
		/// Attempts to receive the next packet and translate its Steam sender into a Nomad peer ID.
		/// </summary>
		/// <param name="destination">Buffer that receives packet payload bytes.</param>
		/// <param name="packet">Receives peer-facing packet metadata when a packet is available from a bound Steam ID.</param>
		/// <returns><see langword="true"/> when a packet was read and its sender is bound to a peer; otherwise, <see langword="false"/>.</returns>
		public bool TryReceive( Span<byte> destination, out NetworkPacketInfo packet )
		{
			packet = default;
			if ( !TryReceive( destination, out ReceivedNetworkPacket received ) ) {
				return false;
			}
			if ( !_steamToPeer.TryGetValue( received.SteamId, out PeerId peerId ) ) {
				return false;
			}

			packet = new NetworkPacketInfo( peerId, received.BytesWritten, received.Mode );
			return true;
		}

		/*
		===============
		TryGetConnection
		===============
		*/
		/// <summary>
		/// Attempts to retrieve a tracked Steam connection by native handle.
		/// </summary>
		/// <param name="handle">Steam networking connection handle to look up.</param>
		/// <param name="connection">Receives the matching connection when one is tracked.</param>
		/// <returns><see langword="true"/> when a matching connection exists; otherwise, <see langword="false"/>.</returns>
		public bool TryGetConnection( HSteamNetConnection handle, out SteamNetConnection connection )
		{
			return _repository.TryGet( handle, out connection );
		}

		/*
		===============
		TryGetConnection
		===============
		*/
		/// <summary>
		/// Attempts to retrieve a tracked Steam connection by remote Steam account ID.
		/// </summary>
		/// <param name="steamId">Remote Steam account identifier.</param>
		/// <param name="connection">Receives the matching connection when one is tracked.</param>
		/// <returns><see langword="true"/> when a matching connection exists; otherwise, <see langword="false"/>.</returns>
		public bool TryGetConnection( CSteamID steamId, out SteamNetConnection connection )
		{
			return _repository.TryGet( steamId, out connection );
		}

		/// <summary>
		/// Attempts to retrieve peer-facing connection information for a bound peer.
		/// </summary>
		/// <param name="peerId">Peer identifier to look up.</param>
		/// <param name="connection">Receives the peer-facing connection when found.</param>
		/// <returns><see langword="true"/> when the peer is bound and a connection is tracked; otherwise, <see langword="false"/>.</returns>
		public bool TryGetConnection( PeerId peerId, out NetConnection connection )
		{
			connection = default;
			if ( !_peerToSteam.TryGetValue( peerId, out CSteamID steamId ) ) {
				return false;
			}
			if ( !_repository.TryGet( steamId, out SteamNetConnection steamConnection ) ) {
				return false;
			}

			connection = ToNetConnection( steamConnection );
			return true;
		}

		/*
		===============
		Snapshot
		===============
		*/
		/// <summary>
		/// Creates a defensive snapshot of the currently tracked Steam connections.
		/// </summary>
		/// <returns>An array containing the connections known to the driver at the time of the call.</returns>
		public SteamNetConnection[] Snapshot()
		{
			return _repository.Snapshot();
		}

		/*
		===============
		OnNetConnectionStatusChanged
		===============
		*/
		/// <summary>
		/// Handles Steamworks connection-state notifications and translates them into
		/// Nomad connection state changes and lifecycle events.
		/// </summary>
		/// <param name="pCallback">Steamworks callback payload containing the connection handle and state information.</param>
		private void OnNetConnectionStatusChanged( SteamNetConnectionStatusChangedCallback_t pCallback )
		{
			HSteamNetConnection handle = pCallback.m_hConn;
			SteamNetConnectionInfo_t info = pCallback.m_info;
			ESteamNetworkingConnectionState state = info.m_eState;

			bool known = _repository.TryGet( handle, out SteamNetConnection connection );

			if ( !known ) {
				if ( !ShouldCreateConnectionForState( state ) ) {
					if ( IsTerminalState( state ) ) {
						SteamNetworkingSockets.CloseConnection(
							handle,
							0,
							"Unknown terminal connection",
							false
						);
					}

					return;
				}

				connection = new SteamNetConnection( handle, info.m_identityRemote );
				_repository.Add( connection );

				if ( state == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting ) {
					connection.SetStatus( NetworkConnectionState.Connecting );
					ConnectionRequested?.Invoke( ToNetConnection( connection ) );
					return;
				}
			}

			switch ( state ) {
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
					break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
					connection.SetStatus( NetworkConnectionState.Connecting );
					Accept( connection.Connection );
					break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
					connection.SetStatus( NetworkConnectionState.Connected );
					ConnectionEstablished?.Invoke( ToNetConnection( connection ) );
					break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Dead:
					connection.SetStatus( NetworkConnectionState.Disconnected );
					_repository.Remove( handle );
					ConnectionClosed?.Invoke( ToNetConnection( connection ) );

					SteamNetworkingSockets.CloseConnection(
						handle,
						0,
						"Connection closed",
						false
					);
					break;

				default:
					break;
			}
		}

		/*
		===============
		ShouldCreateConnectionForState
		===============
		*/
		/// <summary>
		/// Determines whether an unknown Steam connection state should create a repository entry.
		/// </summary>
		/// <param name="state">Steam networking connection state reported by Steamworks.</param>
		/// <returns><see langword="true"/> for active or potentially active states; otherwise, <see langword="false"/>.</returns>
		private static bool ShouldCreateConnectionForState( ESteamNetworkingConnectionState state )
		{
			switch ( state ) {
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
					return true;
				default:
					return false;
			}
		}

		/*
		===============
		IsTerminalState
		===============
		*/
		/// <summary>
		/// Determines whether a Steam connection state represents a terminal state.
		/// </summary>
		/// <param name="state">Steam networking connection state reported by Steamworks.</param>
		/// <returns><see langword="true"/> when Steam considers the connection closed or dead; otherwise, <see langword="false"/>.</returns>
		private static bool IsTerminalState( ESteamNetworkingConnectionState state )
		{
			switch ( state ) {
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Dead:
					return true;
				default:
					return false;
			}
		}

		/*
		===============
		ConvertSendMode
		===============
		*/
		/// <summary>
		/// Converts a Nomad send mode into the corresponding SteamNetworkingSockets send flag.
		/// </summary>
		/// <param name="mode">Nomad send mode to convert.</param>
		/// <returns>The Steamworks send flag matching <paramref name="mode"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode"/> is not supported.</exception>
		private static int ConvertSendMode( NetworkSendMode mode )
		{
			switch ( mode ) {
				case NetworkSendMode.Reliable:
					return Steamworks.Constants.k_nSteamNetworkingSend_Reliable;
				case NetworkSendMode.Unreliable:
					return Steamworks.Constants.k_nSteamNetworkingSend_Unreliable;
				case NetworkSendMode.UnreliableNoDelay:
					return Steamworks.Constants.k_nSteamNetworkingSend_UnreliableNoDelay;
				default:
					throw new ArgumentOutOfRangeException( nameof( mode ) );
			}
		}

		/// <summary>
		/// Converts an internal Steam connection wrapper into the public peer-facing connection value.
		/// </summary>
		/// <param name="connection">Internal Steam connection to convert.</param>
		/// <returns>A peer-facing connection containing the bound peer ID, when known, and current status.</returns>
		private NetConnection ToNetConnection( SteamNetConnection connection )
		{
			PeerId peerId = default;
			if ( connection.RemoteSteamId.HasValue ) {
				_steamToPeer.TryGetValue( connection.RemoteSteamId.Value, out peerId );
			}

			return new NetConnection( peerId, connection.Status );
		}

		/*
		===============
		CreateSocketOptions
		===============
		*/
		/// <summary>
		/// Builds the SteamNetworkingSockets configuration used for listen and outgoing P2P sockets.
		/// </summary>
		/// <returns>An array of Steam networking configuration values for buffering, send rate, Nagle behavior, and timeouts.</returns>
		private static SteamNetworkingConfigValue_t[] CreateSocketOptions()
		{
			return new SteamNetworkingConfigValue_t[] {
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue {
						m_int32 = DEFAULT_SEND_BUFFER_SIZE
					}
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_RecvBufferSize,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue {
						m_int32 = DEFAULT_RECV_BUFFER_SIZE
					}
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue {
						m_int32 = DEFAULT_SEND_RATE_MIN
					}
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_NagleTime,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue {
						m_int32 = DEFAULT_NAGLE_TIME
					}
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutInitial,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue {
						m_int32 = DEFAULT_TIMEOUT_INITIAL_MS
					}
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue {
						m_int32 = DEFAULT_TIMEOUT_CONNECTED_MS
					}
				}
			};
		}
	};
};
