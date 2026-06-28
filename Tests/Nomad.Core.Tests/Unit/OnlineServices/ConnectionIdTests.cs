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
    public sealed class ConnectionIdTests
    {
        [Test]
        public void ConnectionId_EqualityAndInvalidSentinel_UseUnderlyingValue()
        {
            var first = new ConnectionId(7);
            var second = new ConnectionId(7);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first.Equals((object)second), Is.True);
                Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
                Assert.That(ConnectionId.Invalid, Is.EqualTo(new ConnectionId(ushort.MaxValue)));
            }
        }
    }
}
