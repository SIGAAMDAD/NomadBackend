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
using System.Threading.Tasks;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Private.Session;
using Nomad.Networking.Session;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Session
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Session")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSessionServiceTests
    {
        [Test]
        public void Constructor_WhenRequiredDependencyIsNull_ThrowsArgumentNullException()
        {
            var lobby = new RecordingLobbyService();
            var net = new RecordingNetDriver();
            var events = new RecordingEventRegistry();
            var logger = new RecordingLoggerService();

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentNullException>(() => new NetworkSessionService(null, net, events, logger));
                Assert.Throws<ArgumentNullException>(() => new NetworkSessionService(lobby, null, events, logger));
                Assert.Throws<ArgumentNullException>(() => new NetworkSessionService(lobby, net, null, logger));
                Assert.Throws<ArgumentNullException>(() => new NetworkSessionService(lobby, net, events, null));
            }
        }

        [Test]
        public async Task StartHostAsync_WhenListenFails_ReturnsFalseWithoutSession()
        {
            var fixture = CreateFixture();
            fixture.NetDriver.ListenResult = false;

            bool started = await fixture.Service.StartHostAsync(new LobbyCreateInfo { MaxPlayers = 4 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(started, Is.False);
                Assert.That(fixture.Service.IsSessionActive, Is.False);
                Assert.That(fixture.LobbyService.LastCreateInfo, Is.Null);
            }
        }

        [Test]
        public async Task StartHostAsync_WhenLobbyCreateFails_ReturnsFalseWithoutSession()
        {
            var fixture = CreateFixture();
            fixture.LobbyService.CreateSucceeds = false;

            bool started = await fixture.Service.StartHostAsync(new LobbyCreateInfo { MaxPlayers = 4 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(started, Is.False);
                Assert.That(fixture.Service.IsSessionActive, Is.False);
            }
        }

        [Test]
        public async Task StartHostAsync_WhenLobbyCreateSucceeds_CreatesHostSessionAndPublishesEvent()
        {
            var fixture = CreateFixture();
            PeerId local = new PeerId(Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"));
            PeerId remote = new PeerId(Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"));
            fixture.LobbyService.SetMembers(
                new LobbyMemberInfo { Id = local, DisplayName = "Host", IsOwner = true, IsLocal = true },
                new LobbyMemberInfo { Id = remote, DisplayName = "Client", IsOwner = false, IsLocal = false }
            );

            bool started = await fixture.Service.StartHostAsync(new LobbyCreateInfo { MaxPlayers = 8 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(started, Is.True);
                Assert.That(fixture.Service.IsSessionActive, Is.True);
                Assert.That(fixture.Service.IsHost, Is.True);
                Assert.That(fixture.Service.IsClient, Is.False);
                Assert.That(fixture.Service.CurrentSession, Is.Not.Null);
                Assert.That(fixture.Service.CurrentSession!.Mode, Is.EqualTo(NetworkSessionMode.Host));
                Assert.That(fixture.Service.CurrentSession.LocalPeerId, Is.EqualTo(local));
                Assert.That(fixture.Service.CurrentSession.HostPeerId, Is.EqualTo(local));
                Assert.That(fixture.Service.CurrentSession.Peers, Has.Count.EqualTo(2));
                Assert.That(((TestGameEvent<NetworkSessionChangedEventArgs>)fixture.Service.SessionChanged).PublishCallCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task JoinAsClientAsync_WhenJoinFails_ReturnsFalseWithoutSession()
        {
            var fixture = CreateFixture();
            fixture.LobbyService.JoinSucceeds = false;

            bool joined = await fixture.Service.JoinAsClientAsync(new LobbyId(Guid.NewGuid()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(joined, Is.False);
                Assert.That(fixture.Service.IsSessionActive, Is.False);
                Assert.That(fixture.NetDriver.ConnectCallCount, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task JoinAsClientAsync_WhenJoinSucceeds_CreatesClientSessionAndConnectsToHost()
        {
            var fixture = CreateFixture();
            PeerId local = new PeerId(Guid.Parse("cccccccc-3333-3333-3333-333333333333"));
            PeerId host = new PeerId(Guid.Parse("dddddddd-4444-4444-4444-444444444444"));
            LobbyId lobbyId = new LobbyId(Guid.NewGuid());
            fixture.LobbyService.SetMembers(
                new LobbyMemberInfo { Id = host, DisplayName = "Host", IsOwner = true, IsLocal = false },
                new LobbyMemberInfo { Id = local, DisplayName = "Client", IsOwner = false, IsLocal = true }
            );

            bool joined = await fixture.Service.JoinAsClientAsync(lobbyId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(joined, Is.True);
                Assert.That(fixture.Service.IsSessionActive, Is.True);
                Assert.That(fixture.Service.IsClient, Is.True);
                Assert.That(fixture.Service.CurrentSession!.Mode, Is.EqualTo(NetworkSessionMode.Client));
                Assert.That(fixture.Service.CurrentSession.LocalPeerId, Is.EqualTo(local));
                Assert.That(fixture.Service.CurrentSession.HostPeerId, Is.EqualTo(host));
                Assert.That(fixture.NetDriver.ConnectCallCount, Is.EqualTo(1));
                Assert.That(fixture.NetDriver.LastConnectedPeer, Is.EqualTo(host));
            }
        }

        [Test]
        public async Task SendAndBroadcast_ForwardOnlyWhenSessionAllowsIt()
        {
            var fixture = CreateFixture();
            PeerId local = new PeerId(Guid.Parse("aaaaaaaa-5555-5555-5555-555555555555"));
            PeerId remote = new PeerId(Guid.Parse("bbbbbbbb-6666-6666-6666-666666666666"));
            fixture.LobbyService.SetMembers(
                new LobbyMemberInfo { Id = local, DisplayName = "Host", IsOwner = true, IsLocal = true },
                new LobbyMemberInfo { Id = remote, DisplayName = "Client", IsOwner = false, IsLocal = false }
            );
            fixture.Service.SendToHost(new byte[] { 9 }, NetworkSendMode.Reliable);
            Assert.That(fixture.NetDriver.SendCallCount, Is.EqualTo(0));

            await fixture.Service.StartHostAsync(new LobbyCreateInfo { MaxPlayers = 2 });
            fixture.Service.SendToHost(new byte[] { 1, 2 }, NetworkSendMode.Reliable);
            fixture.Service.SendToPeer(remote, new byte[] { 3 }, NetworkSendMode.Unreliable);
            fixture.Service.Broadcast(new byte[] { 4 }, NetworkSendMode.UnreliableNoDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.NetDriver.SendCallCount, Is.EqualTo(3));
                Assert.That(fixture.NetDriver.LastSentPeer, Is.EqualTo(remote));
                Assert.That(fixture.NetDriver.LastSendMode, Is.EqualTo(NetworkSendMode.UnreliableNoDelay));
            }
        }

        [Test]
        public async Task StopAsync_ClosesConnectionsLeavesLobbyAndClearsSession()
        {
            var fixture = CreateFixture();
            fixture.LobbyService.SetMembers(new LobbyMemberInfo
            {
                Id = new PeerId(Guid.NewGuid()),
                DisplayName = "Host",
                IsOwner = true,
                IsLocal = true
            });
            await fixture.Service.StartHostAsync(new LobbyCreateInfo { MaxPlayers = 2 });

            await fixture.Service.StopAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.NetDriver.CloseAllCallCount, Is.EqualTo(1));
                Assert.That(fixture.NetDriver.LastCloseAllReason, Is.EqualTo("Network session stopped"));
                Assert.That(fixture.Service.IsSessionActive, Is.False);
                Assert.That(fixture.Service.CurrentSession, Is.Null);
            }
        }

        [Test]
        public async Task ConnectionEvents_UpdatePeersAndPublishPeerEvents()
        {
            var fixture = CreateFixture();
            PeerId local = new PeerId(Guid.Parse("aaaaaaaa-7777-7777-7777-777777777777"));
            PeerId remote = new PeerId(Guid.Parse("bbbbbbbb-8888-8888-8888-888888888888"));
            fixture.LobbyService.SetMembers(
                new LobbyMemberInfo { Id = local, DisplayName = "Host", IsOwner = true, IsLocal = true },
                new LobbyMemberInfo { Id = remote, DisplayName = "Remote", IsOwner = false, IsLocal = false }
            );
            await fixture.Service.StartHostAsync(new LobbyCreateInfo { MaxPlayers = 4 });

            fixture.NetDriver.RaiseConnectionEstablished(new NetConnection(remote, NetworkConnectionState.Connected));
            fixture.NetDriver.RaiseConnectionClosed(new NetConnection(remote, NetworkConnectionState.Disconnected));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(((TestGameEvent<PeerConnectedEventArgs>)fixture.Service.PeerConnected).PublishCallCount, Is.EqualTo(1));
                Assert.That(((TestGameEvent<PeerDisconnectedEventArgs>)fixture.Service.PeerDisconnected).PublishCallCount, Is.EqualTo(1));
                Assert.That(fixture.Service.CurrentSession!.Peers, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void TryReceive_ForwardsToNetDriver()
        {
            var fixture = CreateFixture();
            PeerId peer = new PeerId(Guid.NewGuid());
            fixture.NetDriver.EnqueueReceive(peer, new byte[] { 1, 2, 3 }, NetworkSendMode.Reliable);
            byte[] destination = new byte[8];

            bool received = fixture.Service.TryReceive(destination, out NetworkPacketInfo info);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received, Is.True);
                Assert.That(destination[0], Is.EqualTo(1));
                Assert.That(destination[1], Is.EqualTo(2));
                Assert.That(destination[2], Is.EqualTo(3));
                Assert.That(info.From, Is.EqualTo(peer));
                Assert.That(info.BytesWritten, Is.EqualTo(3));
            }
        }

        [Test]
        public void Dispose_CanBeCalledMoreThanOnce()
        {
            var fixture = CreateFixture();

            fixture.Service.Dispose();
            Assert.DoesNotThrow(() => fixture.Service.Dispose());
        }

        private static Fixture CreateFixture()
        {
            var lobby = new RecordingLobbyService();
            var net = new RecordingNetDriver();
            var events = new RecordingEventRegistry();
            var logger = new RecordingLoggerService();
            return new Fixture(new NetworkSessionService(lobby, net, events, logger), lobby, net);
        }

        private sealed class Fixture
        {
            public readonly NetworkSessionService Service;
            public readonly RecordingLobbyService LobbyService;
            public readonly RecordingNetDriver NetDriver;

            public Fixture(NetworkSessionService service, RecordingLobbyService lobbyService, RecordingNetDriver netDriver)
            {
                Service = service;
                LobbyService = lobbyService;
                NetDriver = netDriver;
            }
        }
    }
}
