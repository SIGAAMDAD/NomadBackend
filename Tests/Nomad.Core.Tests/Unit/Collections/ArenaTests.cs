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
    public sealed class ArenaTests
    {
        [Test]
        public void Arena_AddAndGet_TracksInsertedValues()
        {
            var arena = new Arena<int>(1);

            int first = arena.Add(10);
            int second = arena.Add(20);
            ref int uninitialized = ref arena.AddUninitialized(out int third);
            uninitialized = 30;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.EqualTo(0));
                Assert.That(second, Is.EqualTo(1));
                Assert.That(third, Is.EqualTo(2));
                Assert.That(arena.Count, Is.EqualTo(3));
                Assert.That(arena.Get(0), Is.EqualTo(10));
                Assert.That(arena.Get(1), Is.EqualTo(20));
                Assert.That(arena.Get(2), Is.EqualTo(30));
                Assert.That(arena.Span.ToArray(), Is.EqualTo(new[] { 10, 20, 30 }));
            }
        }

        [Test]
        public void Arena_Reset_ClearsLogicalCountWithoutDroppingCapacity()
        {
            var arena = new Arena<string>(0);
            arena.Add("one");

            arena.Reset();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(arena.Count, Is.EqualTo(0));
                Assert.That(arena.Span.Length, Is.EqualTo(0));
            }
        }
    }
}
