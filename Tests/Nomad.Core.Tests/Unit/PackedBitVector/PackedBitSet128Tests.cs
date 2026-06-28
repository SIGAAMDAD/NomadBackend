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
using System.Collections.Generic;
using System.Linq;
using Nomad.Core.Util.PackedBitVector;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("PackedBitVector")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class PackedBitSet128Tests
    {
        [Test]
        public void PackedBitSet128_SetUnsetToggleSetAllClearAndSanitize_UpdateExpectedBits()
        {
            var bits = new PackedBitSet128();

            Assert.That(bits.IsEmpty, Is.True);

            bits.Set(0);
            bits.Set(127);
            bits[1] = true;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bits.Get(0), Is.True);
                Assert.That(bits[1], Is.True);
                Assert.That(bits.Get(127), Is.True);
                Assert.That(bits.IsEmpty, Is.False);
            }

            bits.Unset(0);
            bits.Set(1, false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bits.Get(0), Is.False);
                Assert.That(bits.Get(1), Is.False);
                Assert.That(bits.Get(127), Is.True);
            }

            Assert.That(bits.Toggle(0), Is.True);
            Assert.That(bits.Toggle(0), Is.False);

            bits.SetAll();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(bits.Get(0), Is.True);
                Assert.That(bits.Get(127), Is.True);
            }

            bits.Clear();
            Assert.That(bits.IsEmpty, Is.True);

        }
    }
}
