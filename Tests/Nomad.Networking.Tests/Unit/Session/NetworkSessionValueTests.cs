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
using Nomad.Networking.Session;
using Nomad.Networking.ValueObjects;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Session
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Session")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSessionValueTests
    {
        [Test]
        public void NetworkPeerInfo_Constructor_StoresAllFields()
        {
            var peer = new PeerId(Guid.NewGuid());

            var info = new NetworkPeerInfo(peer, "Host", isHost: true, isLocal: false, isReady: true, playerSlot: 2, NetworkConnectionState.Connected);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.PeerId, Is.EqualTo(peer));
                Assert.That(info.DisplayName, Is.EqualTo("Host"));
                Assert.That(info.IsHost, Is.True);
                Assert.That(info.IsLocal, Is.False);
                Assert.That(info.IsReady, Is.True);
                Assert.That(info.PlayerSlot, Is.EqualTo(2));
                Assert.That(info.State, Is.EqualTo(NetworkConnectionState.Connected));
            }
        }

        [Test]
        public void NetworkSessionResults_CreateSuccessAndFailureShapes()
        {
            var session = new NetworkSessionInfo { SessionId = Guid.NewGuid(), Mode = NetworkSessionMode.Host };

            NetworkSessionStartResult started = NetworkSessionStartResult.Started(session);
            NetworkSessionStartResult startFailed = NetworkSessionStartResult.Failed(NetworkSessionFailureReason.PlatformUnavailable);
            NetworkSessionJoinResult joined = NetworkSessionJoinResult.Joined(session);
            NetworkSessionJoinResult joinFailed = NetworkSessionJoinResult.Failed(NetworkSessionFailureReason.SessionNotFound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(started.Success, Is.True);
                Assert.That(started.Session, Is.SameAs(session));
                Assert.That(startFailed.Success, Is.False);
                Assert.That(startFailed.Session, Is.Null);
                Assert.That(startFailed.Reason, Is.EqualTo(NetworkSessionFailureReason.PlatformUnavailable));
                Assert.That(joined.Success, Is.True);
                Assert.That(joined.Session, Is.SameAs(session));
                Assert.That(joinFailed.Success, Is.False);
                Assert.That(joinFailed.Session, Is.Null);
                Assert.That(joinFailed.Reason, Is.EqualTo(NetworkSessionFailureReason.SessionNotFound));
            }
        }

        [Test]
        public void NetworkRpcTarget_FactoryMethods_CreateExpectedTargets()
        {
            var peer = new PeerId(Guid.NewGuid());

            NetworkRpcTarget host = NetworkRpcTarget.Host();
            NetworkRpcTarget targetPeer = NetworkRpcTarget.Peer(peer);
            NetworkRpcTarget all = NetworkRpcTarget.All();
            NetworkRpcTarget others = NetworkRpcTarget.Others();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host.Kind, Is.EqualTo(NetworkTargetKind.Host));
                Assert.That(host.PeerId, Is.EqualTo(default(PeerId)));
                Assert.That(targetPeer.Kind, Is.EqualTo(NetworkTargetKind.Peer));
                Assert.That(targetPeer.PeerId, Is.EqualTo(peer));
                Assert.That(all.Kind, Is.EqualTo(NetworkTargetKind.All));
                Assert.That(others.Kind, Is.EqualTo(NetworkTargetKind.Others));
            }
        }

        [Test]
        public void NetworkRpcContext_Constructor_StoresAllFields()
        {
            var sender = new PeerId(Guid.NewGuid());

            var context = new NetworkRpcContext(sender, fromHost: false, fromClient: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.Sender, Is.EqualTo(sender));
                Assert.That(context.FromHost, Is.False);
                Assert.That(context.FromClient, Is.True);
            }
        }

        [Test]
        public void RpcAttributes_ExposeConstructorAndInitValues()
        {
            var method = new RpcMethodAttribute("Fire", "Combat");
            var payload = new RpcMethodPayloadAttribute("Damage", typeof(int)) { TypeName = "System.Int32", Order = 3 };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(method.Name, Is.EqualTo("Fire"));
                Assert.That(method.NameSpace, Is.EqualTo("Combat"));
                Assert.That(payload.Name, Is.EqualTo("Damage"));
                Assert.That(payload.Type, Is.EqualTo(typeof(int)));
                Assert.That(payload.TypeName, Is.EqualTo("System.Int32"));
                Assert.That(payload.Order, Is.EqualTo(3));
            }
        }

        [Test]
        public void NetworkSendPolicy_StaticPolicies_MapModesAndChannels()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NetworkSendPolicy.ReliableControl.Mode, Is.EqualTo(NetworkSendMode.Reliable));
                Assert.That(NetworkSendPolicy.ReliableControl.Channel, Is.EqualTo(NetworkChannel.Control));
                Assert.That(NetworkSendPolicy.UnreliableInput.Mode, Is.EqualTo(NetworkSendMode.UnreliableNoDelay));
                Assert.That(NetworkSendPolicy.UnreliableInput.Channel, Is.EqualTo(NetworkChannel.Input));
                Assert.That(NetworkSendPolicy.UnreliableSnapshot.Mode, Is.EqualTo(NetworkSendMode.Unreliable));
                Assert.That(NetworkSendPolicy.UnreliableSnapshot.Channel, Is.EqualTo(NetworkChannel.Snapshot));
            }
        }
    }
}
