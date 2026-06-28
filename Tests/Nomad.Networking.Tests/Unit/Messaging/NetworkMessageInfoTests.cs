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
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Messaging
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Messaging")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkMessageInfoTests
    {
        [Test]
        public void Constructor_StoresValues()
        {
            var info = new NetworkMessageInfo(77, typeof(TestNetworkPayload), NetworkMessageKind.Rpc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.Id, Is.EqualTo(77));
                Assert.That(info.Type, Is.EqualTo(typeof(TestNetworkPayload)));
                Assert.That(info.Kind, Is.EqualTo(NetworkMessageKind.Rpc));
            }
        }
    }
}
