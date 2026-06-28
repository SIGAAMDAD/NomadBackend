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
    public sealed class SoA2Tests
    {
        [Test]
        public void SoA2_Add_StoresColumnsInSeparateSpans()
        {
            var soa = new SoA2<int, string>(1);

            int index = soa.Add(7, "seven");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(index, Is.EqualTo(0));
                Assert.That(soa.Count, Is.EqualTo(1));
                Assert.That(soa.A.ToArray(), Is.EqualTo(new[] { 7 }));
                Assert.That(soa.B.ToArray(), Is.EqualTo(new[] { "seven" }));
            }
        }
    }
}
