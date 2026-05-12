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

using System.Reflection;
using Nomad.Core.Abstractions;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.Util.Attributes;
using Nomad.Networking.Authority;
using Nomad.Networking.Events;
using Nomad.Networking.Messaging;
using Nomad.Networking.Private.Authority;
using Nomad.Networking.Private.Events;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Private.Rpc;
using Nomad.Networking.Private.Session;
using Nomad.Networking.Private.Transport;
using Nomad.Networking.Rpc;
using Nomad.Networking.Session;
using Nomad.Networking.Transport;

namespace Nomad.Networking
{
    /// <summary>
    ///
    /// </summary>
    public sealed class NetworkBootstrapper : IBootstrapper
    {
        private INetworkRpcBus? _rpcBus = null;
        private INetworkEventBus? _eventBus = null;
        private INetworkMessageRegistry? _messageRegistry = null;
        private INetworkSerializer? _serializer = null;
        private INetworkTransport? _transport = null;
        private INetworkSessionService? _sessionService = null;
        private INetworkAuthority? _authority = null;

        /// <summary>
        ///
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="locator"></param>
        public void Initialize(IServiceRegistry registry, IServiceLocator locator)
        {
            var logger = locator.GetService<ILoggerService>();
            var onlinePlatformService = locator.GetService<IOnlinePlatformService>();

            _messageRegistry = new NetworkMessageRegistry();
            _serializer = new NetworkSerializer();
            _sessionService = new NetworkSessionService(
                onlinePlatformService.Lobbies,
                onlinePlatformService.NetDriver,
                locator.GetService<IGameEventRegistryService>()
            );
            _transport = new NetworkTransport( _sessionService );
            _authority = new NetworkAuthority();

            _rpcBus = new NetworkRpcBus( _messageRegistry, _serializer, _transport, _authority );
            _eventBus = new NetworkEventBus( _messageRegistry, _serializer, _transport, _authority );

            registry.AddSingleton(_messageRegistry);
            registry.AddSingleton(_serializer);
            registry.AddSingleton(_sessionService);
            registry.AddSingleton(_transport);
            registry.AddSingleton(_authority);
            registry.AddSingleton(_rpcBus);
            registry.AddSingleton(_eventBus);

            var attribute = Assembly.GetAssembly(typeof(NetworkBootstrapper)).GetCustomAttribute<NomadModule>();
            logger.PrintLine($"Initialized {attribute.Name}\n\tBuildId = {attribute.BuildId}\n\tCompileTime = {attribute.CompileTime}\n\tVersion = {attribute.VersionMajor}.{attribute.VersionMinor}.{attribute.VersionPatch}");
        }

        /// <summary>
        ///
        /// </summary>
        public void Shutdown()
        {
        }
    }
}
