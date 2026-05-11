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

namespace Nomad.Networking.Diagnostics
{
    public readonly struct NetworkStats
    {
        public readonly uint PacketsSent;
        public readonly uint PacketsReceived;
        public readonly uint BytesSent;
        public readonly uint BytesReceived;
        public readonly uint PacketsDropped;
        public readonly uint DeserializeFailures;
        public readonly uint UnknownMessageIds;
        public readonly uint AuthorityRejects;

        public NetworkStats(
            uint packetsSent,
            uint packetsReceived,
            uint bytesSent,
            uint bytesReceived,
            uint packetsDropped,
            uint deserializeFailures,
            uint unknownMessageIds,
            uint authorityRejects
        )
        {
            PacketsSent = packetsSent;
            PacketsReceived = packetsReceived;
            BytesSent = bytesSent;
            BytesReceived = bytesReceived;
            PacketsDropped = packetsDropped;
            DeserializeFailures = deserializeFailures;
            UnknownMessageIds = unknownMessageIds;
            AuthorityRejects = authorityRejects;
        }
    }
}
