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
using Nomad.Core.Collections;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Collections")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class CompactGraphTests
    {
        [Test]
        public void CompactGraph_GetNeighbors_ReturnsConfiguredSlice()
        {
            var graph = new CompactGraph(new[] { 0, 2, 3 }, new[] { 1, 2, 0 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(graph.NodeCount, Is.EqualTo(2));
                Assert.That(graph.EdgeCount, Is.EqualTo(3));
                Assert.That(graph.GetNeighbors(0).ToArray(), Is.EqualTo(new[] { 1, 2 }));
                Assert.That(graph.GetNeighbors(1).ToArray(), Is.EqualTo(new[] { 0 }));
            }
        }
    }
}
