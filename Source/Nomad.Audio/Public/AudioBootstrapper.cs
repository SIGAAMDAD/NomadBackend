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

using Nomad.Audio.Interfaces;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;

namespace NomadCore.Systems.Audio.Infrastructure.Startup
{
    /// <summary>
    ///
    /// </summary>
    public static class AudioBootstrapper
    {
        public static void Initialize(IServiceLocator locator, IServiceRegistry registry)
        {
            var system = locator.GetService<IAudioDevice>();
            var logger = locator.GetService<ILoggerService>();
            var cvarSystem = locator.GetService<ICVarSystemService>();
            var listener = locator.GetService<IListenerService>();

            var channelRepository = new FMODChannelRepository(logger, cvarSystem, listener, system.EventRepository, system.GuidRepository as FMODGuidRepository);
            registry.RegisterSingleton<IChannelRepository>(channelRepository);
            registry.RegisterSingleton<IAudioSourceFactory>(new FMODAudioSourceFactory(channelRepository));
            registry.RegisterSingleton<IMusicService>(new FMODMusicService(system.EventRepository, cvarSystem));
        }
    }
}
