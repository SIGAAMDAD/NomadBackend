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
    public sealed class SmallList16Tests
    {
        [Test]
        public void SmallList16_TryAddIndexerAndClear_RespectFixedCapacity()
        {
            var list = new SmallList16<int>();

            for (int i = 0; i < 16; i++)
            {
                Assert.That(list.TryAdd(i), Is.True);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.TryAdd(99), Is.False);
                Assert.That(list.Count, Is.EqualTo(16));
                Assert.That(list.Capacity, Is.EqualTo(16));
                Assert.That(list[0], Is.EqualTo(0));
                Assert.That(list[15], Is.EqualTo(15));
            }

            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
        }
    }
}
