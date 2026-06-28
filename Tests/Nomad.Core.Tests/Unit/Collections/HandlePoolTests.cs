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
    public sealed class HandlePoolTests
    {
        [Test]
        public void HandlePool_AllocateGetFreeAndIsAlive_ManageGenerationalHandles()
        {
            var pool = new HandlePool<string>(1);

            Handle handle = pool.Allocate("first");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pool.Count, Is.EqualTo(1));
                Assert.That(pool.IsAlive(handle), Is.True);
                Assert.That(pool.Get(handle), Is.EqualTo("first"));
            }

            pool.Get(handle) = "updated";
            Assert.That(pool.Get(handle), Is.EqualTo("updated"));
            Assert.That(pool.Free(handle), Is.True);
            Assert.That(pool.Free(handle), Is.False);
            Assert.That(pool.IsAlive(handle), Is.False);
        }
    }
}
