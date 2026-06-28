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
    public sealed class StringIdTableTests
    {
        [Test]
        public void StringIdTable_GetOrAddTryGetIdAndGetString_RoundTripStrings()
        {
            var table = new StringIdTable(1, StringComparer.OrdinalIgnoreCase);

            int alpha = table.GetOrAdd("Alpha");
            int same = table.GetOrAdd("alpha");
            int beta = table.GetOrAdd("Beta");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alpha, Is.EqualTo(same));
                Assert.That(beta, Is.Not.EqualTo(alpha));
                Assert.That(table.Count, Is.EqualTo(2));
                Assert.That(table.TryGetId("ALPHA", out int found), Is.True);
                Assert.That(found, Is.EqualTo(alpha));
                Assert.That(table.GetString(beta), Is.EqualTo("Beta"));
                Assert.That(table.ToArray(), Does.Contain("Alpha"));
            }
        }
    }
}
