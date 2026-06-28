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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Nomad.Core.OnlineServices;
using NUnit.Framework;

namespace Nomad.Core.Tests.OnlineServices
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("OnlineServices")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class OnlineServiceValueTests
    {
        [Test]
        public void PeerId_EqualityValidityHashAndString_FollowGuidValue()
        {
            Guid guid = Guid.NewGuid();
            var left = new PeerId(guid);
            var right = new PeerId(guid);
            var other = new PeerId(Guid.NewGuid());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(left.IsValid, Is.True);
                Assert.That(PeerId.Invalid.IsValid, Is.False);
                Assert.That(left.Equals(right), Is.True);
                Assert.That(left.Equals((object)right), Is.True);
                Assert.That(left.Equals(other), Is.False);
                Assert.That(left.Equals("not a peer"), Is.False);
                Assert.That(left == right, Is.True);
                Assert.That(left != other, Is.True);
                Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
                Assert.That(left.ToString(), Is.EqualTo(guid.ToString()));
            }
        }

        [Test]
        public void LobbyId_EqualityEmptyHashAndString_FollowGuidValue()
        {
            Guid guid = Guid.NewGuid();
            var left = new LobbyId(guid);
            var right = new LobbyId(guid);
            var other = new LobbyId(Guid.NewGuid());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(left.IsEmpty, Is.False);
                Assert.That(LobbyId.Empty.IsEmpty, Is.True);
                Assert.That(LobbyId.Invalid, Is.EqualTo(LobbyId.Empty));
                Assert.That(left.Equals(right), Is.True);
                Assert.That(left.Equals((object)right), Is.True);
                Assert.That(left.Equals(other), Is.False);
                Assert.That(left.Equals("not a lobby"), Is.False);
                Assert.That(left == right, Is.True);
                Assert.That(left != other, Is.True);
                Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
                Assert.That(left.ToString(), Is.EqualTo(guid.ToString("N")));
            }
        }

        [Test]
        public void NetworkPacketInfo_Constructor_StoresAllFields()
        {
            var peer = new PeerId(Guid.NewGuid());

            var packet = new NetworkPacketInfo(peer, 128, NetworkSendMode.UnreliableNoDelay);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(packet.From, Is.EqualTo(peer));
                Assert.That(packet.BytesWritten, Is.EqualTo(128));
                Assert.That(packet.Mode, Is.EqualTo(NetworkSendMode.UnreliableNoDelay));
            }
        }

        [Test]
        public void LobbyRecords_WithInitProperties_RetainAssignedState()
        {
            var lobbyId = new LobbyId(Guid.NewGuid());
            var peerId = new PeerId(Guid.NewGuid());
            var metadata = new Dictionary<string, string> { ["map"] = "arena" };

            var createInfo = new LobbyCreateInfo
            {
                Name = "Test Lobby",
                Map = "Arena",
                GameMode = "Duel",
                MaxPlayers = 8,
                Visibility = LobbyVisibility.FriendsOnly,
                Metadata = metadata
            };
            var lobbyInfo = new LobbyInfo
            {
                Id = lobbyId,
                Name = createInfo.Name,
                Map = createInfo.Map,
                GameMode = createInfo.GameMode,
                OwnerId = 42,
                MaxPlayers = createInfo.MaxPlayers,
                PlayerCount = 1,
                Visibility = createInfo.Visibility,
                Metadata = metadata
            };
            var member = new LobbyMemberInfo
            {
                Id = peerId,
                DisplayName = "Raio",
                IsOwner = true,
                IsLocal = true,
                Status = LobbyMemberState.Connected
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(createInfo.Name, Is.EqualTo("Test Lobby"));
                Assert.That(createInfo.Map, Is.EqualTo("Arena"));
                Assert.That(createInfo.GameMode, Is.EqualTo("Duel"));
                Assert.That(createInfo.MaxPlayers, Is.EqualTo(8));
                Assert.That(createInfo.Visibility, Is.EqualTo(LobbyVisibility.FriendsOnly));
                Assert.That(createInfo.Metadata, Is.SameAs(metadata));
                Assert.That(lobbyInfo.Id, Is.EqualTo(lobbyId));
                Assert.That(lobbyInfo.OwnerId, Is.EqualTo(42));
                Assert.That(lobbyInfo.PlayerCount, Is.EqualTo(1));
                Assert.That(member.Id, Is.EqualTo(peerId));
                Assert.That(member.DisplayName, Is.EqualTo("Raio"));
                Assert.That(member.IsOwner, Is.True);
                Assert.That(member.IsLocal, Is.True);
                Assert.That(member.Status, Is.EqualTo(LobbyMemberState.Connected));
            }
        }

        [Test]
        public void OnlineServiceEnums_ExposeStableMinMaxAndExpectedValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NetworkSendMode.Min, Is.EqualTo(NetworkSendMode.Unreliable));
                Assert.That(NetworkSendMode.Max, Is.EqualTo(NetworkSendMode.Reliable));
                Assert.That((byte)NetworkConnectionState.Disconnected, Is.EqualTo(0));
                Assert.That(Enum.IsDefined(typeof(NetworkConnectionState), NetworkConnectionState.Kicked), Is.True);
                Assert.That(Enum.IsDefined(typeof(LobbyVisibility), LobbyVisibility.Private), Is.True);
                Assert.That(Enum.IsDefined(typeof(LobbyLeaveReason), LobbyLeaveReason.Disconnected), Is.True);
            }
        }
    }
}
