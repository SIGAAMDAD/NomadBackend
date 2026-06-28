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
using Nomad.Networking.Messaging;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Private.Rpc;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Rpc;
using Nomad.Networking.Tests.Support;
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Rpc
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Rpc")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkRpcBusTests
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
                Assert.Throws<ArgumentNullException>(() => new NetworkRpcBus(null, serializer));
                Assert.Throws<ArgumentNullException>(() => new NetworkRpcBus(registry, null));
                Assert.Throws<ArgumentNullException>(() => new NetworkRpcBus(null, serializer, transport, authority));
                Assert.Throws<ArgumentNullException>(() => new NetworkRpcBus(registry, null, transport, authority));
                Assert.Throws<ArgumentNullException>(() => new NetworkRpcBus(registry, serializer, null, authority));
                Assert.Throws<ArgumentNullException>(() => new NetworkRpcBus(registry, serializer, transport, null));
            }
        }

        [Test]
        public void Register_WhenRpcTypeIsNotRegistered_ThrowsInvalidOperationException()
        {
            var bus = CreateBus(out _, out _, out _, out _);

            Assert.Throws<InvalidOperationException>(() => bus.Register<TestNetworkPayload>(delegate { }));
        }

        [Test]
        public void SendToHost_WhenMessageTypeIsUnknown_ReturnsFalseWithoutSending()
        {
            NetworkRpcBus bus = CreateBus(out _, out RecordingTransport transport, out _, out _);
            var payload = new TestNetworkPayload(7, 8);

            bool sent = bus.SendToHost(in payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sent, Is.False);
                Assert.That(transport.SentToHost, Is.Empty);
            }
        }

        [Test]
        public void SendToPeer_WhenAuthorityDenies_ReturnsFalseAndRecordsReject()
        {
            NetworkRpcBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out RecordingAuthority authority, out RecordingDiagnostics diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            authority.Decision = false;
            var payload = new TestNetworkPayload(7, 8);

            bool sent = bus.SendToPeer(new PeerId(Guid.NewGuid()), in payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sent, Is.False);
                Assert.That(transport.SentToPeers, Is.Empty);
                Assert.That(diagnostics.Stats.AuthorityRejects, Is.EqualTo(1));
                Assert.That(authority.Contexts[0].TargetKind, Is.EqualTo(NetworkTargetKind.Peer));
                Assert.That(authority.Contexts[0].Kind, Is.EqualTo(NetworkMessageKind.Rpc));
            }
        }

        [Test]
        public void SendMethods_WhenRegisteredAndAllowed_SendExactEnvelopeSliceAndRecordStats()
        {
            NetworkRpcBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out _, out RecordingDiagnostics diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            var peer = new PeerId(Guid.NewGuid());
            var payload = new TestNetworkPayload(7, 8);

            bool hostSent = bus.SendToHost(in payload, NetworkSendMode.Reliable);
            bool peerSent = bus.SendToPeer(peer, in payload, NetworkSendMode.Unreliable);
            bool broadcastSent = bus.Broadcast(in payload, NetworkSendMode.UnreliableNoDelay);

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
        public void Pump_WhenRegisteredRpcIsQueued_DispatchesHandlerWithContextAndPayload()
        {
            NetworkRpcBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out _, out _);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            var serializer = new NetworkSerializer();
            var payload = new TestNetworkPayload(99, 3);
            byte[] body = new byte[serializer.GetMaxSize<TestNetworkPayload>()];
            serializer.Serialize(in payload, body, out _);
            int callCount = 0;
            NetworkRpcContext observedContext = default;
            TestNetworkPayload observedPayload = default;

            bus.Register<TestNetworkPayload>((in NetworkRpcContext context, in TestNetworkPayload rpc) =>
            {
                callCount++;
                observedContext = context;
                observedPayload = rpc;
            });
            bus.Enqueue(transport.HostPeerId, 5, body);

            bus.Pump();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callCount, Is.EqualTo(1));
                Assert.That(observedContext.Sender, Is.EqualTo(transport.HostPeerId));
                Assert.That(observedContext.FromHost, Is.True);
                Assert.That(observedContext.FromClient, Is.False);
                Assert.That(observedPayload.Value, Is.EqualTo(99));
                Assert.That(observedPayload.Code, Is.EqualTo(3));
            }
        }

        [Test]
        public void Pump_WhenMessageIdIsUnknown_RecordsUnknownMessageId()
        {
            NetworkRpcBus bus = CreateBus(out _, out RecordingTransport transport, out _, out RecordingDiagnostics diagnostics);

            bus.Enqueue(transport.HostPeerId, 999, new byte[] { 1, 2, 3, 4 });
            bus.Pump();

            Assert.That(diagnostics.Stats.UnknownMessageIds, Is.EqualTo(1));
        }

        [Test]
        public void Pump_WhenAuthorityDeniesExecution_DoesNotDispatchAndRecordsReject()
        {
            NetworkRpcBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out RecordingAuthority authority, out RecordingDiagnostics diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            var serializer = new NetworkSerializer();
            var payload = new TestNetworkPayload(99, 3);
            byte[] body = new byte[serializer.GetMaxSize<TestNetworkPayload>()];
            serializer.Serialize(in payload, body, out _);
            int callCount = 0;
            bus.Register<TestNetworkPayload>((in NetworkRpcContext context, in TestNetworkPayload rpc) => callCount++);
            authority.Decision = false;

            bus.Enqueue(transport.HostPeerId, 5, body);
            bus.Pump();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callCount, Is.EqualTo(0));
                Assert.That(diagnostics.Stats.AuthorityRejects, Is.EqualTo(1));
            }
        }

        [Test]
        public void Pump_WhenDeserializeFails_DoesNotDispatchAndRecordsFailure()
        {
            var registry = new NetworkMessageRegistry();
            var serializer = new FailingSerializer { DeserializeResult = false };
            var transport = new RecordingTransport();
            var authority = new RecordingAuthority();
            var diagnostics = new RecordingDiagnostics();
            var bus = new NetworkRpcBus(registry, serializer, transport, authority, diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            int callCount = 0;
            bus.Register<TestNetworkPayload>((in NetworkRpcContext context, in TestNetworkPayload rpc) => callCount++);

            bus.Enqueue(transport.HostPeerId, 5, new byte[] { 1, 2, 3, 4 });
            bus.Pump();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callCount, Is.EqualTo(0));
                Assert.That(diagnostics.Stats.DeserializeFailures, Is.EqualTo(1));
            }
        }

        [Test]
        public void Unregister_RemovesRegisteredHandlerAndIsSafeForUnknownTypes()
        {
            NetworkRpcBus bus = CreateBus(out NetworkMessageRegistry registry, out RecordingTransport transport, out _, out RecordingDiagnostics diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            bus.Register<TestNetworkPayload>((in NetworkRpcContext context, in TestNetworkPayload rpc) => { });

            bus.Unregister<TestNetworkPayload>();
            bus.Unregister<AlternateNetworkPayload>();
            bus.Enqueue(transport.HostPeerId, 5, new byte[] { 1, 2, 3, 4 });
            bus.Pump();

            Assert.That(diagnostics.Stats.UnknownMessageIds, Is.EqualTo(1));
        }

        private static NetworkRpcBus CreateBus(
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
            return new NetworkRpcBus(registry, new NetworkSerializer(), transport, authority, diagnostics);
        }
    }
}
