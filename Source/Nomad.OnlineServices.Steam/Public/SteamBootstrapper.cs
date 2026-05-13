/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.Abstractions;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.ServiceRegistry.Interfaces;
using System.Reflection;
using Nomad.Core.Util.Attributes;
using Steamworks;
using Nomad.OnlineServices.Steam.Private.Services;

#if NET5_0_OR_GREATER
using Nomad.Core.Util;
#endif

namespace Nomad.OnlineServices.Steam
{
    /// <summary>
    ///
    /// </summary>
    public sealed class SteamBootstrapper : IBootstrapper
    {
        private IOnlinePlatformService? _service;

        /// <summary>
        ///
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="locator"></param>
        public void Initialize(IServiceRegistry registry, IServiceLocator locator)
        {
#if NET5_0_OR_GREATER
            var steamworksAssembly = typeof(SteamAPI).Assembly;
            InteropAssemblyResolver.Hook(steamworksAssembly, "steam_api", "libsteam_api", "steam_api64");
            InteropAssemblyResolver.Hook(steamworksAssembly, "steam_api64", "libsteam_api", "steam_api64");
            InteropAssemblyResolver.Hook(steamworksAssembly, "libsteam_api", "libsteam_api", "steam_api64");
#endif
            var logger = locator.GetService<ILoggerService>();
            _service = new SteamService(
                logger,
                locator.GetService<IFileSystem>(),
                locator.GetService<IEngineService>(),
                locator.GetService<IGameEventRegistryService>(),
                locator.GetService<ICVarSystemService>()
            );
            registry.AddSingleton(_service);

            var attribute = Assembly.GetAssembly(typeof(SteamBootstrapper)).GetCustomAttribute<NomadModule>();
            logger.PrintLine($"Initialized {attribute.Name}\n\tBuildId = {attribute.BuildId}\n\tCompileTime = {attribute.CompileTime}\n\tVersion = {attribute.VersionMajor}.{attribute.VersionMinor}.{attribute.VersionPatch}");
        }

        /// <summary>
        ///
        /// </summary>
        public void Shutdown()
        {
            _service?.Dispose();
        }
    }
}
