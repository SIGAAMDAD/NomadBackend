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
    public sealed class SparseSetTests
    {
        [Test]
        public void SparseSet_AddRemoveDenseIdsAndClear_ManageIds()
        {
            var set = new SparseSet(1, 1);

            Assert.That(set.Add(4), Is.True);
            Assert.That(set.Add(8), Is.True);
            Assert.That(set.Add(4), Is.False);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(set.Count, Is.EqualTo(2));
                Assert.That(set.Contains(4), Is.True);
                Assert.That(set.GetDenseId(0), Is.EqualTo(4));
                Assert.That(set.ToArray(), Is.EquivalentTo(new[] { 4, 8 }));
            }

            set.EnsureIdCapacity(128);
            Assert.That(set.Remove(4), Is.True);
            Assert.That(set.Remove(4), Is.False);
            set.Clear();
            Assert.That(set.Count, Is.EqualTo(0));
        }
    }
}
