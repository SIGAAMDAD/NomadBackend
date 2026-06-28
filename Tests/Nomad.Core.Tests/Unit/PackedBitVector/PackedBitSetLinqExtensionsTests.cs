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
    public sealed class PackedBitSetLinqExtensionsTests
    {
        [Test]
        public void PackedBitSetLinqExtensions_SetBitsToArrayAndForEach_EnumerateSetBits()
        {
            var bits = new PackedBitSet8();
            bits.Set(0);
            bits.Set(3);

            var visited = new List<int>();
            bits.ForEachSetBit(visited.Add);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bits.AsEnumerable().ToArray(), Is.EqualTo(new[] { 0, 3 }));
                Assert.That(bits.SetBits().ToArray(), Is.EqualTo(new[] { 0, 3 }));
                Assert.That(bits.ToSetBitArray(), Is.EqualTo(new[] { 0, 3 }));
                Assert.That(visited, Is.EqualTo(new[] { 0, 3 }));
            }
        }

        [Test]
        public void PackedBitSetLinqExtensions_DenseAndSparseCaches_EnumerateGlobalBitIds()
        {
            var dense = new PackedBitSet8DenseCache(16);
            dense.Set(9);

            var sparse = new PackedBitSet8SparseCache(1);
            sparse.Set(17);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dense.AsEnumerable().ToArray(), Is.EqualTo(new[] { 9 }));
                Assert.That(dense.SetBits().ToArray(), Is.EqualTo(new[] { 9 }));
                Assert.That(sparse.AsEnumerable().ToArray(), Is.EqualTo(new[] { 17 }));
                Assert.That(sparse.SetBits().ToArray(), Is.EqualTo(new[] { 17 }));
            }
        }

        [Test]
        public void PackedBitSetLinqExtensions_ForEachSetBit_ThrowsWhenActionIsNull()
        {
            var bits = new PackedBitSet8();

            Assert.Throws<ArgumentNullException>(() => bits.ForEachSetBit(null!));
        }
    }
}
