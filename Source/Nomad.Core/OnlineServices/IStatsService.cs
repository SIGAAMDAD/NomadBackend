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
using Nomad.Core.Util;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    ///
    /// </summary>
    public interface IStatsService : IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        bool SupportsLeaderboards { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task SetStatInt(InternString statName, int value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task SetStatFloat(InternString statName, float value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="statName"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> SetUserStatInt(PeerId peerId, InternString statName, int value, CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="statName"></param>
        /// <param name="value"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> SetUserStatFloat(PeerId peerId, InternString statName, float value, CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <returns></returns>
        Task<int> GetStatInt(InternString statName);

        /// <summary>
        ///
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="statName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<int> GetUserStatInt(PeerId peerId, InternString statName, CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="peerId"></param>
        /// <param name="statName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<float> GetUserStatFloat(PeerId peerId, InternString statName, CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <returns></returns>
        Task<float> GetStatFloat(InternString statName);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        bool StoreStats();
    }
}
