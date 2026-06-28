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

using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Authority;
using Nomad.Networking.Events;
using Nomad.Networking.Messaging;
using Nomad.Networking.Rpc;
using Nomad.Networking.Session;
using Nomad.Networking.Tests.Support;
using Nomad.Networking.Transport;
using NUnit.Framework;

namespace Nomad.Networking.Tests
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Bootstrap")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkBootstrapperTests
    {
        [Test]
        public void Shutdown_IsNoOpAndDoesNotThrow()
        {
            var bootstrapper = new NetworkBootstrapper();

            Assert.DoesNotThrow(() => bootstrapper.Shutdown());
        }

        [Test]
        public void Initialize_RegistersNetworkingServices()
        {
            var registry = new RecordingServiceRegistry();
            var locator = new RecordingServiceLocator(registry);
            var lobbyService = new RecordingLobbyService();
            var netDriver = new RecordingNetDriver();
            var logger = new RecordingLoggerService();
            var onlinePlatform = new RecordingOnlinePlatformService
            {
                Lobbies = lobbyService,
                NetDriver = netDriver
            };
            locator.Add<ILoggerService>(logger);
            locator.Add<IOnlinePlatformService>(onlinePlatform);
            locator.Add<IGameEventRegistryService>(new RecordingEventRegistry());
            var bootstrapper = new NetworkBootstrapper();

            bootstrapper.Initialize(registry, locator);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkMessageRegistry)), Is.True);
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkSerializer)), Is.True);
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkSessionService)), Is.True);
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkTransport)), Is.True);
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkAuthority)), Is.True);
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkRpcBus)), Is.True);
                Assert.That(registry.Instances.ContainsKey(typeof(INetworkEventBus)), Is.True);
                Assert.That(logger.Lines, Has.Count.EqualTo(1));
            }
        }
    }
}
