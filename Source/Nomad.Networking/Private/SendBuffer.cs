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

namespace Nomad.Networking.Private
{
	internal struct PooledSendBuffer : IDisposable
	{
		public byte[] Buffer;
		public int Length;

		public Span<byte> Span => Buffer.AsSpan( 0, Length );

		public PooledSendBuffer( byte[] buffer, int length )
		{
			Buffer = buffer;
			Length = length;
		}

		public void Dispose()
		{
			if ( Buffer != null && Buffer.Length != 0 ) {
				ArrayPool<byte>.Shared.Return( Buffer );
				Buffer = Array.Empty<byte>();
				Length = 0;
			}
		}
	};
};
