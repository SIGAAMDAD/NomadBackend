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
using Nomad.Networking.Private.Messaging;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Messaging
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Messaging")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkMessageEnvelopeTests
    {
        [Test]
        public void WriteAndTryRead_UsesLittleEndianHeaderAndPayloadSlice()
        {
            byte[] buffer = { 0, 0, 10, 20, 30 };

            NetworkMessageEnvelope.Write(0x1234, buffer);
            bool read = NetworkMessageEnvelope.TryRead(buffer, out ushort id, out ReadOnlySpan<byte> payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.True);
                Assert.That(buffer[0], Is.EqualTo(0x34));
                Assert.That(buffer[1], Is.EqualTo(0x12));
                Assert.That(id, Is.EqualTo(0x1234));
                Assert.That(payload.ToArray(), Is.EqualTo(new byte[] { 10, 20, 30 }));
            }
        }

        [Test]
        public void TryRead_WhenSourceIsTooSmall_ReturnsFalse()
        {
            bool read = NetworkMessageEnvelope.TryRead(new byte[] { 1 }, out ushort id, out ReadOnlySpan<byte> payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read, Is.False);
                Assert.That(id, Is.EqualTo(0));
                Assert.That(payload.Length, Is.EqualTo(0));
            }
        }
    }
}
