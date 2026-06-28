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
    public sealed class CollectionLinqExtensionsTests
    {
        private enum TestEnum
        {
            First,
            Second
        }

        [Test]
        public void CollectionLinqExtensions_AsEnumerable_ReturnsCollectionItems()
        {
            var arena = new Arena<int>();
            arena.Add(1);
            arena.Add(2);

            using var list = new PooledList<int>();
            list.Add(3);
            list.Add(4);

            var set = new SparseSet();
            set.Add(5);
            set.Add(6);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(arena.AsEnumerable().ToArray(), Is.EqualTo(new[] { 1, 2 }));
                Assert.That(list.AsEnumerable().ToArray(), Is.EqualTo(new[] { 3, 4 }));
                Assert.That(set.AsEnumerable().ToArray(), Is.EquivalentTo(new[] { 5, 6 }));
            }
        }

        [Test]
        public void CollectionLinqExtensions_AsPairs_ReturnsMapPairs()
        {
            var sparse = new SparseSet<string>();
            sparse.Set(1, "one");

            var enumMap = new PackedEnumMap<TestEnum, int>();
            enumMap.Set(TestEnum.Second, 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sparse.AsPairs().Single().Value, Is.EqualTo("one"));
                Assert.That(sparse.Ids().Single(), Is.EqualTo(1));
                Assert.That(sparse.Values().Single(), Is.EqualTo("one"));
                Assert.That(enumMap.AsPairs().Single().Key, Is.EqualTo(TestEnum.Second));
                Assert.That(enumMap.AsPairs().Single().Value, Is.EqualTo(2));
            }
        }
    }
}
