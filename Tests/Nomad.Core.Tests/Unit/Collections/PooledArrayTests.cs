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
    public sealed class PooledArrayTests
    {
        [Test]
        public void PooledArray_IndexerSpanClearAndDispose_WorkOverRentedArray()
        {
            using var array = new PooledArray<int>(3);
            array[0] = 1;
            array[1] = 2;
            array[2] = 3;

            Assert.That(array.Span.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));

            array.ClearUsed();
            Assert.That(array.Span.ToArray(), Is.EqualTo(new[] { 0, 0, 0 }));
        }

        [Test]
        public void PooledArray_Constructor_ThrowsForNegativeLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PooledArray<int>(-1));
        }
    }
}
