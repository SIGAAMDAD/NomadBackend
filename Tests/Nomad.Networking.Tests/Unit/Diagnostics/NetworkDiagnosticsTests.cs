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

using Nomad.Networking.Private.Diagnostics;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Diagnostics
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Diagnostics")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkDiagnosticsTests
    {
        [Test]
        public void RecordingMethods_AccumulateStats()
        {
            var diagnostics = new NetworkDiagnostics();

            diagnostics.RecordPacketSent(12);
            diagnostics.RecordPacketSent(8);
            diagnostics.RecordPacketReceived(5);
            diagnostics.RecordPacketDropped();
            diagnostics.RecordDeserializeFailure();
            diagnostics.RecordUnknownMessageId();
            diagnostics.RecordAuthorityReject();

            var stats = diagnostics.Stats;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stats.PacketsSent, Is.EqualTo(2));
                Assert.That(stats.BytesSent, Is.EqualTo(20));
                Assert.That(stats.PacketsReceived, Is.EqualTo(1));
                Assert.That(stats.BytesReceived, Is.EqualTo(5));
                Assert.That(stats.PacketsDropped, Is.EqualTo(1));
                Assert.That(stats.DeserializeFailures, Is.EqualTo(1));
                Assert.That(stats.UnknownMessageIds, Is.EqualTo(1));
                Assert.That(stats.AuthorityRejects, Is.EqualTo(1));
            }
        }

        [Test]
        public void Reset_ClearsAllCounters()
        {
            var diagnostics = new NetworkDiagnostics();
            diagnostics.RecordPacketSent(12);
            diagnostics.RecordPacketReceived(5);
            diagnostics.RecordPacketDropped();
            diagnostics.RecordDeserializeFailure();
            diagnostics.RecordUnknownMessageId();
            diagnostics.RecordAuthorityReject();

            diagnostics.Reset();
            var stats = diagnostics.Stats;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stats.PacketsSent, Is.EqualTo(0));
                Assert.That(stats.BytesSent, Is.EqualTo(0));
                Assert.That(stats.PacketsReceived, Is.EqualTo(0));
                Assert.That(stats.BytesReceived, Is.EqualTo(0));
                Assert.That(stats.PacketsDropped, Is.EqualTo(0));
                Assert.That(stats.DeserializeFailures, Is.EqualTo(0));
                Assert.That(stats.UnknownMessageIds, Is.EqualTo(0));
                Assert.That(stats.AuthorityRejects, Is.EqualTo(0));
            }
        }
    }
}
