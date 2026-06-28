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
using Nomad.Networking.Private.Authority;
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Authority
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Authority")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkAuthorityTests
    {
        [Test]
        public void AddRule_WhenRuleIsNull_ThrowsArgumentNullException()
        {
            var authority = new NetworkAuthority();

            Assert.Throws<ArgumentNullException>(() => authority.AddRule(null));
        }

        [Test]
        public void Evaluate_WhenNoRuleAllows_UsesDefaultDecision()
        {
            var authority = new NetworkAuthority();
            NetworkAuthorityContext context = CreateContext(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Send, senderIsHost: false);

            bool deniedByDefault = authority.Evaluate(in context);
            authority.DefaultDecision = NetworkAuthorityDecision.Allow;
            bool allowedByDefault = authority.Evaluate(in context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deniedByDefault, Is.False);
                Assert.That(allowedByDefault, Is.True);
            }
        }

        [Test]
        public void Evaluate_WhenAnyRuleDenies_DenyOverridesAllow()
        {
            var authority = new NetworkAuthority { DefaultDecision = NetworkAuthorityDecision.Allow };
            authority.AddRule(new StaticRule(NetworkAuthorityDecision.Allow));
            authority.AddRule(new StaticRule(NetworkAuthorityDecision.Deny));
            authority.AddRule(new ThrowingRule());
            NetworkAuthorityContext context = CreateContext(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Send, senderIsHost: false);

            bool allowed = authority.Evaluate(in context);

            Assert.That(allowed, Is.False);
        }

        [Test]
        public void Evaluate_WhenAtLeastOneRuleAllowsAndNoneDeny_ReturnsTrue()
        {
            var authority = new NetworkAuthority();
            authority.AddRule(new StaticRule(NetworkAuthorityDecision.Abstain));
            authority.AddRule(new StaticRule(NetworkAuthorityDecision.Allow));
            NetworkAuthorityContext context = CreateContext(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Send, senderIsHost: false);

            bool allowed = authority.Evaluate(in context);

            Assert.That(allowed, Is.True);
        }

        [TestCase(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Rpc, NetworkAuthorityOperation.ExecuteRpc, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Rpc, NetworkAuthorityOperation.Receive, false, NetworkAuthorityDecision.Abstain)]
        [TestCase(NetworkMessageKind.Event, NetworkAuthorityOperation.PublishEvent, true, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Event, NetworkAuthorityOperation.PublishEvent, false, NetworkAuthorityDecision.Deny)]
        [TestCase(NetworkMessageKind.Command, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Input, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Snapshot, NetworkAuthorityOperation.Send, true, NetworkAuthorityDecision.Allow)]
        [TestCase(NetworkMessageKind.Snapshot, NetworkAuthorityOperation.Send, false, NetworkAuthorityDecision.Deny)]
        public void HostAuthoritativeRule_Evaluate_ReturnsExpectedDecision(
            NetworkMessageKind kind,
            NetworkAuthorityOperation operation,
            bool senderIsHost,
            NetworkAuthorityDecision expected
        )
        {
            var rule = new HostAuthoritativeRule();
            NetworkAuthorityContext context = CreateContext(kind, operation, senderIsHost);

            NetworkAuthorityDecision decision = rule.Evaluate(in context);

            Assert.That(decision, Is.EqualTo(expected));
        }

        [Test]
        public void NetworkAuthorityContext_Constructor_StoresAllFields()
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
                42,
                NetworkMessageKind.Rpc,
                NetworkTargetKind.Peer,
                localIsHost: true
            );

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.Operation, Is.EqualTo(NetworkAuthorityOperation.ExecuteRpc));
                Assert.That(context.Sender, Is.EqualTo(sender));
                Assert.That(context.Target, Is.EqualTo(target));
                Assert.That(context.LocalPeer, Is.EqualTo(local));
                Assert.That(context.HostPeer, Is.EqualTo(host));
                Assert.That(context.MessageId, Is.EqualTo(42));
                Assert.That(context.Kind, Is.EqualTo(NetworkMessageKind.Rpc));
                Assert.That(context.TargetKind, Is.EqualTo(NetworkTargetKind.Peer));
                Assert.That(context.LocalIsHost, Is.True);
            }
        }

        private static NetworkAuthorityContext CreateContext(
            NetworkMessageKind kind,
            NetworkAuthorityOperation operation,
            bool senderIsHost
        )
        {
            PeerId host = new PeerId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
            PeerId client = new PeerId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
            PeerId sender = senderIsHost ? host : client;

            return new NetworkAuthorityContext(
                operation,
                sender,
                default,
                client,
                host,
                7,
                kind,
                NetworkTargetKind.Host,
                localIsHost: senderIsHost
            );
        }

        private sealed class StaticRule : INetworkAuthorityRule
        {
            private readonly NetworkAuthorityDecision _decision;

            public StaticRule(NetworkAuthorityDecision decision)
            {
                _decision = decision;
            }

            public NetworkAuthorityDecision Evaluate(in NetworkAuthorityContext context)
            {
                return _decision;
            }
        }

        private sealed class ThrowingRule : INetworkAuthorityRule
        {
            public NetworkAuthorityDecision Evaluate(in NetworkAuthorityContext context)
            {
                throw new InvalidOperationException("Deny should short-circuit following rules.");
            }
        }
    }
}
