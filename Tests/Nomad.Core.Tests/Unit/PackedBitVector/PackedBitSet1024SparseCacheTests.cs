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
using System.Collections.Generic;
using System.Linq;
using Nomad.Core.Util.PackedBitVector;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("PackedBitVector")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class PackedBitSet1024SparseCacheTests
    {
        [Test]
        public void PackedBitSet1024SparseCache_SetUnsetToggleDirtyAndClear_UpdateSparsePages()
        {
            var cache = new PackedBitSet1024SparseCache(1);
            int highBit = 2048 + 3;
            int expectedPage = 2;

            cache.Set(highBit);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cache.PageCount, Is.EqualTo(1));
                Assert.That(cache.Get(highBit), Is.True);
                Assert.That(cache.GetPageKeyBySlot(0), Is.EqualTo(expectedPage));
                Assert.That(cache.IsDirtyBySlot(0), Is.True);
                Assert.That(cache.GetPageBySlot(0).Get(3), Is.True);
            }

            cache.MarkCleanBySlot(0);
            Assert.That(cache.IsDirtyBySlot(0), Is.False);

            Assert.That(cache.Toggle(highBit), Is.False);
            cache.Set(highBit);
            cache.Unset(highBit);
            Assert.That(cache.Get(highBit), Is.False);

            cache.Clear();
            Assert.That(cache.PageCount, Is.EqualTo(0));
        }
    }
}
