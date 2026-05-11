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
using System.Buffers;
using Nomad.Core.OnlineServices;

namespace Nomad.Networking.Private.Rpc
{
	internal struct InboundRpc : IDisposable
	{
		public PeerId Sender;
		public ushort MessageId;
		public byte[] Payload;
		public int PayloadLength;

		public InboundRpc(
			PeerId sender,
			ushort messageId,
			byte[] payload,
			int payloadLength
		)
		{
			Sender = sender;
			MessageId = messageId;
			Payload = payload;
			PayloadLength = payloadLength;
		}

		public void Dispose()
		{
			if ( Payload != null && Payload.Length != 0 ) {
				ArrayPool<byte>.Shared.Return( Payload );
				Payload = Array.Empty<byte>();
				PayloadLength = 0;
			}
		}
	};
};
