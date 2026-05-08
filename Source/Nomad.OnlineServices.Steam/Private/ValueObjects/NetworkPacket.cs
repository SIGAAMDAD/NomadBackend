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
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	internal readonly struct NetworkPacket
	{
		public readonly CSteamID PeerId;
		public readonly NetworkPacketHeader Header;
		public readonly ReadOnlyMemory<byte> Payload;

		public readonly NetworkPacketType Type => Header.Type;
		public readonly uint Sequence => Header.Sequence;
		public readonly uint Tick => Header.Tick;

		public NetworkPacket( CSteamID peerId, in NetworkPacketHeader header, ReadOnlyMemory<byte> payload )
		{
			PeerId = peerId;
			Header = header;
			Payload = payload;
		}

		public void Serialize( Span<byte> destination )
		{
			Header.WriteTo( destination.Slice( 0, NetworkPacketHeader.SIZE ) );
			Payload.Span.CopyTo( destination.Slice( NetworkPacketHeader.SIZE, Payload.Length ) );
		}
	};
};
