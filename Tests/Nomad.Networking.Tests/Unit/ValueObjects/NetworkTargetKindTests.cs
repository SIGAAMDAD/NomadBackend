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

using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.ValueObjects
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("ValueObjects")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkTargetKindTests
    {
        [Test]
        public void Values_AreStableProtocolValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((byte)NetworkTargetKind.Host, Is.EqualTo(0));
                Assert.That((byte)NetworkTargetKind.Peer, Is.EqualTo(1));
                Assert.That((byte)NetworkTargetKind.All, Is.EqualTo(2));
                Assert.That((byte)NetworkTargetKind.Others, Is.EqualTo(3));
            }
        }
    }
}
