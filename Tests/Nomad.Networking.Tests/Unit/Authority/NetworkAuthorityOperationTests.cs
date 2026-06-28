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

using Nomad.Networking.Authority;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Authority
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Authority")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkAuthorityOperationTests
    {
        [Test]
        public void Values_AreStableProtocolValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((byte)NetworkAuthorityOperation.Send, Is.EqualTo(0));
                Assert.That((byte)NetworkAuthorityOperation.Receive, Is.EqualTo(1));
                Assert.That((byte)NetworkAuthorityOperation.ExecuteRpc, Is.EqualTo(2));
                Assert.That((byte)NetworkAuthorityOperation.PublishEvent, Is.EqualTo(3));
            }
        }
    }
}
