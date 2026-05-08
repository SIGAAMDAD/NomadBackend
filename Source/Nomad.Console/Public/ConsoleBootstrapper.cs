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
using Nomad.Core.CVars;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;

namespace Nomad.Console
{
    /// <summary>
    ///
    /// </summary>
    public class ConsoleBootstrapper : IBootstrapper
    {
        private IConsoleObject? _consoleObject;

        /// <summary>
        ///
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="locator"></param>
        public void Initialize(IServiceRegistry registry, IServiceLocator locator)
        {
            ArgumentGuard.ThrowIfNull(registry);
            ArgumentGuard.ThrowIfNull(locator);

            var eventFactory = locator.GetService<IGameEventRegistryService>();
            var logger = locator.GetService<ILoggerService>();
            var engineService = locator.GetService<IEngineService>();
            var fileSystem = locator.GetService<IFileSystem>();
            var cvarSystem = locator.GetService<ICVarSystemService>();

            _consoleObject = engineService.CreateConsoleObject();

            registry.AddSingleton(new ConsoleHistoryBuffer(eventFactory, new ConsoleHistoryStore(fileSystem)));
        }

        /// <summary>
        ///
        /// </summary>
        public void Shutdown()
        {
            _consoleObject?.Dispose();
        }
    }
}
