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
    public sealed class NetworkSessionInfoTests
    {
        [Test]
        public void InitProperties_StoreSessionSnapshot()
        {
            PeerId local = new PeerId(Guid.NewGuid());
            PeerId host = new PeerId(Guid.NewGuid());
            var peer = new NetworkPeerInfo(local, "Local", true, true, true, 0, NetworkConnectionState.Connected);
            DateTime updated = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            var info = new NetworkSessionInfo
            {
                SessionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                LobbyId = new LobbyId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
                Mode = NetworkSessionMode.Host,
                State = NetworkConnectionState.Connected,
                MinPlayers = 1,
                MaxPlayers = 8,
                PeerCount = 1,
                LocalPeerId = local,
                HostPeerId = host,
                Peers = new[] { peer },
                LastUpdatedUtc = updated
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.Mode, Is.EqualTo(NetworkSessionMode.Host));
                Assert.That(info.State, Is.EqualTo(NetworkConnectionState.Connected));
                Assert.That(info.MinPlayers, Is.EqualTo(1));
                Assert.That(info.MaxPlayers, Is.EqualTo(8));
                Assert.That(info.PeerCount, Is.EqualTo(1));
                Assert.That(info.LocalPeerId, Is.EqualTo(local));
                Assert.That(info.HostPeerId, Is.EqualTo(host));
                Assert.That(info.Peers, Has.Count.EqualTo(1));
                Assert.That(info.LastUpdatedUtc, Is.EqualTo(updated));
            }
        }

        [Test]
        public void Defaults_UseEmptyPeerListAndUtcTimestamp()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var info = new NetworkSessionInfo();
            var after = DateTime.UtcNow.AddSeconds(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.Peers, Is.Empty);
                Assert.That(info.LastUpdatedUtc, Is.InRange(before, after));
            }
        }
    }
}
