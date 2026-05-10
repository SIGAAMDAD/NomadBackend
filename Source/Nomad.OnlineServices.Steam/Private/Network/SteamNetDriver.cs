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
	/// <para>Owns the raw SteamNetworkingSockets transport state:</para>
	/// <para>* listen socket</para>
	/// <para>* poll group</para>
	/// <para>* connection handles</para>
	/// <para>* connection status callback</para>
	///
	/// <para>This class does not own lobby state or gameplay peer/session state.</para>
	/// </summary>

	internal sealed class SteamNetDriver : IDisposable
	{
		private const int DEFAULT_SEND_BUFFER_SIZE = 64 * 1024;
		private const int DEFAULT_RECV_BUFFER_SIZE = 64 * 1024;
		private const int DEFAULT_SEND_RATE_MIN = 64000;
		private const int DEFAULT_NAGLE_TIME = 0;
		private const int DEFAULT_TIMEOUT_INITIAL_MS = 100000;
		private const int DEFAULT_TIMEOUT_CONNECTED_MS = 1000000;

		public bool IsListening => _listenSocket != HSteamListenSocket.Invalid;
		public bool IsInitialized => _receiver.IsOpen;

		public event Action<SteamNetConnection> ConnectionRequested;
		public event Action<SteamNetConnection> ConnectionEstablished;
		public event Action<SteamNetConnection> ConnectionClosed;

		private readonly Callback<SteamNetConnectionStatusChangedCallback_t> _netConnectionStatusChanged;

		private readonly SteamConnectionRepository _repository = new SteamConnectionRepository();
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
		///
		/// </summary>
		/// <param name="eventFactory"></param>
		/// <exception cref="ArgumentNullException"></exception>
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
		///
		/// </summary>
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
		///
		/// </summary>
		/// <param name="virtualPort"></param>
		/// <returns></returns>
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
		ConnectP2P
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="remoteSteamId"></param>
		/// <param name="virtualPort"></param>
		/// <returns></returns>
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
		///
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
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

		/*
		===============
		Close
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
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
		///
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		public bool Close( HSteamNetConnection handle, string reason )
		{
			if ( handle == HSteamNetConnection.Invalid ) {
				return false;
			}

			bool hadConnection = _repository.TryGet( handle, out SteamNetConnection connection );

			if ( hadConnection ) {
				connection.SetStatus( NetworkConnectionState.Disconnected );
				_repository.Remove( handle );
				ConnectionClosed( connection );
			}

			SteamNetworkingSockets.CloseConnection(
				handle,
				0,
				string.IsNullOrEmpty( reason ) ? "Closed" : reason,
				false
			);

			return hadConnection;
		}

		/*
		===============
		CloseAll
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="reason"></param>
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
		///
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="payload"></param>
		/// <param name="type"></param>
		/// <param name="mode"></param>
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

		/*
		===============
		TryReceive
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool TryReceive( Span<byte> destination, out ReceivedNetworkPacket packet )
		{
			packet = default;
			return _receiver != null && _receiver.TryReceive( destination, out packet );
		}

		/*
		===============
		TryGetConnection
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="connection"></param>
		/// <returns></returns>
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
		///
		/// </summary>
		/// <param name="steamId"></param>
		/// <param name="connection"></param>
		/// <returns></returns>
		public bool TryGetConnection( CSteamID steamId, out SteamNetConnection connection )
		{
			return _repository.TryGet( steamId, out connection );
		}

		/*
		===============
		Snapshot
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
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
		///
		/// </summary>
		/// <param name="pCallback"></param>
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
					ConnectionRequested?.Invoke( connection );
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
					ConnectionEstablished?.Invoke( connection );
					break;

				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Dead:
					connection.SetStatus( NetworkConnectionState.Disconnected );
					_repository.Remove( handle );
					ConnectionClosed?.Invoke( connection );

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
		///
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
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
		///
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
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
		///
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
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

		/*
		===============
		CreateSocketOptions
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
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
