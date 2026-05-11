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
using Nomad.Core.Events;
using Nomad.Networking.Diagnostics;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Private.Events
{
	internal sealed class EventInvoker<TArgs> : IEventInvoker
		where TArgs : struct
	{
		private readonly IGameEvent<TArgs> _gameEvent;
		private readonly INetworkSerializer _serializer;
		private readonly INetworkDiagnostics? _diagnostics;

		public Type MessageType => typeof( TArgs );

		public EventInvoker(
			IGameEvent<TArgs> gameEvent,
			INetworkSerializer serializer,
			INetworkDiagnostics? diagnostics
		)
		{
			_gameEvent = gameEvent ?? throw new ArgumentNullException( nameof( gameEvent ) );
			_serializer = serializer ?? throw new ArgumentNullException( nameof( serializer ) );
			_diagnostics = diagnostics;
		}

		public void Dispatch( in InboundEvent inbound )
		{
			ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>(
				inbound.Payload,
				0,
				inbound.PayloadLength
			);

			if ( !_serializer.Deserialize( payload, out TArgs args ) ) {
				_diagnostics?.RecordDeserializeFailure();
				return;
			}

			_gameEvent.Publish( in args );
		}
	};
};
