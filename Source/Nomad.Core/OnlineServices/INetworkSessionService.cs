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

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    ///
    /// </summary>
    public interface INetworkSessionService
    {
        bool IsSessionActive { get; }
        bool IsHost { get; }
        bool IsClient { get; }
        NetworkSessionInfo? CurrentSession { get; }

        IGameEvent<EmptyEventArgs> SessionChanged { get; }
        IGameEvent<PeerId> PeerConnected { get; }
        IGameEvent<PeerId> PeerDisconnected { get; }

        Task<bool> StartHostAsync(LobbyInfo info, CancellationToken ct = default);
        Task<bool> JoinAsClientAsync(Guid id, CancellationToken ct = default);
        Task StopAsync(CancellationToken ct = default);

        void SendToHost<TMessage>(ref TMessage message)
            where TMessage : struct;

        void SendToPeer<TMessage>(PeerId peerId, ref TMessage message)
            where TMessage : struct;

        void Broadcast<TMessage>(ref TMessage message)
            where TMessage : struct;
    }
}
