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

namespace Nomad.Networking.Transport
{
    /// <summary>
    ///
    /// </summary>
    public interface INetworkTransport
    {
        bool IsActive { get; }
        bool IsHost { get; }
        bool IsClient { get; }

        PeerId LocalPeerId { get; }
        PeerId HostPeerId { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool SendToHost(ReadOnlySpan<byte> payload, NetworkSendMode mode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="payload"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool SendToPeer(PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool Broadcast(ReadOnlySpan<byte> payload, NetworkSendMode mode);

        /// <summary>
        ///
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="packet"></param>
        /// <returns></returns>
        bool TryReceive(Span<byte> destination, out NetworkPacketInfo packet);
    }
}
