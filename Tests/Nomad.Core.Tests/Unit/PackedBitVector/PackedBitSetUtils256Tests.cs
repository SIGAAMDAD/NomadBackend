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
    public sealed class PackedBitSetUtils256Tests
    {
        [Test]
        public void PackedBitSetUtils256_BooleanAlgebraAndCounting_ReturnExpectedResults()
        {
            var first = new PackedBitSet256();
            var last = new PackedBitSet256();
            first.Set(0);
            last.Set(255);

            PackedBitSet256 union = PackedBitSetUtils256.Or(in first, in last);
            PackedBitSet256 intersection = PackedBitSetUtils256.And(in first, in last);
            PackedBitSet256 xor = PackedBitSetUtils256.Xor(in first, in last);
            PackedBitSet256 withoutLast = PackedBitSetUtils256.AndNot(in union, in last);
            PackedBitSet256 notFirst = PackedBitSetUtils256.Not(in first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PackedBitSetUtils256.IsEmpty(in first), Is.False);
                Assert.That(PackedBitSetUtils256.Equals(in first, in first), Is.True);
                Assert.That(PackedBitSetUtils256.Intersects(in first, in last), Is.False);
                Assert.That(PackedBitSetUtils256.ContainsAll(in union, in first), Is.True);
                Assert.That(PackedBitSetUtils256.PopCount(in union), Is.EqualTo(2));
                Assert.That(PackedBitSetUtils256.FirstSetBit(in union), Is.EqualTo(0));
                Assert.That(intersection.IsEmpty, Is.True);
                Assert.That(xor.Get(0), Is.True);
                Assert.That(xor.Get(255), Is.True);
                Assert.That(withoutLast.Get(0), Is.True);
                Assert.That(withoutLast.Get(255), Is.False);
                Assert.That(notFirst.Get(0), Is.False);
                Assert.That(notFirst.Get(255), Is.True);
            }

            PackedBitSetUtils256.OrInPlace(ref first, in last);
            Assert.That(first.Get(255), Is.True);
            PackedBitSetUtils256.AndInPlace(ref first, in last);
            Assert.That(first.Get(0), Is.False);
            PackedBitSetUtils256.XorInPlace(ref first, in last);
            Assert.That(first.IsEmpty, Is.True);
        }
    }
}
