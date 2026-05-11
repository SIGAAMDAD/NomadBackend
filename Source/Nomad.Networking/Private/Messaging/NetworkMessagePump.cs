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

using Nomad.Networking.Messaging;
using Nomad.Networking.Diagnostics;
using Nomad.Networking.Private.Events;
using Nomad.Networking.Private.Rpc;
using Nomad.Networking.Transport;
using System;

namespace Nomad.Networking.Private.Messaging
{
	internal sealed class NetworkMessagePump : INetworkMessagePump
	{
		private const int MAX_PACKET_SIZE = 64 * 1024;

		private readonly INetworkTransport _transport;
		private readonly INetworkMessageRegistry _registry;
		private readonly NetworkRpcBus _rpcBus;
		private readonly NetworkEventBus _eventBus;
		private readonly INetworkDiagnostics? _diagnostics;
		private readonly byte[] _receiveBuffer = new byte[MAX_PACKET_SIZE];

		public NetworkMessagePump(
			INetworkTransport transport,
			INetworkMessageRegistry registry,
			NetworkRpcBus rpcBus,
			NetworkEventBus? eventBus = null,
			INetworkDiagnostics? diagnostics = null
		)
		{
			_transport = transport ?? throw new ArgumentNullException( nameof( transport ) );
			_registry = registry ?? throw new ArgumentNullException( nameof( registry ) );
			_rpcBus = rpcBus ?? throw new ArgumentNullException( nameof( rpcBus ) );
			_eventBus = eventBus;
			_diagnostics = diagnostics;
		}

		public void Pump()
		{
			while ( _transport.TryReceive( _receiveBuffer, out NetworkPacketInfo packet ) ) {
				_diagnostics?.RecordPacketReceived( packet.BytesWritten );

				ReadOnlySpan<byte> data = _receiveBuffer.AsSpan( 0, packet.BytesWritten );
				if ( !NetworkMessageEnvelope.TryRead( data, out ushort messageId, out ReadOnlySpan<byte> payload ) ) {
					_diagnostics?.RecordPacketDropped();
					continue;
				}

				if ( !_registry.TryGetInfo( messageId, out NetworkMessageInfo info ) ) {
					_diagnostics?.RecordUnknownMessageId();
					continue;
				}

				switch ( info.Kind ) {
					case NetworkMessageKind.Rpc:
						_rpcBus.Enqueue( packet.From, messageId, payload );
						break;
					case NetworkMessageKind.Event:
						_eventBus.Enqueue( packet.From, messageId, payload );
						break;
					default:
						_diagnostics?.RecordPacketDropped();
						break;
				}
			}

			_eventBus.Pump();
			_rpcBus.Pump();
		}
	}
}
