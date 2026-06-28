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
using Nomad.Networking.Session;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Session
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Session")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkPeerInfoTests
    {
        [Test]
        public void Constructor_StoresValues()
        {
            PeerId peer = new PeerId(Guid.NewGuid());

            var info = new NetworkPeerInfo(peer, "Noah", true, false, true, 2, NetworkConnectionState.Connected);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.PeerId, Is.EqualTo(peer));
                Assert.That(info.DisplayName, Is.EqualTo("Noah"));
                Assert.That(info.IsHost, Is.True);
                Assert.That(info.IsLocal, Is.False);
                Assert.That(info.IsReady, Is.True);
                Assert.That(info.PlayerSlot, Is.EqualTo(2));
                Assert.That(info.State, Is.EqualTo(NetworkConnectionState.Connected));
            }
        }
    }
}
