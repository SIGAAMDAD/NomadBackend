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

using System;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Interfaces;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.CVars;

namespace Nomad.Audio.Fmod
{
    /// <summary>
    /// Initializes FMOD.
    /// </summary>
    public static class FMODBootstrapper
    {
        /// <summary>
        /// Initializes the FMOD audio backend.
        /// </summary>
        /// <param name="locator"></param>
        /// <param name="registry"></param>
        public static void Initialize(IServiceLocator locator, IServiceRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(locator);

            ILoggerService logger = locator.GetService<ILoggerService>();
            ICVarSystemService cvarSystem = locator.GetService<ICVarSystemService>();

            try
            {
                var system = new FMODDevice(locator, registry);
                var listener = new FMODListenerService(logger, system);

                registry.RegisterSingleton<IListenerService>(listener);
            }
            catch (FMODException e)
            {
                logger.PrintError($"FMODBootstrapper: error initializing FMOD audio system - {e.Error}\n{e}");
            }
        }
    }
}
