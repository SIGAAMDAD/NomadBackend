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
using Nomad.Networking.Extensions;
using Nomad.Networking.Messaging;
using Nomad.Networking.Private.Events;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Tests.Support;
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Events
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Events")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkEventBusTests
    {
        [Test]
        public void Constructor_WhenRequiredDependencyIsNull_ThrowsArgumentNullException()
        {
            var registry = new NetworkMessageRegistry();
            var serializer = new NetworkSerializer();
            var transport = new RecordingTransport();
            var authority = new RecordingAuthority();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => new NetworkEventBus(null, serializer));
                Assert.Throws<ArgumentNullException>(() => new NetworkEventBus(registry, null));
                Assert.Throws<ArgumentNullException>(() => new NetworkEventBus(null, serializer, transport, authority));
                Assert.Throws<ArgumentNullException>(() => new NetworkEventBus(registry, null, transport, authority));
                Assert.Throws<ArgumentNullException>(() => new NetworkEventBus(registry, serializer, null, authority));
                Assert.Throws<ArgumentNullException>(() => new NetworkEventBus(registry, serializer, transport, null));
            }
        }

        [Test]
        public void RegisterAndUnregister_WhenEventTypeIsUnknown_ThrowInvalidOperationException()
        {
            var bus = CreateBus(out _, out _, out _, out _);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<InvalidOperationException>(() => bus.Register(gameEvent));
                Assert.Throws<InvalidOperationException>(() => bus.Unregister<TestNetworkPayload>());
            }
        }

        [Test]
        public void PublishToHost_WhenMessageTypeIsUnknown_ThrowsWithoutSending()
        {
            NetworkEventBus bus = CreateBus(out _, out RecordingTransport transport, out _, out _);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var payload = new TestNetworkPayload(7, 8);

            Assert.Throws<InvalidOperationException>(() => bus.PublishToHost(gameEvent, in payload));
            Assert.That(transport.SentToHost, Is.Empty);
        }

        [Test]
        public void PublishToPeer_WhenAuthorityDenies_ReturnsFalseAndRecordsReject()
        {
            NetworkEventBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out RecordingAuthority authority, out RecordingDiagnostics diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Event);
            authority.Decision = false;
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var payload = new TestNetworkPayload(7, 8);

            bool sent = bus.PublishToPeer(new PeerId(Guid.NewGuid()), gameEvent, in payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sent, Is.False);
                Assert.That(transport.SentToPeers, Is.Empty);
                Assert.That(diagnostics.Stats.AuthorityRejects, Is.EqualTo(1));
                Assert.That(authority.Contexts[0].TargetKind, Is.EqualTo(NetworkTargetKind.Peer));
                Assert.That(authority.Contexts[0].Kind, Is.EqualTo(NetworkMessageKind.Event));
            }
        }

        [Test]
        public void PublishMethods_WhenRegisteredAndAllowed_SendExactEnvelopeSliceAndRecordStats()
        {
            NetworkEventBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out _, out RecordingDiagnostics diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Event);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var peer = new PeerId(Guid.NewGuid());
            var payload = new TestNetworkPayload(7, 8);

            bool hostSent = bus.PublishToHost(gameEvent, in payload, NetworkSendMode.Reliable);
            bool peerSent = bus.PublishToPeer(peer, gameEvent, in payload, NetworkSendMode.Unreliable);
            bool broadcastSent = bus.PublishToAll(gameEvent, in payload, NetworkSendMode.UnreliableNoDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hostSent, Is.True);
                Assert.That(peerSent, Is.True);
                Assert.That(broadcastSent, Is.True);
                Assert.That(transport.SentToHost, Has.Count.EqualTo(1));
                Assert.That(transport.SentToPeers, Has.Count.EqualTo(1));
                Assert.That(transport.Broadcasts, Has.Count.EqualTo(1));
                Assert.That(transport.SentToPeers[0].Peer, Is.EqualTo(peer));
                Assert.That(transport.SentToHost[0].Payload.Length, Is.EqualTo(2 + new NetworkSerializer().GetMaxSize<TestNetworkPayload>()));
                Assert.That(transport.SentToHost[0].Payload[0], Is.EqualTo(5));
                Assert.That(transport.SentToHost[0].Payload[1], Is.EqualTo(0));
                Assert.That(diagnostics.Stats.PacketsSent, Is.EqualTo(3));
                Assert.That(diagnostics.Stats.BytesSent, Is.EqualTo((uint)(transport.SentToHost[0].Payload.Length * 3)));
            }
        }

        [Test]
        public void Pump_WhenRegisteredEventIsQueued_PublishesGameEvent()
        {
            NetworkEventBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out _, out _);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Event);
            var serializer = new NetworkSerializer();
            var payload = new TestNetworkPayload(99, 3);
            byte[] body = new byte[serializer.GetMaxSize<TestNetworkPayload>()];
            serializer.Serialize(in payload, body, out _);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            bus.Register(gameEvent);

            bus.Enqueue(transport.HostPeerId, 5, body);
            bus.Pump();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gameEvent.PublishCallCount, Is.EqualTo(1));
                Assert.That(gameEvent.PublishedPayload.Value, Is.EqualTo(99));
                Assert.That(gameEvent.PublishedPayload.Code, Is.EqualTo(3));
            }
        }

        [Test]
        public void Pump_WhenMessageIdIsUnknown_RecordsUnknownMessageId()
        {
            NetworkEventBus bus = CreateBus(out _, out RecordingTransport transport, out _, out RecordingDiagnostics diagnostics);

            bus.Enqueue(transport.HostPeerId, 999, new byte[] { 1, 2, 3, 4 });
            bus.Pump();

            Assert.That(diagnostics.Stats.UnknownMessageIds, Is.EqualTo(1));
        }

        [Test]
        public void Pump_WhenDeserializeFails_DoesNotPublishAndRecordsFailure()
        {
            var registry = new NetworkMessageRegistry();
            var serializer = new FailingSerializer { DeserializeResult = false };
            var transport = new RecordingTransport();
            var authority = new RecordingAuthority();
            var diagnostics = new RecordingDiagnostics();
            var bus = new NetworkEventBus(registry, serializer, transport, authority, diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Event);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            bus.Register(gameEvent);

            bus.Enqueue(transport.HostPeerId, 5, new byte[] { 1, 2, 3, 4 });
            bus.Pump();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gameEvent.PublishCallCount, Is.EqualTo(0));
                Assert.That(diagnostics.Stats.DeserializeFailures, Is.EqualTo(1));
            }
        }

        [Test]
        public void GameEventExtensions_ForwardToInitializedEventBus()
        {
            NetworkEventBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out _, out _);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Event);
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var peer = new PeerId(Guid.NewGuid());
            var payload = new TestNetworkPayload(10, 1);

            gameEvent.NetworkRegister();
            bool host = gameEvent.NetworkPublishToHost(in payload);
            bool target = gameEvent.NetworkPublishToPeer(peer, in payload);
            bool all = gameEvent.NetworkPublishToAll(in payload);
            gameEvent.NetworkUnregister();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host, Is.True);
                Assert.That(target, Is.True);
                Assert.That(all, Is.True);
                Assert.That(transport.SentToHost, Has.Count.EqualTo(1));
                Assert.That(transport.SentToPeers, Has.Count.EqualTo(1));
                Assert.That(transport.Broadcasts, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void NetworkEventContext_Constructor_StoresAllFields()
        {
            var peer = new PeerId(Guid.NewGuid());

            var context = new NetworkEventContext(peer, 123, fromHost: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.Sender, Is.EqualTo(peer));
                Assert.That(context.Tick, Is.EqualTo(123));
                Assert.That(context.FromHost, Is.True);
            }
        }

        private static NetworkEventBus CreateBus(
            out NetworkMessageRegistry registry,
            out RecordingTransport transport,
            out RecordingAuthority authority,
            out RecordingDiagnostics diagnostics
        )
        {
            registry = new NetworkMessageRegistry();
            transport = new RecordingTransport();
            authority = new RecordingAuthority();
            diagnostics = new RecordingDiagnostics();
            return new NetworkEventBus(registry, new NetworkSerializer(), transport, authority, diagnostics);
        }
    }
}
