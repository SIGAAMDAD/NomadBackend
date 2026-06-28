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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Nomad.Core.Physics.ValueObjects;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("ValueObjects")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class CollisionLayerMaskTests
    {
        [Test]
        public void CollisionLayerMask_FromLayerContainsAndOperators_CombineMasks()
        {
            var layerOne = CollisionLayerMask.FromLayer(1);
            var layerTwo = CollisionLayerMask.FromLayer(2);
            var combined = layerOne | layerTwo;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(layerOne.Contains(1), Is.True);
                Assert.That(layerOne.Contains(2), Is.False);
                Assert.That(combined.Contains(1), Is.True);
                Assert.That(combined.Contains(2), Is.True);
                Assert.That((combined & layerOne).Contains(1), Is.True);
                Assert.That(CollisionLayerMask.None.Value, Is.EqualTo(0u));
                Assert.That(CollisionLayerMask.All.Value, Is.EqualTo(uint.MaxValue));
            }
        }
    }
}
