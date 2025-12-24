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

using System.Collections.Generic;
using System;
using Nomad.Core.ServiceRegistry.Interfaces;

namespace Nomad.Core.ServiceRegistry.Services
{
    /// <summary>
    ///
    /// </summary>
    internal sealed class ServiceScope(IServiceLocator locator) : IServiceScope
    {
        public IServiceLocator ServiceLocator => _locator;
        private readonly IServiceLocator _locator = locator;

        private readonly Dictionary<Type, object> _scopedInstance = new Dictionary<Type, object>();

        /*
		===============
		Dispose
		===============
		*/
        public void Dispose()
        {
            foreach (var instance in _scopedInstance.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _scopedInstance.Clear();
        }
    }
}
