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

using Nomad.Networking.Diagnostics;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Diagnostics
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Diagnostics")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkStatsTests
    {
        [Test]
        public void Constructor_StoresCounters()
        {
            var stats = new NetworkStats(1, 2, 3, 4, 5, 6, 7, 8);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stats.PacketsSent, Is.EqualTo(1));
                Assert.That(stats.PacketsReceived, Is.EqualTo(2));
                Assert.That(stats.BytesSent, Is.EqualTo(3));
                Assert.That(stats.BytesReceived, Is.EqualTo(4));
                Assert.That(stats.PacketsDropped, Is.EqualTo(5));
                Assert.That(stats.DeserializeFailures, Is.EqualTo(6));
                Assert.That(stats.UnknownMessageIds, Is.EqualTo(7));
                Assert.That(stats.AuthorityRejects, Is.EqualTo(8));
            }
        }
    }
}
