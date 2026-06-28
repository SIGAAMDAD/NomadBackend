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
    public sealed class PackedBitSetUtils1024Tests
    {
        [Test]
        public void PackedBitSetUtils1024_BooleanAlgebraAndCounting_ReturnExpectedResults()
        {
            var first = new PackedBitSet1024();
            var last = new PackedBitSet1024();
            first.Set(0);
            last.Set(1023);

            PackedBitSet1024 union = PackedBitSetUtils1024.Or(in first, in last);
            PackedBitSet1024 intersection = PackedBitSetUtils1024.And(in first, in last);
            PackedBitSet1024 xor = PackedBitSetUtils1024.Xor(in first, in last);
            PackedBitSet1024 withoutLast = PackedBitSetUtils1024.AndNot(in union, in last);
            PackedBitSet1024 notFirst = PackedBitSetUtils1024.Not(in first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PackedBitSetUtils1024.IsEmpty(in first), Is.False);
                Assert.That(PackedBitSetUtils1024.Equals(in first, in first), Is.True);
                Assert.That(PackedBitSetUtils1024.Intersects(in first, in last), Is.False);
                Assert.That(PackedBitSetUtils1024.ContainsAll(in union, in first), Is.True);
                Assert.That(PackedBitSetUtils1024.PopCount(in union), Is.EqualTo(2));
                Assert.That(PackedBitSetUtils1024.FirstSetBit(in union), Is.EqualTo(0));
                Assert.That(intersection.IsEmpty, Is.True);
                Assert.That(xor.Get(0), Is.True);
                Assert.That(xor.Get(1023), Is.True);
                Assert.That(withoutLast.Get(0), Is.True);
                Assert.That(withoutLast.Get(1023), Is.False);
                Assert.That(notFirst.Get(0), Is.False);
                Assert.That(notFirst.Get(1023), Is.True);
            }

            PackedBitSetUtils1024.OrInPlace(ref first, in last);
            Assert.That(first.Get(1023), Is.True);
            PackedBitSetUtils1024.AndInPlace(ref first, in last);
            Assert.That(first.Get(0), Is.False);
            PackedBitSetUtils1024.XorInPlace(ref first, in last);
            Assert.That(first.IsEmpty, Is.True);
        }
    }
}
