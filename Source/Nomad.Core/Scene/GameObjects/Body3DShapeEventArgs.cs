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
    /// Describes a shape-level overlap event involving another 3D collision body.
    /// </summary>
    public readonly struct Body3DShapeEventArgs
    {
        /// <summary>
        /// The overlapping collision body.
        /// </summary>
        public ICollisionBody3D Body => _body;

        /// <summary>
        /// The shape index on the other body.
        /// </summary>
        public long BodyShapeIndex => _bodyShapeIndex;

        /// <summary>
        /// The local shape index on this area.
        /// </summary>
        public long LocalShapeIndex => _localShapeIndex;

        private readonly ICollisionBody3D _body;
        private readonly long _bodyShapeIndex;
        private readonly long _localShapeIndex;

        /// <summary>
        /// Creates a new set of shape-level body overlap event arguments.
        /// </summary>
        public Body3DShapeEventArgs(ICollisionBody3D body, long bodyShapeIndex, long localShapeIndex)
        {
            _body = body;
            _bodyShapeIndex = bodyShapeIndex;
            _localShapeIndex = localShapeIndex;
        }
    }
}
