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
using Nomad.Core.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Collections")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class PooledBinaryHeapTComparerTests
    {
        private readonly struct DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y) => y.CompareTo(x);
        }

        [Test]
        public void PooledBinaryHeap_WithStructComparer_UsesProvidedComparer()
        {
            using var heap = new PooledBinaryHeap<int, DescendingComparer>(1, new DescendingComparer());

            heap.Push(1);
            heap.Push(3);
            heap.Push(2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(heap.Count, Is.EqualTo(3));
                Assert.That(heap.PeekMinRef(), Is.EqualTo(3));
                Assert.That(heap.PopMin(), Is.EqualTo(3));
                Assert.That(heap.PopMin(), Is.EqualTo(2));
                Assert.That(heap.PopMin(), Is.EqualTo(1));
            }
        }
    }
}
