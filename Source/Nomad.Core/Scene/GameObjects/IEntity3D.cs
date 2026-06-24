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
    /// Represents a 3D scene entity.
    /// </summary>
    public interface IEntity3D : ISceneObject
    {
        /// <summary>
        /// Gets or sets whether the entity is visible.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the entity position.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the entity scale.
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the entity rotation in degrees.
        /// </summary>
        Vector3 Rotation { get; set; }

        /// <summary>
        /// Gets or sets the render order.
        /// </summary>
        int RenderOrder { get; set; }
    }
}
