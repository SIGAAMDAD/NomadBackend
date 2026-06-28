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
    public sealed class BitSetSparseDictionaryTests
    {
        [Test]
        public void BitSetSparseDictionary_SetGetRemoveAndSlotAccess_ManageSparseKeys()
        {
            var map = new BitSetSparseDictionary<string>(1);

            map.Set(1, "one");
            map.Set(130, "one-thirty");
            map.Set(130, "updated");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(map.Count, Is.EqualTo(2));
                Assert.That(map.PageCount, Is.GreaterThanOrEqualTo(2));
                Assert.That(map.ContainsKey(1), Is.True);
                Assert.That(map.ContainsKey(130), Is.True);
                Assert.That(map.TryGetValue(130, out string? value), Is.True);
                Assert.That(value, Is.EqualTo("updated"));
            }

            int slot = map.GetPageKeyBySlot(0) == 2 ? 0 : 1;
            int local = 130 & 63;
            Assert.That(map.GetOccupiedMaskBySlot(slot), Is.Not.EqualTo(0UL));
            Assert.That(map.GetValueBySlotLocal(slot, local), Is.EqualTo("updated"));
            map.GetValueBySlotLocalRef(slot, local) = "ref-updated";
            Assert.That(map.TryGetValue(130, out string? refValue), Is.True);
            Assert.That(refValue, Is.EqualTo("ref-updated"));

            Assert.That(map.Remove(1), Is.True);
            Assert.That(map.Remove(1), Is.False);
            map.Clear();
            Assert.That(map.Count, Is.EqualTo(0));
        }
    }
}
