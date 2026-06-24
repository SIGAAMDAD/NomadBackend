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

namespace Nomad.Core.Scene.GameObjects
{
    /// <summary>
    /// Represents a 3D in-game viewpoint.
    /// </summary>
    public interface ICamera3D : IEntity3D
    {
        /// <summary>
        /// Gets or sets the camera field of view in degrees.
        /// </summary>
        float FieldOfView { get; set; }

        /// <summary>
        /// Gets or sets the near clipping plane distance.
        /// </summary>
        float NearClip { get; set; }

        /// <summary>
        /// Gets or sets the far clipping plane distance.
        /// </summary>
        float FarClip { get; set; }

        /// <summary>
        /// Gets or sets the followed object.
        /// </summary>
        IGameObject FollowTarget { get; set; }
    }
}
