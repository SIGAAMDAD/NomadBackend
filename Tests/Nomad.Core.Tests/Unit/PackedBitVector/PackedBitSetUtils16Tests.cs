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
    public sealed class PackedBitSetUtils16Tests
    {
        [Test]
        public void PackedBitSetUtils16_BooleanAlgebraAndCounting_ReturnExpectedResults()
        {
            var first = new PackedBitSet16();
            var last = new PackedBitSet16();
            first.Set(0);
            last.Set(15);

            PackedBitSet16 union = PackedBitSetUtils16.Or(in first, in last);
            PackedBitSet16 intersection = PackedBitSetUtils16.And(in first, in last);
            PackedBitSet16 xor = PackedBitSetUtils16.Xor(in first, in last);
            PackedBitSet16 withoutLast = PackedBitSetUtils16.AndNot(in union, in last);
            PackedBitSet16 notFirst = PackedBitSetUtils16.Not(in first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PackedBitSetUtils16.IsEmpty(in first), Is.False);
                Assert.That(PackedBitSetUtils16.Equals(in first, in first), Is.True);
                Assert.That(PackedBitSetUtils16.Intersects(in first, in last), Is.False);
                Assert.That(PackedBitSetUtils16.ContainsAll(in union, in first), Is.True);
                Assert.That(PackedBitSetUtils16.PopCount(in union), Is.EqualTo(2));
                Assert.That(PackedBitSetUtils16.FirstSetBit(in union), Is.EqualTo(0));
                Assert.That(intersection.IsEmpty, Is.True);
                Assert.That(xor.Get(0), Is.True);
                Assert.That(xor.Get(15), Is.True);
                Assert.That(withoutLast.Get(0), Is.True);
                Assert.That(withoutLast.Get(15), Is.False);
                Assert.That(notFirst.Get(0), Is.False);
                Assert.That(notFirst.Get(15), Is.True);
            }

            PackedBitSetUtils16.OrInPlace(ref first, in last);
            Assert.That(first.Get(15), Is.True);
            PackedBitSetUtils16.AndInPlace(ref first, in last);
            Assert.That(first.Get(0), Is.False);
            PackedBitSetUtils16.XorInPlace(ref first, in last);
            Assert.That(first.IsEmpty, Is.True);
        }
    }
}
