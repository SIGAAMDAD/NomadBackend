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
    public sealed class PackedBitSetUtils128Tests
    {
        [Test]
        public void PackedBitSetUtils128_BooleanAlgebraAndCounting_ReturnExpectedResults()
        {
            var first = new PackedBitSet128();
            var last = new PackedBitSet128();
            first.Set(0);
            last.Set(127);

            PackedBitSet128 union = PackedBitSetUtils128.Or(in first, in last);
            PackedBitSet128 intersection = PackedBitSetUtils128.And(in first, in last);
            PackedBitSet128 xor = PackedBitSetUtils128.Xor(in first, in last);
            PackedBitSet128 withoutLast = PackedBitSetUtils128.AndNot(in union, in last);
            PackedBitSet128 notFirst = PackedBitSetUtils128.Not(in first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PackedBitSetUtils128.IsEmpty(in first), Is.False);
                Assert.That(PackedBitSetUtils128.Equals(in first, in first), Is.True);
                Assert.That(PackedBitSetUtils128.Intersects(in first, in last), Is.False);
                Assert.That(PackedBitSetUtils128.ContainsAll(in union, in first), Is.True);
                Assert.That(PackedBitSetUtils128.PopCount(in union), Is.EqualTo(2));
                Assert.That(PackedBitSetUtils128.FirstSetBit(in union), Is.EqualTo(0));
                Assert.That(intersection.IsEmpty, Is.True);
                Assert.That(xor.Get(0), Is.True);
                Assert.That(xor.Get(127), Is.True);
                Assert.That(withoutLast.Get(0), Is.True);
                Assert.That(withoutLast.Get(127), Is.False);
                Assert.That(notFirst.Get(0), Is.False);
                Assert.That(notFirst.Get(127), Is.True);
            }

            PackedBitSetUtils128.OrInPlace(ref first, in last);
            Assert.That(first.Get(127), Is.True);
            PackedBitSetUtils128.AndInPlace(ref first, in last);
            Assert.That(first.Get(0), Is.False);
            PackedBitSetUtils128.XorInPlace(ref first, in last);
            Assert.That(first.IsEmpty, Is.True);
        }
    }
}
