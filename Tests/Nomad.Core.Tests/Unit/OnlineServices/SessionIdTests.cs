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
using Nomad.Core.OnlineServices;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("OnlineServices")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class SessionIdTests
    {
        [Test]
        public void SessionId_EqualityOperatorsAndHashCode_UseUnderlyingGuid()
        {
            Guid id = Guid.NewGuid();
            var first = new SessionId(id);
            var second = new SessionId(id);
            var other = new SessionId(Guid.NewGuid());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first == second, Is.True);
                Assert.That(first != other, Is.True);
                Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
                Assert.That(first.ToString(), Is.EqualTo(id.ToString("N")));
            }
        }
    }
}
