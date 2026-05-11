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
using System.Threading.Tasks;
using System.Threading;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Messaging;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Session
{
    /// <summary>
    ///
    /// </summary>
    public interface INetworkSessionService
    {
        /// <summary>
        ///
        /// </summary>
        bool IsSessionActive { get; }

        /// <summary>
        ///
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        ///
        /// </summary>
        bool IsClient { get; }

        /// <summary>
        ///
        /// </summary>
        NetworkSessionInfo? CurrentSession { get; }

        /// <summary>
        /// Event that triggers whenever a network session is created, joined, or left.
        /// </summary>
        [Event(nameSpace: "Nomad.Networking.Session", PayloadName = "NetworkSessionChangedEventArgs")]
        [EventPayload("SessionId", typeof(Guid), Order = 1)]
        [EventPayload("LobbyId", typeof(LobbyId), Order = 2)]
        [EventPayload("Mode", typeof(NetworkSessionMode), Order = 3)]
        [EventPayload("LocalPeerId", typeof(PeerId), Order = 4)]
        [EventPayload("HostPeerId", typeof(PeerId), Order = 5)]
        IGameEvent<NetworkSessionChangedEventArgs> SessionChanged { get; }

        [Event(nameSpace: "Nomad.Networking.Session")]
        [EventPayload("PeerId", typeof(PeerId), Order = 1)]
        IGameEvent<PeerConnectedEventArgs> PeerConnected { get; }

        [Event(nameSpace: "Nomad.Networking.Session")]
        [EventPayload("PeerId", typeof(PeerId), Order = 1)]
        [EventPayload("LeaveReason", typeof(LobbyLeaveReason), Order = 2)]
        IGameEvent<PeerDisconnectedEventArgs> PeerDisconnected { get; }

        Task<bool> StartHostAsync(LobbyCreateInfo info, CancellationToken ct = default);
        Task<bool> JoinAsClientAsync(LobbyId lobbyId, CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);

        void SendToHost(ReadOnlySpan<byte> payload, NetworkSendMode mode = NetworkSendMode.Unreliable);
        void SendToPeer(PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode = NetworkSendMode.Unreliable);
        void Broadcast(ReadOnlySpan<byte> payload, NetworkSendMode mode = NetworkSendMode.Unreliable);
        bool TryReceive(Span<byte> destination, out NetworkPacketInfo info);
    }
}
