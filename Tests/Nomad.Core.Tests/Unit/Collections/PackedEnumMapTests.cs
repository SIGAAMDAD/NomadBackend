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
    public sealed class PackedEnumMapTests
    {
        private enum TestEnum
        {
            First = 0,
            Second = 2
        }

        [Test]
        public void PackedEnumMap_SetContainsAndIndexer_StoreValuesByEnumOrdinal()
        {
            var map = new PackedEnumMap<TestEnum, string>();

            map.Set(TestEnum.Second, "two");
            map[TestEnum.First] = "one";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(map.Contains(TestEnum.Second), Is.True);
                Assert.That(map.Contains(TestEnum.First), Is.False);
                Assert.That(map[TestEnum.Second], Is.EqualTo("two"));
                Assert.That(map[TestEnum.First], Is.EqualTo("one"));
            }
        }
    }
}
