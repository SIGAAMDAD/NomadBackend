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
	/// Immutable public snapshot of the currently active network session.
	/// </summary>
	public sealed record NetworkSessionInfo
	{
		/// <summary>
		/// Unique id for this gameplay session.
		/// Usually matches the lobby id for lobby-backed sessions.
		/// </summary>
		public Guid SessionId { get; init; }

		/// <summary>
		/// The lobby that created or discovered this session, if any.
		/// LobbyId.Invalid means this session is not backed by a lobby.
		/// </summary>
		public LobbyId LobbyId { get; init; }

		/// <summary>
		/// Whether this local machine is offline, host, client, or dedicated server.
		/// </summary>
		public NetworkSessionMode Mode { get; init; }

		/// <summary>
		/// Current high-level lifecycle state of the local session.
		/// </summary>
		public NetworkConnectionState State { get; init; }

		/// <summary>
		/// Minimum players required for this session to be considered startable.
		/// </summary>
		public int MinPlayers { get; init; }

		/// <summary>
		/// Maximum accepted gameplay peers, usually including the host/local player.
		/// </summary>
		public int MaxPlayers { get; init; }

		/// <summary>
		/// Number of accepted gameplay peers.
		/// This is usually Peers.Count, but keeping it explicit avoids allocations
		/// or repeated list traversal for UI.
		/// </summary>
		public int PeerCount { get; init; }

		/// <summary>
		/// Local peer id inside this session.
		/// </summary>
		public PeerId LocalPeerId { get; init; }

		/// <summary>
		/// Host peer id inside this session.
		/// </summary>
		public PeerId HostPeerId { get; init; }

		/// <summary>
		/// Accepted gameplay peers. This should not include mere lobby members
		/// unless they have been accepted into the network session.
		/// </summary>
		public IReadOnlyList<NetworkPeerInfo> Peers { get; init; } = Array.Empty<NetworkPeerInfo>();

		/// <summary>
		/// When this session snapshot was produced.
		/// Useful for UI/debugging and stale-state checks.
		/// </summary>
		public DateTime LastUpdatedUtc { get; init; } = DateTime.UtcNow;
	}
}
