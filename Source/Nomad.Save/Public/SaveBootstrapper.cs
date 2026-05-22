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

using Nomad.Core.Abstractions;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.CVars;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Save.Private.Services;
using Nomad.Save.Services;
using System.Reflection;
using Nomad.Core.Util.Attributes;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Mapping;

namespace Nomad.Save
{
    /// <summary>
    ///
    /// </summary>
    public sealed class SaveBootstrapper : IBootstrapper
    {
        private ISaveDataProvider? _saveProvider;
        private ISaveMappingService? _mappingService;

        /// <summary>
        ///
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="locator"></param>
        public void Initialize(IServiceRegistry registry, IServiceLocator locator)
        {
            ArgumentGuard.ThrowIfNull(registry, nameof(registry));
            ArgumentGuard.ThrowIfNull(locator, nameof(locator));

            var logger = locator.GetService<ILoggerService>();
            var fileSystem = locator.GetService<IFileSystem>();
            var cvarSystem = locator.GetService<ICVarSystemService>();
            var engineService = locator.GetService<IEngineService>();
            var eventFactory = locator.GetService<IGameEventRegistryService>();

            _saveProvider = new SaveDataProvider(engineService, eventFactory, cvarSystem, fileSystem, logger);
            registry.AddSingleton(_saveProvider);

            _mappingService = new SaveMappingService();
            registry.AddSingleton(_mappingService);

            var attribute = Assembly.GetAssembly(typeof(SaveBootstrapper)).GetCustomAttribute<NomadModule>();
            logger.PrintLine($"Initialized {attribute.Name}\n\tBuildId = {attribute.BuildId}\n\tCompileTime = {attribute.CompileTime}\n\tVersion = {attribute.VersionMajor}.{attribute.VersionMinor}.{attribute.VersionPatch}");
        }

        /// <summary>
        ///
        /// </summary>
        public void Shutdown()
        {
            _saveProvider?.Dispose();
        }
    }
}
