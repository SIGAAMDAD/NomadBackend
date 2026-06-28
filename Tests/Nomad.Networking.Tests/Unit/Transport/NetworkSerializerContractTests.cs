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

using Nomad.Networking.Private.Transport;
using Nomad.Networking.Transport;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Transport
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Transport")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSerializerContractTests
    {
        [Test]
        public void NetworkSerializer_ImplementsSerializerContract()
        {
            INetworkSerializer serializer = new NetworkSerializer();
            var payload = new TestNetworkPayload(11, 12);
            byte[] buffer = new byte[serializer.GetMaxSize<TestNetworkPayload>()];

            bool serialized = serializer.Serialize(in payload, buffer, out int written);
            bool deserialized = serializer.Deserialize(buffer, out TestNetworkPayload copy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serialized, Is.True);
                Assert.That(deserialized, Is.True);
                Assert.That(written, Is.EqualTo(buffer.Length));
                Assert.That(copy.Value, Is.EqualTo(11));
                Assert.That(copy.Code, Is.EqualTo(12));
            }
        }
    }
}
