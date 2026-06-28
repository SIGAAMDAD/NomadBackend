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
using Nomad.Networking.Authority;
using Nomad.Networking.Messaging;
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Authority
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Authority")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkAuthorityContextTests
    {
        [Test]
        public void Constructor_StoresAllValues()
        {
            PeerId sender = new PeerId(Guid.NewGuid());
            PeerId target = new PeerId(Guid.NewGuid());
            PeerId local = new PeerId(Guid.NewGuid());
            PeerId host = new PeerId(Guid.NewGuid());

            var context = new NetworkAuthorityContext(
                NetworkAuthorityOperation.ExecuteRpc,
                sender,
                target,
                local,
                host,
                99,
                NetworkMessageKind.Rpc,
                NetworkTargetKind.Peer,
                true
            );

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.Operation, Is.EqualTo(NetworkAuthorityOperation.ExecuteRpc));
                Assert.That(context.Sender, Is.EqualTo(sender));
                Assert.That(context.Target, Is.EqualTo(target));
                Assert.That(context.LocalPeer, Is.EqualTo(local));
                Assert.That(context.HostPeer, Is.EqualTo(host));
                Assert.That(context.MessageId, Is.EqualTo(99));
                Assert.That(context.Kind, Is.EqualTo(NetworkMessageKind.Rpc));
                Assert.That(context.TargetKind, Is.EqualTo(NetworkTargetKind.Peer));
                Assert.That(context.LocalIsHost, Is.True);
            }
        }
    }
}
