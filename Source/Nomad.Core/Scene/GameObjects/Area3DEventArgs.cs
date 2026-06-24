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
    /// Describes an overlap event involving another 3D area.
    /// </summary>
    public readonly struct Area3DEventArgs
    {
        /// <summary>
        /// The overlapping area.
        /// </summary>
        public IArea3D Area => _area;

        private readonly IArea3D _area;

        /// <summary>
        /// Creates a new set of area overlap event arguments.
        /// </summary>
        public Area3DEventArgs(IArea3D area)
        {
            _area = area;
        }
    }
}
