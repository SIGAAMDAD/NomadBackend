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
using Nomad.Networking.Messaging;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Messaging
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Messaging")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkMessageRegistryTests
    {
        [Test]
        public void Register_MapsTypeAndIdBothWays()
        {
            var registry = new NetworkMessageRegistry();

            registry.Register<TestNetworkPayload>(17, NetworkMessageKind.Rpc);

            bool foundId = registry.TryGetId<TestNetworkPayload>(out ushort id);
            bool foundInfo = registry.TryGetInfo(17, out NetworkMessageInfo info);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundId, Is.True);
                Assert.That(id, Is.EqualTo(17));
                Assert.That(foundInfo, Is.True);
                Assert.That(info.Id, Is.EqualTo(17));
                Assert.That(info.Type, Is.EqualTo(typeof(TestNetworkPayload)));
                Assert.That(info.Kind, Is.EqualTo(NetworkMessageKind.Rpc));
            }
        }

        [Test]
        public void Register_ReRegisteringSameTypeAndId_IsIdempotent()
        {
            var registry = new NetworkMessageRegistry();

            registry.Register<TestNetworkPayload>(17, NetworkMessageKind.Rpc);
            registry.Register<TestNetworkPayload>(17, NetworkMessageKind.Event);
            registry.TryGetInfo(17, out NetworkMessageInfo info);

            Assert.That(info.Kind, Is.EqualTo(NetworkMessageKind.Event));
        }

        [Test]
        public void Register_WhenIdBelongsToDifferentType_ThrowsInvalidOperationException()
        {
            var registry = new NetworkMessageRegistry();
            registry.Register<TestNetworkPayload>(17, NetworkMessageKind.Rpc);

            Assert.Throws<InvalidOperationException>(() => registry.Register<AlternateNetworkPayload>(17, NetworkMessageKind.Rpc));
        }

        [Test]
        public void Register_WhenTypeAlreadyHasDifferentId_ThrowsInvalidOperationException()
        {
            var registry = new NetworkMessageRegistry();
            registry.Register<TestNetworkPayload>(17, NetworkMessageKind.Rpc);

            Assert.Throws<InvalidOperationException>(() => registry.Register<TestNetworkPayload>(18, NetworkMessageKind.Rpc));
        }

        [Test]
        public void TryGet_WhenMessageIsUnknown_ReturnsFalseAndDefaultValues()
        {
            var registry = new NetworkMessageRegistry();

            bool foundId = registry.TryGetId<TestNetworkPayload>(out ushort id);
            bool foundInfo = registry.TryGetInfo(123, out NetworkMessageInfo info);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundId, Is.False);
                Assert.That(id, Is.EqualTo(0));
                Assert.That(foundInfo, Is.False);
                Assert.That(info.Id, Is.EqualTo(0));
                Assert.That(info.Type, Is.Null);
            }
        }
    }
}
