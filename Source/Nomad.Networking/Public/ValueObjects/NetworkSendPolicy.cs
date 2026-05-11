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

using Nomad.Networking.Messaging;

namespace Nomad.Networking.ValueObjects
{
    public readonly struct NetworkSendPolicy
    {
        public readonly NetworkSendMode Mode;
        public readonly NetworkChannel Channel;

        public NetworkSendPolicy(NetworkSendMode mode, NetworkChannel channel)
        {
            Mode = mode;
            Channel = channel;
        }

        public static readonly NetworkSendPolicy ReliableControl =
            new NetworkSendPolicy(NetworkSendMode.Reliable, NetworkChannel.Control);

        public static readonly NetworkSendPolicy UnreliableInput =
            new NetworkSendPolicy(NetworkSendMode.UnreliableNoDelay, NetworkChannel.Input);

        public static readonly NetworkSendPolicy UnreliableSnapshot =
            new NetworkSendPolicy(NetworkSendMode.Unreliable, NetworkChannel.Snapshot);
    }
}
