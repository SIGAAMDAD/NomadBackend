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
        ValueTask SetStatInt(InternString statName, int value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ValueTask SetStatFloat(InternString statName, float value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <returns></returns>
        ValueTask<int> GetStatInt(InternString statName);

        /// <summary>
        ///
        /// </summary>
        /// <param name="statName"></param>
        /// <returns></returns>
        ValueTask<float> GetStatFloat(InternString statName);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        ValueTask<bool> StoreStats();
    }
}
