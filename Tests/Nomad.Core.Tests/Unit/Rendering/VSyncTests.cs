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
using Nomad.Core.Engine.Rendering;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Rendering")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class VSyncTests
    {
        [Test]
        public void VSyncMode_ToDisplayString_ReturnsExpectedLabels()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(VSyncMode.Disabled.ToDisplayString(), Is.EqualTo("Off"));
                Assert.That(VSyncMode.Enabled.ToDisplayString(), Is.EqualTo("On"));
                Assert.That(VSyncMode.Adaptive.ToDisplayString(), Is.EqualTo("Adaptive"));
                Assert.That(VSyncMode.TripleBuffered.ToDisplayString(), Is.EqualTo("Triple Buffered"));
            }
        }

        [Test]
        public void VSyncMode_ToDisplayString_ThrowsForOutOfRangeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VSyncMode.Count.ToDisplayString());
            Assert.Throws<ArgumentOutOfRangeException>(() => ((VSyncMode)255).ToDisplayString());
        }
    }
}
