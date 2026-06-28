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
using Nomad.Networking.Private;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Private
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Private")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class PooledSendBufferTests
    {
        [Test]
        public void Constructor_StoresBufferAndLengthAndSpanUsesLength()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(16);
            buffer[0] = 1;
            buffer[1] = 2;
            var pooled = new PooledSendBuffer(buffer, 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pooled.Buffer, Is.SameAs(buffer));
                Assert.That(pooled.Length, Is.EqualTo(2));
                Assert.That(pooled.Span.Length, Is.EqualTo(2));
                Assert.That(pooled.Span[0], Is.EqualTo(1));
                Assert.That(pooled.Span[1], Is.EqualTo(2));
            }

            pooled.Dispose();
        }

        [Test]
        public void Dispose_ReturnsBufferAndClearsState()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4);
            var pooled = new PooledSendBuffer(buffer, 4);

            pooled.Dispose();
            pooled.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(pooled.Buffer, Is.SameAs(Array.Empty<byte>()));
                Assert.That(pooled.Length, Is.EqualTo(0));
            }
        }
    }
}
