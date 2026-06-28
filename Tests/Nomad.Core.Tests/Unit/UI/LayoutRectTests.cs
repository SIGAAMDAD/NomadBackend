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
using Nomad.Core.UI;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("UI")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class LayoutRectTests
    {
        [Test]
        public void LayoutRect_Constructor_AssignsPositionAndSize()
        {
            var rect = new LayoutRect(1f, 2f, 3f, 4f);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rect.X, Is.EqualTo(1f));
                Assert.That(rect.Y, Is.EqualTo(2f));
                Assert.That(rect.Width, Is.EqualTo(3f));
                Assert.That(rect.Height, Is.EqualTo(4f));
            }
        }
    }
}
