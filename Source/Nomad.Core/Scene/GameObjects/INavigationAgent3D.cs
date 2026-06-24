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

using System.Numerics;

namespace Nomad.Core.Scene.GameObjects
{
    /// <summary>
    /// Represents a 3D navigation agent that can follow a path toward a target position.
    /// </summary>
    public interface INavigationAgent3D
    {
        /// <summary>
        /// Gets or sets the desired navigation target.
        /// </summary>
        Vector3 TargetPosition { get; set; }

        /// <summary>
        /// Gets or sets the current agent velocity.
        /// </summary>
        Vector3 Velocity { get; set; }

        /// <summary>
        /// Gets the next point the agent should steer toward.
        /// </summary>
        Vector3 NextPathPosition { get; }

        /// <summary>
        /// Gets or sets the distance from the target that counts as arrival.
        /// </summary>
        float StoppingDistance { get; set; }

        /// <summary>
        /// Gets or sets whether local avoidance is enabled.
        /// </summary>
        bool AvoidanceEnabled { get; set; }

        /// <summary>
        /// Gets whether the current path has been completed.
        /// </summary>
        bool IsNavigationFinished { get; }
    }
}
