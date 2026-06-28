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
    public sealed class HostAuthoritativeRuleTests
    {
        [TestCase(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Rpc, NetworkAuthorityOperation.ExecuteRpc, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Receive, false, NetworkAuthorityDecision.Abstain)]
        [TestCase(NetworkMessageKind.Event, NetworkAuthorityOperation.PublishEvent, true, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Event, NetworkAuthorityOperation.PublishEvent, false, NetworkAuthorityDecision.Deny)]
        [TestCase(NetworkMessageKind.Command, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Input, NetworkAuthorityOperation.Send, true, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Snapshot, NetworkAuthorityOperation.Send, true, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Snapshot, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Deny)]
        [TestCase(NetworkMessageKind.Internal, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Abstain)]
        public void Evaluate_ReturnsExpectedDecision(NetworkMessageKind kind, NetworkAuthorityOperation operation, bool senderIsHost, NetworkAuthorityDecision expected)
        {
            var rule = new HostAuthoritativeRule();
            NetworkAuthorityContext context = CreateContext(kind, operation, senderIsHost);

            NetworkAuthorityDecision actual = rule.Evaluate(in context);

            Assert.That(actual, Is.EqualTo(expected));
        }

        private static NetworkAuthorityContext CreateContext(NetworkMessageKind kind, NetworkAuthorityOperation operation, bool senderIsHost)
        {
            PeerId host = new PeerId(Guid.Parse("10000000-0000-0000-0000-000000000001"));
            PeerId client = new PeerId(Guid.Parse("10000000-0000-0000-0000-000000000002"));
            PeerId sender = senderIsHost ? host : client;

            return new NetworkAuthorityContext(
                operation,
                sender,
                target: host,
                localPeer: client,
                hostPeer: host,
                messageId: 12,
                kind,
                NetworkTargetKind.Host,
                localIsHost: senderIsHost
            );
        }
    }
}
