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
    public sealed class PooledQueueTests
    {
        [Test]
        public void PooledQueue_EnqueueDequeuePeekAndClear_OperateFifo()
        {
            using var queue = new PooledQueue<int>(1);

            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.PeekRef(), Is.EqualTo(1));
                Assert.That(queue.GetFromOldest(1), Is.EqualTo(2));
                Assert.That(queue.GetFromNewest(0), Is.EqualTo(3));
                Assert.That(queue.Dequeue(), Is.EqualTo(1));
                Assert.That(queue.TryDequeue(out int value), Is.True);
                Assert.That(value, Is.EqualTo(2));
            }

            queue.Clear();
            Assert.That(queue.TryDequeue(out _), Is.False);
        }
    }
}
