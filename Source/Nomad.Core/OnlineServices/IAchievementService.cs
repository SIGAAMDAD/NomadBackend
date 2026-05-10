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
using Nomad.Core.Events;
using Nomad.Core.Util;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    /// Interface for achievement services.
    /// </summary>
    public interface IAchievementService : IDisposable
    {
        /// <summary>
        /// Indicates whether the service supports achievements.
        /// </summary>
        bool SupportsAchievements { get; }

        /// <summary>
        ///
        /// </summary>
        int NumAchievements { get; }

        /// <summary>
        /// Event that triggers whenever an achievement is unlocked from the internal API.
        /// </summary>
        [Event(nameSpace: "Nomad.Core.OnlineServices", PayloadName = "AchievementUnlockedEventArgs")]
        [EventPayload("Id", typeof(InternString))]
        IGameEvent<AchievementUnlockedEventArgs> Unlocked { get; }

        /// <summary>
        /// Event that triggers whenever an achievement's progress has been changed.
        /// </summary>
        [Event(nameSpace: "Nomad.Core.OnlineServices", PayloadName = "AchievementProgressChangedEventArgs")]
        [EventPayload("Id", typeof(InternString), Order = 1)]
        [EventPayload("Progress", typeof(float), Order = 2)]
        IGameEvent<AchievementProgressChangedEventArgs> ProgressChanged { get; }

        /// <summary>
        /// Unlocks the specified achievement.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to unlock.</param>
        /// <returns></returns>
        Task UnlockAchievement(InternString achievementId);

        /// <summary>
        /// Locks the specified achievement.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to lock.</param>
        /// <returns></returns>
        Task LockAchievement(InternString achievementId);

        /// <summary>
        /// Sets the progress for the specified achievement.
        /// </summary>
        /// <param name="achievementId">The ID of the achievement to set progress for.</param>
        /// <param name="current">The current progress value.</param>
        /// <returns></returns>
        Task SetAchievementProgress(InternString achievementId, float current);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        IAchievementInfo? GetAchievementInfo(InternString achievementId);
    }
}
