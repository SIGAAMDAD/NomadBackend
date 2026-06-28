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
    public sealed class DenseIdMapTests
    {
        [Test]
        public void DenseIdMap_SetTryGetRemoveAndClear_ManageDenseIds()
        {
            var map = new DenseIdMap<string>(1);

            map.Set(3, "three");
            map.Set(7, "seven");
            map.Set(3, "updated");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(map.Count, Is.EqualTo(2));
                Assert.That(map.Contains(3), Is.True);
                Assert.That(map.TryGetValue(3, out string? value), Is.True);
                Assert.That(value, Is.EqualTo("updated"));
                Assert.That(map.TryGetValue(99, out _), Is.False);
            }

            Assert.That(map.Remove(7), Is.True);
            Assert.That(map.Remove(7), Is.False);
            map.EnsureCapacity(128);
            map.Clear();
            Assert.That(map.Count, Is.EqualTo(0));
        }
    }
}
