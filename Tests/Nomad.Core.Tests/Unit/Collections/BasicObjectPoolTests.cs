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
using Nomad.Core.Memory;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Collections")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class BasicObjectPoolTests
    {
        [Test]
        public void BasicObjectPool_Constructor_ThrowsWhenFactoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new BasicObjectPool<PooledItem>(null));
        }

        [Test]
        public void BasicObjectPool_RentAndReturn_TracksCounts()
        {
            var pool = new BasicObjectPool<PooledItem>(() => new PooledItem(), initialSize: 1, maxSize: 2);

            var item = pool.Rent();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pool.TotalCount, Is.EqualTo(1));
                Assert.That(pool.AvailableCount, Is.EqualTo(0));
                Assert.That(pool.ActiveObjectCount, Is.EqualTo(1));
            }

            pool.Return(item);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pool.TotalCount, Is.EqualTo(1));
                Assert.That(pool.AvailableCount, Is.EqualTo(1));
                Assert.That(pool.ActiveObjectCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void BasicObjectPool_Dispose_DisposesAvailableObjectsAndResetsCounts()
        {
            var pool = new BasicObjectPool<PooledItem>(() => new PooledItem(), initialSize: 1, maxSize: 2);

            var item = pool.Rent();
            pool.Return(item);

            pool.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.DisposeCount, Is.EqualTo(1));
                Assert.That(pool.TotalCount, Is.EqualTo(0));
                Assert.That(pool.AvailableCount, Is.EqualTo(0));
                Assert.That(pool.ActiveObjectCount, Is.EqualTo(0));
            }
        }

        private sealed class PooledItem : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
