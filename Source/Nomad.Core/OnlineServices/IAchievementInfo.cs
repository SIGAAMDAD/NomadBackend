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

using Nomad.Core.Util;
using System;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    ///
    /// </summary>
    public interface IAchievementInfo
    {
        /// <summary>
        /// The achievement's internal id.
        /// </summary>
        string? Id { get; }

        /// <summary>
        /// The id of the statistic tied to this achievement's progress.
        /// </summary>
        string? StatId { get; }

        /// <summary>
        /// Whether the achievement has been unlocked or not.
        /// </summary>
        bool Achieved { get; }

        /// <summary>
        /// How much progress we have on the achievement.
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// The maximum allowed progress for this achievement.
        /// </summary>
        float MaxProgress { get; }

        /// <summary>
        /// The time at which this achievement was unlocked.
        /// </summary>
        DateTime UnlockedTime { get; }
    }
}
