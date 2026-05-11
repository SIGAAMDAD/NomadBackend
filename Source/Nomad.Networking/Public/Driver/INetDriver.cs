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
using Nomad.Core.OnlineServices;
using Nomad.Networking.Messaging;
using Nomad.Networking.Session;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Driver
{
	public interface INetDriver : IDisposable
	{
		bool IsListening { get; }
		bool IsInitialized { get; }

		event Action<NetConnection> ConnectionRequested;
		event Action<NetConnection> ConnectionEstablished;
		event Action<NetConnection> ConnectionClosed;

		bool Listen( int virtualPort = 0 );
		bool Connect( PeerId peerId, int virtualPort = 0 );
		bool Accept( PeerId peerId );
		bool Close( PeerId peerId, string reason );
		void CloseAll( string reason );

		bool Send( PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode );
		bool TryReceive( Span<byte> destination, out NetworkPacketInfo packet );
		bool TryGetConnection( PeerId peerId, out NetConnection connection );
	}

	public readonly struct NetConnection
	{
		public PeerId PeerId { get; }
		public NetworkConnectionState State { get; }

		public NetConnection( PeerId peerId, NetworkConnectionState state )
		{
			PeerId = peerId;
			State = state;
		}
	}
}
