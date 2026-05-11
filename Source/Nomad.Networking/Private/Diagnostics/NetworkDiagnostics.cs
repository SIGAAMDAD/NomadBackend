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

using Nomad.Networking.Diagnostics;

namespace Nomad.Networking.Private.Diagnostics
{
	internal sealed class NetworkDiagnostics : INetworkDiagnostics
	{
		private uint _packetsSent = 0;
		private uint _packetsReceived = 0;
		private uint _bytesSent = 0;
		private uint _bytesReceived = 0;
		private uint _packetsDropped = 0;
		private uint _deserializeFailures = 0;
		private uint _unknownMessageIds = 0;
		private uint _authorityRejects = 0;

		public NetworkStats Stats => new NetworkStats(
			_packetsSent,
			_packetsReceived,
			_bytesSent,
			_bytesReceived,
			_packetsDropped,
			_deserializeFailures,
			_unknownMessageIds,
			_authorityRejects
		);

		public void Reset()
		{
			_packetsSent = 0;
			_packetsReceived = 0;
			_bytesSent = 0;
			_bytesReceived = 0;
			_packetsDropped = 0;
			_deserializeFailures = 0;
			_unknownMessageIds = 0;
			_authorityRejects = 0;
		}

		public void RecordPacketSent( int bytes )
		{
			_packetsSent++;
			_bytesSent += (uint)bytes;
		}

		public void RecordPacketReceived( int bytes )
		{
			_packetsReceived++;
			_bytesReceived += (uint)bytes;
		}

		public void RecordPacketDropped()
		{
			_packetsDropped++;
		}

		public void RecordDeserializeFailure()
		{
			_deserializeFailures++;
		}

		public void RecordUnknownMessageId()
		{
			_unknownMessageIds++;
		}

		public void RecordAuthorityReject()
		{
			_authorityRejects++;
		}
	};
};
