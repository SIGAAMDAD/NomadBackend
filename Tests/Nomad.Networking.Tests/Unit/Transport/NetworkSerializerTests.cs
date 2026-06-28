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
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Transport
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Transport")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSerializerTests
    {
        [Test]
        public void SerializeAndDeserialize_WhenBufferIsLargeEnough_RoundTripsStruct()
        {
            var serializer = new NetworkSerializer();
            var payload = new TestNetworkPayload(123456, 42);
            byte[] buffer = new byte[serializer.GetMaxSize<TestNetworkPayload>()];

            bool serialized = serializer.Serialize(in payload, buffer, out int bytesWritten);
            bool deserialized = serializer.Deserialize(buffer, out TestNetworkPayload copy);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serialized, Is.True);
                Assert.That(deserialized, Is.True);
                Assert.That(bytesWritten, Is.EqualTo(serializer.GetMaxSize<TestNetworkPayload>()));
                Assert.That(copy.Value, Is.EqualTo(payload.Value));
                Assert.That(copy.Code, Is.EqualTo(payload.Code));
            }
        }

        [Test]
        public void Serialize_WhenDestinationIsTooSmall_ReturnsFalseAndWritesZeroBytes()
        {
            var serializer = new NetworkSerializer();
            var payload = new TestNetworkPayload(1, 2);

            bool serialized = serializer.Serialize(in payload, new byte[1], out int bytesWritten);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(serialized, Is.False);
                Assert.That(bytesWritten, Is.EqualTo(0));
            }
        }

        [Test]
        public void Deserialize_WhenSourceIsTooSmall_ReturnsFalseAndDefaultValue()
        {
            var serializer = new NetworkSerializer();

            bool deserialized = serializer.Deserialize(new byte[1], out TestNetworkPayload payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deserialized, Is.False);
                Assert.That(payload.Value, Is.EqualTo(0));
                Assert.That(payload.Code, Is.EqualTo(0));
            }
        }
    }
}
