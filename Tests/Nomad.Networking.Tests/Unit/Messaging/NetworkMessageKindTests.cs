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

using Nomad.Networking.Messaging;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Messaging
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Messaging")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkMessageKindTests
    {
        [Test]
        public void Values_AreStableProtocolValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((byte)NetworkMessageKind.Unknown, Is.EqualTo(0));
                Assert.That((byte)NetworkMessageKind.Event, Is.EqualTo(1));
                Assert.That((byte)NetworkMessageKind.Rpc, Is.EqualTo(2));
                Assert.That((byte)NetworkMessageKind.Command, Is.EqualTo(3));
                Assert.That((byte)NetworkMessageKind.Input, Is.EqualTo(4));
                Assert.That((byte)NetworkMessageKind.Snapshot, Is.EqualTo(5));
                Assert.That((byte)NetworkMessageKind.Internal, Is.EqualTo(6));
            }
        }
    }
}
