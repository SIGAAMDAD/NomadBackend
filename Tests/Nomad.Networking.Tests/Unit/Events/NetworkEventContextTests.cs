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
using Nomad.Core.OnlineServices;
using Nomad.Networking.Events;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Events
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Events")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkEventContextTests
    {
        [Test]
        public void Constructor_StoresValues()
        {
            PeerId sender = new PeerId(Guid.NewGuid());

            var context = new NetworkEventContext(sender, 1234, true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.Sender, Is.EqualTo(sender));
                Assert.That(context.Tick, Is.EqualTo(1234));
                Assert.That(context.FromHost, Is.True);
            }
        }
    }
}
