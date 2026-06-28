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
using Nomad.Networking.Private.Diagnostics;
using Nomad.Networking.Private.Events;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Events
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Events")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class EventInvokerTests
    {
        [Test]
        public void Constructor_WhenGameEventIsNull_ThrowsArgumentNullException()
        {
            var serializer = new NetworkSerializer();

            Assert.Throws<ArgumentNullException>(() => new EventInvoker<TestNetworkPayload>(null, serializer, null));
        }

        [Test]
        public void Constructor_WhenSerializerIsNull_ThrowsArgumentNullException()
        {
            var gameEvent = new TestGameEvent<TestNetworkPayload>();

            Assert.Throws<ArgumentNullException>(() => new EventInvoker<TestNetworkPayload>(gameEvent, null, null));
        }

        [Test]
        public void MessageType_ReturnsPayloadType()
        {
            var invoker = new EventInvoker<TestNetworkPayload>(new TestGameEvent<TestNetworkPayload>(), new NetworkSerializer(), null);

            Assert.That(invoker.MessageType, Is.EqualTo(typeof(TestNetworkPayload)));
        }

        [Test]
        public void Dispatch_WhenPayloadDeserializes_PublishesGameEvent()
        {
            var serializer = new NetworkSerializer();
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var payload = new TestNetworkPayload(77, 3);
            byte[] buffer = new byte[serializer.GetMaxSize<TestNetworkPayload>()];
            serializer.Serialize(in payload, buffer, out int bytesWritten);
            var inbound = new InboundEvent(default, 1, buffer, bytesWritten);
            var invoker = new EventInvoker<TestNetworkPayload>(gameEvent, serializer, null);

            invoker.Dispatch(in inbound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gameEvent.PublishCallCount, Is.EqualTo(1));
                Assert.That(gameEvent.PublishedPayload.Value, Is.EqualTo(77));
                Assert.That(gameEvent.PublishedPayload.Code, Is.EqualTo(3));
            }
        }

        [Test]
        public void Dispatch_WhenPayloadDoesNotDeserialize_RecordsFailureAndDoesNotPublish()
        {
            var diagnostics = new NetworkDiagnostics();
            var gameEvent = new TestGameEvent<TestNetworkPayload>();
            var serializer = new FailingSerializer { DeserializeResult = false };
            var inbound = new InboundEvent(default, 1, new byte[] { 1, 2, 3, 4 }, 4);
            var invoker = new EventInvoker<TestNetworkPayload>(gameEvent, serializer, diagnostics);

            invoker.Dispatch(in inbound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gameEvent.PublishCallCount, Is.EqualTo(0));
                Assert.That(diagnostics.Stats.DeserializeFailures, Is.EqualTo(1));
            }
        }
    }
}
