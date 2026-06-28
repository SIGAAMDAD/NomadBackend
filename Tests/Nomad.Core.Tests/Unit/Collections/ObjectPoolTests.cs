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
    public sealed class ObjectPoolTests
    {
        [Test]
        public void ObjectPool_RentAndReturn_ReusesInstancesAndRunsReset()
        {
            int created = 0;
            int reset = 0;
            var pool = new ObjectPool<List<int>>(
                () => { created++; return new List<int>(); },
                list => { reset++; list.Clear(); },
                initialCapacity: 1
            );

            List<int> first = pool.Rent();
            first.Add(42);
            pool.Return(first);
            List<int> second = pool.Rent();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(created, Is.EqualTo(1));
                Assert.That(reset, Is.EqualTo(1));
                Assert.That(second, Is.SameAs(first));
                Assert.That(second, Is.Empty);
            }
        }
    }
}
