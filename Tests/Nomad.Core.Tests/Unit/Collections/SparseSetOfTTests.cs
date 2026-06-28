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
    public sealed class SparseSetOfTTests
    {
        [Test]
        public void SparseSetOfT_AddSetRemoveAndPairs_ManageIdsAndValues()
        {
            var set = new SparseSet<string>(1, 1);

            Assert.That(set.Add(4, "four"), Is.True);
            Assert.That(set.Add(4, "duplicate"), Is.False);
            set.Set(8, "eight");
            set.GetValueByDenseIndexRef(0) = "updated";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(set.Count, Is.EqualTo(2));
                Assert.That(set.Contains(4), Is.True);
                Assert.That(set.Values.ToArray(), Does.Contain("updated"));
                Assert.That(set.GetValueByDenseIndex(0), Is.EqualTo("updated"));
                Assert.That(set.ToPairArray().Select(pair => pair.Key), Is.EquivalentTo(new[] { 4, 8 }));
            }

            set.EnsureIdCapacity(128);
            Assert.That(set.Remove(8), Is.True);
            Assert.That(set.Remove(8), Is.False);
            set.Clear();
            Assert.That(set.Count, Is.EqualTo(0));
        }
    }
}
