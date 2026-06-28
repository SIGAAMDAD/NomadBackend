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
    public sealed class ThicknessTests
    {
        [Test]
        public void Thickness_Constructor_AssignsEdges()
        {
            var thickness = new Thickness(1f, 2f, 3f, 4f);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(thickness.Left, Is.EqualTo(1f));
                Assert.That(thickness.Top, Is.EqualTo(2f));
                Assert.That(thickness.Right, Is.EqualTo(3f));
                Assert.That(thickness.Bottom, Is.EqualTo(4f));
            }
        }
    }
}
