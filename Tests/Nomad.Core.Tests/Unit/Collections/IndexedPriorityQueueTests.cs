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
    public sealed class IndexedPriorityQueueTests
    {
        [Test]
        public void IndexedPriorityQueue_PushOrDecreaseAndPopMin_ReturnsLowestPriorityId()
        {
            var queue = new IndexedPriorityQueue(1, 1);

            queue.PushOrDecrease(10, 10f);
            queue.PushOrDecrease(20, 20f);
            queue.PushOrDecrease(20, 1f);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queue.Count, Is.EqualTo(2));
                Assert.That(queue.Contains(10), Is.True);
                Assert.That(queue.Contains(20), Is.True);
                Assert.That(queue.PopMin(), Is.EqualTo(20));
                Assert.That(queue.PopMin(), Is.EqualTo(10));
            }

            queue.Clear();
            Assert.That(queue.Count, Is.EqualTo(0));
        }
    }
}
