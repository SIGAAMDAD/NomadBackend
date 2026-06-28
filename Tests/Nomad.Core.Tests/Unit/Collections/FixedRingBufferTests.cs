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
    public sealed class FixedRingBufferTests
    {
        [Test]
        public void FixedRingBuffer_PushBackAndGetFromNewest_TracksRecentValues()
        {
            var buffer = new FixedRingBuffer<int>(2);

            buffer.PushBack(1);
            buffer.PushBack(2);
            buffer.PushBack(3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buffer.Count, Is.EqualTo(2));
                Assert.That(buffer.Capacity, Is.EqualTo(2));
                Assert.That(buffer.GetFromNewest(0), Is.EqualTo(3));
                Assert.That(buffer.GetFromNewest(1), Is.EqualTo(2));
            }

            buffer.Clear();
            Assert.That(buffer.Count, Is.EqualTo(0));
        }
    }
}
