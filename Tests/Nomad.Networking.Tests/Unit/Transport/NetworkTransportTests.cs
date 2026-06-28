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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Session;
using Nomad.Networking.Tests.Support;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Transport
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Transport")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkTransportTests
    {
        [Test]
        public void Constructor_WhenSessionServiceIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new NetworkTransport(null));
        }

        [Test]
        public void Properties_ForwardSessionState()
        {
            var sessionService = new FakeNetworkSessionService
            {
                IsSessionActive = true,
                IsHost = true,
                IsClient = false,
                CurrentSession = new NetworkSessionInfo
                {
                    LocalPeerId = new PeerId(Guid.NewGuid()),
                    HostPeerId = new PeerId(Guid.NewGuid())
                }
            };
            var transport = new NetworkTransport(sessionService);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(transport.IsActive, Is.True);
                Assert.That(transport.IsHost, Is.True);
                Assert.That(transport.IsClient, Is.False);
                Assert.That(transport.LocalPeerId, Is.EqualTo(sessionService.CurrentSession.LocalPeerId));
                Assert.That(transport.HostPeerId, Is.EqualTo(sessionService.CurrentSession.HostPeerId));
            }
        }

        [Test]
        public void SendMethods_WhenSessionIsInactive_ReturnFalseWithoutForwarding()
        {
            var sessionService = new FakeNetworkSessionService { IsSessionActive = false };
            var transport = new NetworkTransport(sessionService);

            bool host = transport.SendToHost(new byte[] { 1 }, NetworkSendMode.Reliable);
            bool peer = transport.SendToPeer(new PeerId(Guid.NewGuid()), new byte[] { 1 }, NetworkSendMode.Reliable);
            bool all = transport.Broadcast(new byte[] { 1 }, NetworkSendMode.Reliable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host, Is.False);
                Assert.That(peer, Is.False);
                Assert.That(all, Is.False);
                Assert.That(sessionService.SendToHostCount, Is.EqualTo(0));
                Assert.That(sessionService.SendToPeerCount, Is.EqualTo(0));
                Assert.That(sessionService.BroadcastCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void SendMethods_WhenSessionIsActive_ForwardToSessionService()
        {
            var sessionService = new FakeNetworkSessionService { IsSessionActive = true };
            var transport = new NetworkTransport(sessionService);
            var peer = new PeerId(Guid.NewGuid());

            bool host = transport.SendToHost(new byte[] { 1, 2 }, NetworkSendMode.Reliable);
            bool target = transport.SendToPeer(peer, new byte[] { 3 }, NetworkSendMode.Unreliable);
            bool all = transport.Broadcast(new byte[] { 4 }, NetworkSendMode.UnreliableNoDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(host, Is.True);
                Assert.That(target, Is.True);
                Assert.That(all, Is.True);
                Assert.That(sessionService.SendToHostCount, Is.EqualTo(1));
                Assert.That(sessionService.SendToPeerCount, Is.EqualTo(1));
                Assert.That(sessionService.BroadcastCount, Is.EqualTo(1));
                Assert.That(sessionService.LastPeer, Is.EqualTo(peer));
            }
        }

        [Test]
        public void TryReceive_ForwardsToSessionService()
        {
            var sessionService = new FakeNetworkSessionService();
            var transport = new NetworkTransport(sessionService);
            sessionService.PacketToReceive = new byte[] { 1, 2, 3 };
            sessionService.PacketInfo = new NetworkPacketInfo(new PeerId(Guid.NewGuid()), 3, NetworkSendMode.Reliable);
            byte[] destination = new byte[8];

            bool received = transport.TryReceive(destination, out NetworkPacketInfo info);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received, Is.True);
                Assert.That(destination[0], Is.EqualTo(1));
                Assert.That(destination[1], Is.EqualTo(2));
                Assert.That(destination[2], Is.EqualTo(3));
                Assert.That(info.From, Is.EqualTo(sessionService.PacketInfo.From));
                Assert.That(info.BytesWritten, Is.EqualTo(3));
            }
        }

        private sealed class FakeNetworkSessionService : INetworkSessionService
        {
            public int SendToHostCount;
            public int SendToPeerCount;
            public int BroadcastCount;
            public PeerId LastPeer;
            public byte[] PacketToReceive = Array.Empty<byte>();
            public NetworkPacketInfo PacketInfo;

            public bool IsSessionActive { get; set; }
            public bool IsHost { get; set; }
            public bool IsClient { get; set; }
            public NetworkSessionInfo? CurrentSession { get; set; }
            public IGameEvent<NetworkSessionChangedEventArgs> SessionChanged { get; } = new TestGameEvent<NetworkSessionChangedEventArgs>();
            public IGameEvent<PeerConnectedEventArgs> PeerConnected { get; } = new TestGameEvent<PeerConnectedEventArgs>();
            public IGameEvent<PeerDisconnectedEventArgs> PeerDisconnected { get; } = new TestGameEvent<PeerDisconnectedEventArgs>();

            public Task<bool> StartHostAsync(LobbyCreateInfo info, CancellationToken ct = default)
            {
                return Task.FromResult(false);
            }

            public Task<bool> JoinAsClientAsync(LobbyId lobbyId, CancellationToken ct = default)
            {
                return Task.FromResult(false);
            }

            public Task StopAsync(CancellationToken ct = default)
            {
                return Task.CompletedTask;
            }

            public void SendToHost(ReadOnlySpan<byte> payload, NetworkSendMode mode)
            {
                SendToHostCount++;
            }

            public void SendToPeer(PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode)
            {
                SendToPeerCount++;
                LastPeer = peerId;
            }

            public void Broadcast(ReadOnlySpan<byte> payload, NetworkSendMode mode)
            {
                BroadcastCount++;
            }

            public bool TryReceive(Span<byte> destination, out NetworkPacketInfo info)
            {
                if (PacketToReceive.Length == 0)
                {
                    info = default;
                    return false;
                }

                PacketToReceive.AsSpan().CopyTo(destination);
                info = PacketInfo;
                PacketToReceive = Array.Empty<byte>();
                return true;
            }
        }
    }
}
