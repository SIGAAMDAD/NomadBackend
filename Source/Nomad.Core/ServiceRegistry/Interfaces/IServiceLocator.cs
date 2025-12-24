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

namespace Nomad.Core.ServiceRegistry.Interfaces
{
    /// <summary>
    ///
    /// </summary>
    public interface IServiceLocator : IDisposable
    {
        IServiceRegistry Collection { get; }

        TService GetService<TService>() where TService : class;
        bool TryGetService<TService>(out TService service) where TService : class;
        IEnumerable<TService> GetServices<TService>() where TService : class;
        IServiceScope CreateScope();
        TService CreateInstance<TService>() where TService : class;
    }
}
