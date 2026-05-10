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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nomad.Core.Events;
using Nomad.Core.Util;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    /// Interface for lobby services.
    /// </summary>
    public interface ILobbyService : IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        bool IsInLobby { get; }

        /// <summary>
        ///
        /// </summary>
        bool IsLobbyLeader { get; }

        /// <summary>
        ///
        /// </summary>
        LobbyInfo? Current { get; }

        /// <summary>
        /// Event that triggers upon receiving the results of attempting to join a network lobby.
        /// </summary>
        [Event(nameSpace: "Nomad.Core.OnlineServices", PayloadName = "LobbyJoinedResultEventArgs")]
        [EventPayload("Id", typeof(Guid))]
        IGameEvent<LobbyJoinedResultEventArgs> LobbyJoined { get; }

        /// <summary>
        ///
        /// </summary>
        [Event(nameSpace: "Nomad.Core.OnlineServices", PayloadName = "LobbyLeaveResultEventArgs")]
        [EventPayload("Id", typeof(Guid), Order = 1)]
        [EventPayload("Reason", typeof(LobbyLeaveReason), Order = 2)]
        IGameEvent<LobbyLeaveResultEventArgs> LobbyLeft { get; }

        /// <summary>
        /// Event that triggers upon a lobby's creation. We should only be receiving this event if our machine is the host.
        /// </summary>
        [Event(nameSpace: "Nomad.Core.OnlineServices", PayloadName = "LobbyStartResultEventArgs")]
        [EventPayload("Success", typeof(bool), Order = 1)]
        [EventPayload("Id", typeof(Guid), Order = 2)]
        IGameEvent<LobbyStartResultEventArgs> LobbyStarted { get; }

        /// <summary>
        /// Creates a new lobby with the provided parameters
        /// </summary>
        /// <param name="lobbyInfo">The information to create the lobby with.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [ResultObject("LobbyCreateResult", isRecord: true, Namespace = "Nomad.Core.OnlineServices")]
        [ResultObjectPayload("Id", typeof(LobbyId), order: 1)]
        [ResultObjectPayload("Lobby", typeof(LobbyInfo), order: 2, IsOptional = true)]
        [ResultObjectPayload("Reason", typeof(NetworkSessionFailureReason), order: 3)]
        [ResultObjectFailure("Reason", MethodName = "Failure")]
        [ResultObjectSuccess("Id", MethodName = "Created")]
        Task<LobbyCreateResult> CreateLobby(LobbyCreateInfo lobbyInfo, CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="lobbyId">The lobby's unique 64-bit id.</param>
        /// <param name="ct"></param>
        /// <returns>True if the lobby was joined successfully, false otherwise.</returns>
        [ResultObject("LobbyJoinResult", isRecord: true, Namespace = "Nomad.Core.OnlineServices")]
        [ResultObjectPayload("LobbyData", typeof(LobbyInfo), order: 2, IsOptional = true)]
        [ResultObjectPayload("Reason", typeof(NetworkSessionFailureReason), order: 3, IsOptional = true)]
        [ResultObjectFailure("Reason", MethodName = "Failure")]
        [ResultObjectSuccess("LobbyData", MethodName = "Joined")]
        Task<LobbyJoinResult> JoinLobby(LobbyId lobbyId, CancellationToken ct = default);

        /// <summary>
        /// Leaves the current lobby.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns><c>True</c> if the lobby was left successfully, <c>false</c> otherwise.</returns>
        Task<bool> LeaveLobby(CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<LobbyMemberInfo> GetMembers();

        /// <summary>
        ///
        /// </summary>
        /// <param name="active"></param>
        void SetLobbyRefresh(bool active);
    }
}
