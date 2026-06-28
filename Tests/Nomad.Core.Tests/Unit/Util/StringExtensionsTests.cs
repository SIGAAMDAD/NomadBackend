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
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Util")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class StringExtensionsTests
    {
        [Test]
        public void HashFileName_IsCaseInsensitiveAndUsesSeed()
        {
            uint lower = "textures/wall.png".HashFileName();
            uint upper = "TEXTURES/WALL.PNG".HashFileName();
            uint seeded = "textures/wall.png".HashFileName(123);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(upper, Is.EqualTo(lower));
                Assert.That(seeded, Is.Not.EqualTo(lower));
            }
        }

        [Test]
        public void HashFileName_ThrowsForNullOrEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null!).HashFileName());
            Assert.Throws<ArgumentException>(() => string.Empty.HashFileName());
        }
    }
}
