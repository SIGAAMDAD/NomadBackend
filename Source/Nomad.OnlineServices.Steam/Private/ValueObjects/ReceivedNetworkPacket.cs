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

using System.Buffers;
using Nomad.Networking.Messaging;
using Nomad.Core.OnlineServices;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	internal readonly struct ReceivedNetworkPacket
	{
		public readonly CSteamID SteamId;
		public readonly byte[] Payload;
		public readonly int BytesWritten;
		public readonly NetworkSendMode Mode;

		public ReceivedNetworkPacket( CSteamID steamId, byte[] payload, int bytesWritten, NetworkSendMode mode )
		{
			SteamId = steamId;
			Payload = payload;
			BytesWritten = bytesWritten;
			Mode = mode;
		}

		public void ReleasePayload()
		{
			ArrayPool<byte>.Shared.Return( Payload );
		}
	};
};
