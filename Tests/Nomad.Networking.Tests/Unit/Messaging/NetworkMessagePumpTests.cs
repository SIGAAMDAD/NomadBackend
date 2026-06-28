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
using Nomad.Networking.Private.Events;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Private.Rpc;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Rpc;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Messaging
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Messaging")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkMessagePumpTests
    {
        [Test]
        public void Pump_RoutesRpcAndEventPacketsAndRecordsMalformedPackets()
        {
            var registry = new NetworkMessageRegistry();
            var serializer = new NetworkSerializer();
            var transport = new RecordingTransport();
            var authority = new RecordingAuthority();
            var diagnostics = new RecordingDiagnostics();
            var rpcBus = new NetworkRpcBus(registry, serializer, transport, authority, diagnostics);
            var eventBus = new NetworkEventBus(registry, serializer, transport, authority, diagnostics);
            var pump = new NetworkMessagePump(transport, registry, rpcBus, eventBus, diagnostics);
            registry.Register<TestNetworkPayload>(5, NetworkMessageKind.Rpc);
            registry.Register<AlternateNetworkPayload>(6, NetworkMessageKind.Event);
            registry.Register<UnroutableNetworkPayload>(7, NetworkMessageKind.Command);
            int rpcCalls = 0;
            var gameEvent = new TestGameEvent<AlternateNetworkPayload>();
            rpcBus.Register<TestNetworkPayload>((in NetworkRpcContext context, in TestNetworkPayload rpc) => rpcCalls++);
            eventBus.Register(gameEvent);

            transport.EnqueueReceive(transport.HostPeerId, new byte[] { 1 });
            transport.EnqueueReceive(transport.HostPeerId, CreateEnvelope(777, new byte[] { 1, 2, 3, 4 }));
            transport.EnqueueReceive(transport.HostPeerId, CreateEnvelope(7, new byte[] { 1, 2, 3, 4 }));
            transport.EnqueueReceive(transport.HostPeerId, CreateSerializedEnvelope(serializer, 5, new TestNetworkPayload(1, 2)));
            transport.EnqueueReceive(transport.HostPeerId, CreateSerializedEnvelope(serializer, 6, new AlternateNetworkPayload(9)));

            pump.Pump();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(diagnostics.Stats.PacketsReceived, Is.EqualTo(5));
                Assert.That(diagnostics.Stats.PacketsDropped, Is.EqualTo(2));
                Assert.That(diagnostics.Stats.UnknownMessageIds, Is.EqualTo(1));
                Assert.That(rpcCalls, Is.EqualTo(1));
                Assert.That(gameEvent.PublishCallCount, Is.EqualTo(1));
                Assert.That(gameEvent.PublishedPayload.Value, Is.EqualTo(9));
            }
        }

        [Test]
        public void Pump_WhenEventBusIsMissing_DropsEventPacketWithoutThrowing()
        {
            var registry = new NetworkMessageRegistry();
            var serializer = new NetworkSerializer();
            var transport = new RecordingTransport();
            var authority = new RecordingAuthority();
            var diagnostics = new RecordingDiagnostics();
            var rpcBus = new NetworkRpcBus(registry, serializer, transport, authority, diagnostics);
            var pump = new NetworkMessagePump(transport, registry, rpcBus, eventBus: null, diagnostics: diagnostics);
            registry.Register<AlternateNetworkPayload>(6, NetworkMessageKind.Event);
            transport.EnqueueReceive(transport.HostPeerId, CreateSerializedEnvelope(serializer, 6, new AlternateNetworkPayload(9)));

            pump.Pump();

            Assert.That(diagnostics.Stats.PacketsDropped, Is.EqualTo(1));
        }

        private static byte[] CreateSerializedEnvelope<TPayload>(NetworkSerializer serializer, ushort id, TPayload payload)
            where TPayload : struct
        {
            byte[] body = new byte[serializer.GetMaxSize<TPayload>()];
            serializer.Serialize(in payload, body, out int bytesWritten);
            byte[] envelope = new byte[NetworkMessageEnvelope.HEADER_SIZE + bytesWritten];
            NetworkMessageEnvelope.Write(id, envelope);
            body.AsSpan(0, bytesWritten).CopyTo(envelope.AsSpan(NetworkMessageEnvelope.HEADER_SIZE));
            return envelope;
        }

        private static byte[] CreateEnvelope(ushort id, byte[] body)
        {
            byte[] envelope = new byte[NetworkMessageEnvelope.HEADER_SIZE + body.Length];
            NetworkMessageEnvelope.Write(id, envelope);
            body.CopyTo(envelope, NetworkMessageEnvelope.HEADER_SIZE);
            return envelope;
        }

        private readonly struct UnroutableNetworkPayload
        {
            public readonly int Value;

            public UnroutableNetworkPayload(int value)
            {
                Value = value;
            }
        }
    }
}
