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

namespace Nomad.Core.Events
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="TArgs"></typeparam>
    /// <param name="eventArgs"></param>
    public delegate void EventCallback<TArgs>(in TArgs args)
        where TArgs : struct;
}
