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
using Nomad.Networking.Diagnostics;
using Nomad.Networking.Rpc;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Private.Rpc
{
	internal sealed class RpcInvoker<TRpc> : IRpcInvoker
		where TRpc : struct
	{
		private readonly NetworkRpcHandler<TRpc> _handler;
		private readonly INetworkSerializer _serializer;
		private readonly INetworkTransport _transport;
		private readonly INetworkDiagnostics? _diagnostics;

		public Type MessageType => typeof( TRpc );

		public RpcInvoker(
			NetworkRpcHandler<TRpc> handler,
			INetworkSerializer serializer,
			INetworkTransport transport,
			INetworkDiagnostics? diagnostics
		)
		{
			_handler = handler ?? throw new ArgumentNullException( nameof( handler ) );
			_serializer = serializer ?? throw new ArgumentNullException( nameof( serializer ) );
			_transport = transport ?? throw new ArgumentNullException( nameof( transport ) );
			_diagnostics = diagnostics;
		}

		public void Dispatch( in InboundRpc inbound )
		{
			ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>(
				inbound.Payload,
				0,
				inbound.PayloadLength
			);

			if ( !_serializer.Deserialize( payload, out TRpc rpc ) ) {
				_diagnostics?.RecordDeserializeFailure();
				return;
			}

			bool fromHost = inbound.Sender.Equals( _transport.HostPeerId );

			var context = new NetworkRpcContext(
				inbound.Sender,
				fromHost,
				!fromHost
			);

			_handler.Invoke( in context, in rpc );
		}
	};
};
