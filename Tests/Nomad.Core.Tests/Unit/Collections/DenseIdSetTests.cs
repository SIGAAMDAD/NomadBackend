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
    public sealed class DenseIdSetTests
    {
        [Test]
        public void DenseIdSet_AddContainsRemoveAndClear_ManageDenseIds()
        {
            var set = new DenseIdSet(1);

            Assert.That(set.Add(70), Is.True);
            Assert.That(set.Add(70), Is.False);
            set.EnsureCapacity(256);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(set.Count, Is.EqualTo(1));
                Assert.That(set.Contains(70), Is.True);
                Assert.That(set.Contains(71), Is.False);
            }

            Assert.That(set.Remove(70), Is.True);
            Assert.That(set.Remove(70), Is.False);
            set.Clear();
            Assert.That(set.Count, Is.EqualTo(0));
        }
    }
}
