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

using Nomad.Core.OnlineServices;
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.ValueObjects
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("ValueObjects")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSendPolicyTests
    {
        [Test]
        public void Constructor_StoresModeAndChannel()
        {
            var policy = new NetworkSendPolicy(NetworkSendMode.Unreliable, NetworkChannel.Debug);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(policy.Mode, Is.EqualTo(NetworkSendMode.Unreliable));
                Assert.That(policy.Channel, Is.EqualTo(NetworkChannel.Debug));
            }
        }

        [Test]
        public void Presets_UseExpectedModesAndChannels()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NetworkSendPolicy.ReliableControl.Mode, Is.EqualTo(NetworkSendMode.Reliable));
                Assert.That(NetworkSendPolicy.ReliableControl.Channel, Is.EqualTo(NetworkChannel.Control));
                Assert.That(NetworkSendPolicy.UnreliableInput.Mode, Is.EqualTo(NetworkSendMode.UnreliableNoDelay));
                Assert.That(NetworkSendPolicy.UnreliableInput.Channel, Is.EqualTo(NetworkChannel.Input));
                Assert.That(NetworkSendPolicy.UnreliableSnapshot.Mode, Is.EqualTo(NetworkSendMode.Unreliable));
                Assert.That(NetworkSendPolicy.UnreliableSnapshot.Channel, Is.EqualTo(NetworkChannel.Snapshot));
            }
        }
    }
}
