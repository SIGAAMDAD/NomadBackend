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
    public sealed class CollectionStandardExtensionsTests
    {
        [Test]
        public void CollectionStandardExtensions_PooledListHelpers_MutateAndQueryList()
        {
            using var list = new PooledList<int>();
            list.AddRange(new[] { 3, 1, 2, 2 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Contains(1), Is.True);
                Assert.That(list.IndexOf(2), Is.EqualTo(2));
                Assert.That(list.ToArray(), Is.EqualTo(new[] { 3, 1, 2, 2 }));
            }

            Assert.That(list.RemoveAll(value => value == 2), Is.EqualTo(2));
            list.Sort();
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 3 }));

            list.Reverse();
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 3, 1 }));
            Assert.That(list.BinarySearch(3), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void CollectionStandardExtensions_SmallListToArray_ReturnsStoredValues()
        {
            var list = new SmallList4<int>();
            list.TryAdd(1);
            list.TryAdd(2);

            Assert.That(list.ToArray(), Is.EqualTo(new[] { 1, 2 }));
        }
    }
}
