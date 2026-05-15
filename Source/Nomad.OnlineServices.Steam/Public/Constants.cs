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

namespace Nomad.OnlineServices.Steam
{
    /// <summary>
    /// Steam Integration global constants.
    /// </summary>
    public static partial class Constants
    {
        /// <summary>
        ///
        /// </summary>
        public static partial class CVars
        {
            /// <summary>
            ///
            /// </summary>
            internal const string NAMESPACE = "Nomad.OnlineServices.Steam";

            /// <summary>
            ///
            /// </summary>
            internal const string LOBBY_UDDATE_INTERVAL = NAMESPACE + ".LobbyUpdateInterval";

            /// <summary>
            ///
            /// </summary>
			internal const string LOBBY_METADATA_FETCH_INTERVAL = NAMESPACE + ".LobbyMetadataFetchInterval";

            /// <summary>
            ///
            /// </summary>
			internal const string LOBBY_MAX_CLIENTS = NAMESPACE + ".LobbyMaxClients";

            /// <summary>
            ///
            /// </summary>
			internal const string LOBBY_PURGE_INTERVAL = NAMESPACE + ".LobbyPurgeInterval";
        }

        /// <summary>
        /// The maximum number of SteamInput handles that can be processed at once.
        /// </summary>
        public const int INPUT_HANDLES_MAX = 4;

        /// <summary>
        /// The default maximum number of lobby clients.
        /// </summary>
        public const int MAX_LOBBY_PLAYERS = 16;

        /// <summary>
        /// The default interval in which a lobby will be deleted. If a lobby has not been heard from in over 30 seconds, the
        /// backend will automatically purge it from the system.
        /// </summary>
        public const int LOBBY_PURGE_TIME_SEC = 30;

        /// <summary>
        /// The default interval in which the current lobby's metadata will be requested from steam services and updated.
        /// </summary>
        public const int LOBBY_UPDATE_TIME_SEC = 120;
    }
}
