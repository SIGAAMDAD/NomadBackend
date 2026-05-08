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
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Repositories;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services.LobbyServices
{
	internal sealed class SteamConnectionService : IDisposable
	{
		private readonly Callback<SteamNetConnectionStatusChangedCallback_t> _netConnectionStatusChanged;

		private readonly SteamConnectionRepository _connections;

		private readonly IGameEventRegistryService _eventFactory;

		private HSteamNetPollGroup _pollGroup = HSteamNetPollGroup.Invalid;
		private HSteamListenSocket _listenSocket = HSteamListenSocket.Invalid;
		private readonly SteamNetworkingConfigValue_t[] _socketOptions;

		public event Action<SteamNetConnection> ConnectionRequested;
		public event Action<SteamNetConnection> ConnectionEstablished;
		public event Action<SteamNetConnection> ConnectionClosed;

		private bool _isDisposed = false;

		/*
		===============
		SteamConnectionService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		public SteamConnectionService( ILoggerService logger, IGameEventRegistryService eventFactory )
		{
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
			_connections = new SteamConnectionRepository( logger );

			_netConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create( OnConnectionStatusChanged );

			_socketOptions = new SteamNetworkingConfigValue_t[] {
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 64 * 1024 }
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendRateMin,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 64000 }
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_RecvBufferSize,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 64 * 1024 }
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_NagleTime,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 0 }
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutInitial,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 100000 }
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutConnected,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 1000000 }
				},
				new SteamNetworkingConfigValue_t {
					m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SymmetricConnect,
					m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
					m_val = new SteamNetworkingConfigValue_t.OptionValue { m_int32 = 1 }
				}
			};
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
				foreach ( var connection in _connections.Snapshot() ) {
					SteamNetworkingSockets.CloseConnection( connection.Connection, 0, "Shutdown", false );
				}
				if ( _listenSocket != HSteamListenSocket.Invalid ) {
					SteamNetworkingSockets.CloseListenSocket( _listenSocket );
					_listenSocket = HSteamListenSocket.Invalid;
				}
				if ( _pollGroup != HSteamNetPollGroup.Invalid ) {
					SteamNetworkingSockets.DestroyPollGroup( _pollGroup );
					_pollGroup = HSteamNetPollGroup.Invalid;
				}
				_netConnectionStatusChanged?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		public void StartHosting( int virtualPort = 0 )
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
			_pollGroup = SteamNetworkingSockets.CreatePollGroup();
			_listenSocket = SteamNetworkingSockets.CreateListenSocketP2P( virtualPort, _socketOptions.Length, _socketOptions );
		}

		public HSteamNetConnection ConnectP2P( CSteamID remoteSteamId, int virtualPort = 0 )
		{
			var identity = new SteamNetworkingIdentity();
			identity.SetSteamID( remoteSteamId );

			var conn = SteamNetworkingSockets.ConnectP2P( ref identity, virtualPort, 0, null );
			if ( conn == HSteamNetConnection.Invalid ) {
				return conn;
			}

			var session = new SteamNetConnection( conn, identity );
			_connections.Add( session );
			return conn;
		}

		public bool Accept( HSteamNetConnection handle )
		{
			if ( !_connections.TryGet( handle, out var connection ) || connection == null ) {
				return false;
			}

			var result = SteamNetworkingSockets.AcceptConnection( connection.Connection );
			if ( result != EResult.k_EResultOK ) {
				return false;
			}

			SteamNetworkingSockets.SetConnectionPollGroup( handle, _pollGroup );
			return true;
		}

		private void OnConnectionStatusChanged( SteamNetConnectionStatusChangedCallback_t pCallback )
		{
			var handle = pCallback.m_hConn;
			var info = pCallback.m_info;

			switch ( info.m_eState ) {
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
					if ( !_connections.TryGet( handle, out var existing ) || existing == null ) {
						var inbound = new SteamNetConnection(
							handle,
							info.m_identityRemote
						);
						_connections.Add( inbound );
						ConnectionRequested?.Invoke( inbound );
					} else {
						existing.SetStatus( NetworkConnectionState.Connected );
					}
					break;
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected: {
						if ( _connections.TryGet( handle, out var connection ) && connection != null ) {
							connection.SetStatus( NetworkConnectionState.Connected );
							ConnectionEstablished?.Invoke( connection );
						}
						break;
					}
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
				case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally: {
						if ( _connections.TryGet( handle, out var connection ) && connection != null ) {
							bool localProblem = info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally;
							connection.SetStatus( NetworkConnectionState.Disconnected );
							ConnectionClosed?.Invoke( connection );
						}
						SteamNetworkingSockets.CloseConnection( handle, 0, "Cleanup", false );
						_connections.Remove( handle );
						break;
					}
				default:
					throw new ArgumentOutOfRangeException( nameof( info.m_eState ) );
			}
		}
	};
};
