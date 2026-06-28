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
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Rpc
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Rpc")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkRpcTargetTests
    {
        [Test]
        public void StaticFactories_CreateExpectedTargets()
        {
            PeerId peer = new PeerId(Guid.NewGuid());
            NetworkRpcTarget host = NetworkRpcTarget.Host();
            NetworkRpcTarget peerTarget = NetworkRpcTarget.Peer(peer);
            NetworkRpcTarget all = NetworkRpcTarget.All();
            NetworkRpcTarget others = NetworkRpcTarget.Others();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host.Kind, Is.EqualTo(NetworkTargetKind.Host));
                Assert.That(host.PeerId, Is.EqualTo(default(PeerId)));
                Assert.That(peerTarget.Kind, Is.EqualTo(NetworkTargetKind.Peer));
                Assert.That(peerTarget.PeerId, Is.EqualTo(peer));
                Assert.That(all.Kind, Is.EqualTo(NetworkTargetKind.All));
                Assert.That(others.Kind, Is.EqualTo(NetworkTargetKind.Others));
            }
        }
    }
}
