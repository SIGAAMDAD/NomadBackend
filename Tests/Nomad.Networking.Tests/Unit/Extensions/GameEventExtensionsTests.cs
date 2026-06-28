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
using Nomad.Core.Events;
using Nomad.Core.Exceptions;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Events;
using Nomad.Networking.Extensions;
using Nomad.Networking.Messaging;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Extensions
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Extensions")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class GameEventExtensionsTests
    {
        [Test]
        public void NetworkMethods_ForwardToInitializedEventBus()
        {
            var bus = new RecordingNetworkEventBus();
            GameEventExtensions.Initialize(bus);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var payload = new TestNetworkPayload(10, 4);
            PeerId peer = new PeerId(Guid.NewGuid());

            gameEvent.NetworkRegister();
            bool host = gameEvent.NetworkPublishToHost(in payload, NetworkSendMode.Unreliable);
            bool target = gameEvent.NetworkPublishToPeer(peer, in payload, NetworkSendMode.Reliable);
            bool all = gameEvent.NetworkPublishToAll(in payload, NetworkSendMode.UnreliableNoDelay);
            gameEvent.NetworkUnregister();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bus.RegisterCount, Is.EqualTo(1));
                Assert.That(bus.UnregisterCount, Is.EqualTo(1));
                Assert.That(host, Is.True);
                Assert.That(target, Is.True);
                Assert.That(all, Is.True);
                Assert.That(bus.PublishToHostCount, Is.EqualTo(1));
                Assert.That(bus.PublishToPeerCount, Is.EqualTo(1));
                Assert.That(bus.PublishToAllCount, Is.EqualTo(1));
                Assert.That(bus.LastPeer, Is.EqualTo(peer));
            }
        }

        [Test]
        public void Initialize_WhenEventBusIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => GameEventExtensions.Initialize(null));
        }

        private sealed class RecordingNetworkEventBus : INetworkEventBus
        {
            public int RegisterCount;
            public int UnregisterCount;
            public int PublishToHostCount;
            public int PublishToPeerCount;
            public int PublishToAllCount;
            public PeerId LastPeer;

            public void Register<TArgs>(IGameEvent<TArgs> gameEvent) where TArgs : struct => RegisterCount++;
            public void Unregister<TArgs>() where TArgs : struct => UnregisterCount++;

            public bool PublishToHost<TArgs>(IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
                where TArgs : struct
            {
                PublishToHostCount++;
                return true;
            }

            public bool PublishToPeer<TArgs>(PeerId peerId, IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
                where TArgs : struct
            {
                LastPeer = peerId;
                PublishToPeerCount++;
                return true;
            }

            public bool PublishToAll<TArgs>(IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
                where TArgs : struct
            {
                PublishToAllCount++;
                return true;
            }
        }
    }
}
