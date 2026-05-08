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
using System.Collections.Generic;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    /// Creation information for lobbies.
    /// </summary>
    public record LobbyInfo
    {
        /// <summary>
        ///
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        ///
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        ///
        /// </summary>
        public string? Map { get; init; }

        /// <summary>
        ///
        /// </summary>
        public string? GameMode { get; init; }

        /// <summary>
        ///
        /// </summary>
        public ulong OwnerId { get; init; }

        /// <summary>
        ///
        /// </summary>
        public int MaxPlayers { get; init; }

        /// <summary>
        /// The current number of players in the lobby.
        /// </summary>
        public int PlayerCount { get; init; }

        /// <summary>
        ///
        /// </summary>
        public LobbyVisibility Visibility { get; init; }

        /// <summary>
        ///
        /// </summary>
        public Dictionary<string, string>? Metadata { get; init; }
    }
}
