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
using Nomad.Networking.Private.Diagnostics;
using Nomad.Networking.Private.Rpc;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Rpc;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Rpc
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Rpc")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class RpcInvokerTests
    {
        [Test]
        public void Constructor_WhenHandlerIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RpcInvoker<TestNetworkPayload>(null, new NetworkSerializer(), new RecordingTransport(), null));
        }

        [Test]
        public void Constructor_WhenSerializerIsNull_ThrowsArgumentNullException()
        {
            NetworkRpcHandler<TestNetworkPayload> handler = (in NetworkRpcContext context, in TestNetworkPayload rpc) => { };

            Assert.Throws<ArgumentNullException>(() => new RpcInvoker<TestNetworkPayload>(handler, null, new RecordingTransport(), null));
        }

        [Test]
        public void Constructor_WhenTransportIsNull_ThrowsArgumentNullException()
        {
            NetworkRpcHandler<TestNetworkPayload> handler = (in NetworkRpcContext context, in TestNetworkPayload rpc) => { };

            Assert.Throws<ArgumentNullException>(() => new RpcInvoker<TestNetworkPayload>(handler, new NetworkSerializer(), null, null));
        }

        [Test]
        public void MessageType_ReturnsRpcType()
        {
            NetworkRpcHandler<TestNetworkPayload> handler = (in NetworkRpcContext context, in TestNetworkPayload rpc) => { };
            var invoker = new RpcInvoker<TestNetworkPayload>(handler, new NetworkSerializer(), new RecordingTransport(), null);

            Assert.That(invoker.MessageType, Is.EqualTo(typeof(TestNetworkPayload)));
        }

        [Test]
        public void Dispatch_WhenPayloadDeserializes_InvokesHandlerWithContext()
        {
            var serializer = new NetworkSerializer();
            var transport = new RecordingTransport();
            PeerId sender = transport.HostPeerId;
            var payload = new TestNetworkPayload(20, 6);
            byte[] buffer = new byte[serializer.GetMaxSize<TestNetworkPayload>()];
            serializer.Serialize(in payload, buffer, out int bytesWritten);
            NetworkRpcContext receivedContext = default;
            TestNetworkPayload receivedRpc = default;
            NetworkRpcHandler<TestNetworkPayload> handler = (in NetworkRpcContext context, in TestNetworkPayload rpc) =>
            {
                receivedContext = context;
                receivedRpc = rpc;
            };
            var invoker = new RpcInvoker<TestNetworkPayload>(handler, serializer, transport, null);
            var inbound = new InboundRpc(sender, 8, buffer, bytesWritten);

            invoker.Dispatch(in inbound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(receivedContext.Sender, Is.EqualTo(sender));
                Assert.That(receivedContext.FromHost, Is.True);
                Assert.That(receivedContext.FromClient, Is.False);
                Assert.That(receivedRpc.Value, Is.EqualTo(20));
                Assert.That(receivedRpc.Code, Is.EqualTo(6));
            }
        }

        [Test]
        public void Dispatch_WhenPayloadDoesNotDeserialize_RecordsFailureAndSkipsHandler()
        {
            var diagnostics = new NetworkDiagnostics();
            bool called = false;
            NetworkRpcHandler<TestNetworkPayload> handler = (in NetworkRpcContext context, in TestNetworkPayload rpc) => called = true;
            var invoker = new RpcInvoker<TestNetworkPayload>(handler, new FailingSerializer { DeserializeResult = false }, new RecordingTransport(), diagnostics);
            var inbound = new InboundRpc(default, 1, new byte[] { 1, 2, 3, 4 }, 4);

            invoker.Dispatch(in inbound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(called, Is.False);
                Assert.That(diagnostics.Stats.DeserializeFailures, Is.EqualTo(1));
            }
        }
    }
}
