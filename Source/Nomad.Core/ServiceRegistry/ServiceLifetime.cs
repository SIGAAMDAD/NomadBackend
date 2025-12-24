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

namespace Nomad.Core.ServiceRegistry
{
    /// <summary>
    /// The lifetime of a registered service
    /// </summary>
    public enum ServiceLifetime : byte
    {
        Singleton,      // one instance per container
        Transient,      // new instance each time
        Scoped,         // one instance per scope
        Thread,         // one instance per thread
        Scene,          // new instance each time the scene is changed

        Count
    }
}
