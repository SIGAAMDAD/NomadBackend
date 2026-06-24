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
using Nomad.Core.Events;

namespace Nomad.Core.Scene.GameObjects
{
    /// <summary>
    /// Represents a 3D in-game object.
    /// </summary>
    public interface IObject3D : IGameObject
    {
        /// <summary>
        /// Raised when the object display state changes.
        /// </summary>
        IGameEvent<bool> DisplayStateChanged { get; }

        /// <summary>
        /// Gets or sets whether the object is shown.
        /// </summary>
        bool Show { get; set; }

        /// <summary>
        /// Gets or sets the object position.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Gets or sets the object scale.
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        /// Gets or sets the object rotation in degrees.
        /// </summary>
        Vector3 Rotation { get; set; }
    }
}
