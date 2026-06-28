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
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Events
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Events")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkEventHandlerTests
    {
        [Test]
        public void Delegate_CanReceiveContextAndPayloadByReadonlyReference()
        {
            PeerId sender = new PeerId(Guid.NewGuid());
            NetworkEventContext receivedContext = default;
            TestNetworkPayload receivedPayload = default;
            NetworkEventHandler<TestNetworkPayload> handler = (in NetworkEventContext context, in TestNetworkPayload payload) =>
            {
                receivedContext = context;
                receivedPayload = payload;
            };
            var originalContext = new NetworkEventContext(sender, 5, true);
            var originalPayload = new TestNetworkPayload(9, 2);

            handler(in originalContext, in originalPayload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(receivedContext.Sender, Is.EqualTo(sender));
                Assert.That(receivedContext.Tick, Is.EqualTo(5));
                Assert.That(receivedPayload.Value, Is.EqualTo(9));
                Assert.That(receivedPayload.Code, Is.EqualTo(2));
            }
        }
    }
}
