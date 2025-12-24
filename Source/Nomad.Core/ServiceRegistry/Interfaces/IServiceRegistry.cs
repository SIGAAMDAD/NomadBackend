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
using System.Collections.Generic;
using Nomad.Core.ServiceRegistry.Services;

namespace Nomad.Core.ServiceRegistry.Interfaces
{
    /// <summary>
    ///
    /// </summary>
    public interface IServiceRegistry
    {
        TService Register<TService, TImplementation>(ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService;

        TService Register<TService>(Func<IServiceLocator, TService> factory, ServiceLifetime lifetime)
            where TService : class;

        TService RegisterSingleton<TService>(TService instance)
            where TService : class;

        TService RegisterSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        bool IsRegistered<TService>()
            where TService : class;

        IEnumerable<ServiceDescriptor> GetDescriptors();
    }
}
