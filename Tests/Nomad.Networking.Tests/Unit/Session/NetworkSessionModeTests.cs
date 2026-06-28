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

using Nomad.Networking.Session;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Session
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Session")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSessionModeTests
    {
        [Test]
        public void Values_AreStableProtocolValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((byte)NetworkSessionMode.Offline, Is.EqualTo(0));
                Assert.That((byte)NetworkSessionMode.Host, Is.EqualTo(1));
                Assert.That((byte)NetworkSessionMode.Client, Is.EqualTo(2));
                Assert.That((byte)NetworkSessionMode.DedicatedServer, Is.EqualTo(3));
            }
        }
    }
}
