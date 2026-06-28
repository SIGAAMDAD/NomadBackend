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
    public sealed class PooledListTests
    {
        [Test]
        public void PooledList_AddAddRangeRemoveClearAndDispose_UpdateListState()
        {
            using var list = new PooledList<int>(1);

            list.Add(1);
            ref int second = ref list.AddUninitialized();
            second = 2;
            list.AddRange(new[] { 3, 4 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Count, Is.EqualTo(4));
                Assert.That(list.Span.ToArray(), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            }

            list.RemoveAtSwapBack(1);
            Assert.That(list.Span.ToArray(), Is.EquivalentTo(new[] { 1, 3, 4 }));

            list.EnsureCapacity(32);
            Assert.That(list.Capacity, Is.GreaterThanOrEqualTo(32));
            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
        }
    }
}
