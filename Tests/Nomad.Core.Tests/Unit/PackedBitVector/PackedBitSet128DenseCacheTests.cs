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
    public sealed class PackedBitSet128DenseCacheTests
    {
        [Test]
        public void PackedBitSet128DenseCache_SetUnsetToggleDirtyAndClear_UpdatePages()
        {
            var cache = new PackedBitSet128DenseCache(257);
            int highBit = 128 + 1;
            int highPage = 1;

            cache.Set(highBit);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cache.PageCount, Is.GreaterThanOrEqualTo(3));
                Assert.That(cache.Get(highBit), Is.True);
                Assert.That(cache.IsDirty(highPage), Is.True);
                Assert.That(cache.GetPageByIndex(highPage).Get(1), Is.True);
            }

            cache.MarkClean(highPage);
            Assert.That(cache.IsDirty(highPage), Is.False);

            cache.Set(highBit, false);
            Assert.That(cache.Get(highBit), Is.False);
            Assert.That(cache.Toggle(highBit), Is.True);
            cache.Unset(highBit);
            Assert.That(cache.Get(highBit), Is.False);

            cache.Clear();
            Assert.That(cache.Get(highBit), Is.False);
        }
    }
}
