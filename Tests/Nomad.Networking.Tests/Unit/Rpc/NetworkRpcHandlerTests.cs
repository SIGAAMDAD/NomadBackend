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
    public sealed class NetworkRpcHandlerTests
    {
        [Test]
        public void Delegate_CanReceiveContextAndRpcByReadonlyReference()
        {
            PeerId sender = new PeerId(Guid.NewGuid());
            NetworkRpcContext receivedContext = default;
            TestNetworkPayload receivedRpc = default;
            NetworkRpcHandler<TestNetworkPayload> handler = (in NetworkRpcContext context, in TestNetworkPayload rpc) =>
            {
                receivedContext = context;
                receivedRpc = rpc;
            };
            var context = new NetworkRpcContext(sender, false, true);
            var rpc = new TestNetworkPayload(33, 7);

            handler(in context, in rpc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(receivedContext.Sender, Is.EqualTo(sender));
                Assert.That(receivedContext.FromClient, Is.True);
                Assert.That(receivedRpc.Value, Is.EqualTo(33));
                Assert.That(receivedRpc.Code, Is.EqualTo(7));
            }
        }
    }
}
