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
    public sealed class PackedBitSetUtils512Tests
    {
        [Test]
        public void PackedBitSetUtils512_BooleanAlgebraAndCounting_ReturnExpectedResults()
        {
            var first = new PackedBitSet512();
            var last = new PackedBitSet512();
            first.Set(0);
            last.Set(511);

            PackedBitSet512 union = PackedBitSetUtils512.Or(in first, in last);
            PackedBitSet512 intersection = PackedBitSetUtils512.And(in first, in last);
            PackedBitSet512 xor = PackedBitSetUtils512.Xor(in first, in last);
            PackedBitSet512 withoutLast = PackedBitSetUtils512.AndNot(in union, in last);
            PackedBitSet512 notFirst = PackedBitSetUtils512.Not(in first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PackedBitSetUtils512.IsEmpty(in first), Is.False);
                Assert.That(PackedBitSetUtils512.Equals(in first, in first), Is.True);
                Assert.That(PackedBitSetUtils512.Intersects(in first, in last), Is.False);
                Assert.That(PackedBitSetUtils512.ContainsAll(in union, in first), Is.True);
                Assert.That(PackedBitSetUtils512.PopCount(in union), Is.EqualTo(2));
                Assert.That(PackedBitSetUtils512.FirstSetBit(in union), Is.EqualTo(0));
                Assert.That(intersection.IsEmpty, Is.True);
                Assert.That(xor.Get(0), Is.True);
                Assert.That(xor.Get(511), Is.True);
                Assert.That(withoutLast.Get(0), Is.True);
                Assert.That(withoutLast.Get(511), Is.False);
                Assert.That(notFirst.Get(0), Is.False);
                Assert.That(notFirst.Get(511), Is.True);
            }

            PackedBitSetUtils512.OrInPlace(ref first, in last);
            Assert.That(first.Get(511), Is.True);
            PackedBitSetUtils512.AndInPlace(ref first, in last);
            Assert.That(first.Get(0), Is.False);
            PackedBitSetUtils512.XorInPlace(ref first, in last);
            Assert.That(first.IsEmpty, Is.True);
        }
    }
}
