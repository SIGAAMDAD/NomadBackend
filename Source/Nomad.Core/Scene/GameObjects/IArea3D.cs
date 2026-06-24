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

using Nomad.Core.Events;

namespace Nomad.Core.Scene.GameObjects
{
    /// <summary>
    /// Represents a 3D overlap volume or trigger area.
    /// </summary>
    public interface IArea3D : ICollisionBody3D
    {
        /// <summary>
        /// Raised when another area begins overlapping this area.
        /// </summary>
        IGameEvent<Area3DEventArgs> AreaEntered { get; }

        /// <summary>
        /// Raised when another area stops overlapping this area.
        /// </summary>
        IGameEvent<Area3DEventArgs> AreaExited { get; }

        /// <summary>
        /// Raised when a shape on another area begins overlapping a local shape.
        /// </summary>
        IGameEvent<Area3DShapeEventArgs> AreaShapeEntered { get; }

        /// <summary>
        /// Raised when a shape on another area stops overlapping a local shape.
        /// </summary>
        IGameEvent<Area3DShapeEventArgs> AreaShapeExited { get; }

        /// <summary>
        /// Raised when another body begins overlapping this area.
        /// </summary>
        IGameEvent<Body3DEventArgs> BodyEntered { get; }

        /// <summary>
        /// Raised when another body stops overlapping this area.
        /// </summary>
        IGameEvent<Body3DEventArgs> BodyExited { get; }

        /// <summary>
        /// Raised when a shape on another body begins overlapping a local shape.
        /// </summary>
        IGameEvent<Body3DShapeEventArgs> BodyShapeEntered { get; }

        /// <summary>
        /// Raised when a shape on another body stops overlapping a local shape.
        /// </summary>
        IGameEvent<Body3DShapeEventArgs> BodyShapeExited { get; }

        /// <summary>
        /// Gets or sets whether the area is actively reporting overlaps.
        /// </summary>
        bool Monitoring { get; set; }

        /// <summary>
        /// Gets or sets whether the area behaves as a trigger volume.
        /// </summary>
        bool IsTrigger { get; set; }
    }
}
