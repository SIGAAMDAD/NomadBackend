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

using Nomad.Core.OnlineServices;
using Nomad.Networking.ValueObjects;

namespace Nomad.Networking.Rpc
{
	public readonly struct NetworkRpcTarget
	{
		public NetworkTargetKind Kind { get; }
		public PeerId PeerId { get; }

		private NetworkRpcTarget( NetworkTargetKind kind, PeerId peerId )
		{
			Kind = kind;
			PeerId = peerId;
		}

		public static NetworkRpcTarget Host()
		{
			return new NetworkRpcTarget( NetworkTargetKind.Host, default );
		}

		public static NetworkRpcTarget Peer( PeerId peerId )
		{
			return new NetworkRpcTarget( NetworkTargetKind.Peer, peerId );
		}

		public static NetworkRpcTarget All()
		{
			return new NetworkRpcTarget( NetworkTargetKind.All, default );
		}

		public static NetworkRpcTarget Others()
		{
			return new NetworkRpcTarget( NetworkTargetKind.Others, default );
		}
	}
}
