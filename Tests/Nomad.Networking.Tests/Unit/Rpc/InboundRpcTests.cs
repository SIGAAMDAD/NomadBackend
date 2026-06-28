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
using System.Buffers;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Private.Rpc;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Rpc
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Rpc")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class InboundRpcTests
    {
        [Test]
        public void Constructor_StoresFields()
        {
            PeerId sender = new PeerId(Guid.NewGuid());
            byte[] payload = ArrayPool<byte>.Shared.Rent(4);
            var inbound = new InboundRpc(sender, 11, payload, 4);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inbound.Sender, Is.EqualTo(sender));
                Assert.That(inbound.MessageId, Is.EqualTo(11));
                Assert.That(inbound.Payload, Is.SameAs(payload));
                Assert.That(inbound.PayloadLength, Is.EqualTo(4));
            }

            inbound.Dispose();
        }

        [Test]
        public void Dispose_ReturnsPayloadAndClearsState()
        {
            byte[] payload = ArrayPool<byte>.Shared.Rent(8);
            var inbound = new InboundRpc(default, 1, payload, 8);

            inbound.Dispose();
            inbound.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(inbound.Payload, Is.SameAs(Array.Empty<byte>()));
                Assert.That(inbound.PayloadLength, Is.EqualTo(0));
            }
        }
    }
}
