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
    public sealed class BitMatrixTests
    {
        [Test]
        public void BitMatrix_SetGetAndClear_UpdateIndividualCells()
        {
            var matrix = new BitMatrix(3, 2);

            matrix.Set(1, 0, true);
            matrix.Set(2, 1, true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matrix.Width, Is.EqualTo(3));
                Assert.That(matrix.Height, Is.EqualTo(2));
                Assert.That(matrix.Get(1, 0), Is.True);
                Assert.That(matrix.Get(2, 1), Is.True);
                Assert.That(matrix.Get(0, 0), Is.False);
            }

            matrix.Set(1, 0, false);
            matrix.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matrix.Get(1, 0), Is.False);
                Assert.That(matrix.Get(2, 1), Is.False);
            }
        }
    }
}
