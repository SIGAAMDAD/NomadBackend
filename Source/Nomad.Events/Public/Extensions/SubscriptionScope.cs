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

using System;
using System.Collections.Generic;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;

namespace Nomad.Events.Extensions
{
    /// <summary>
    /// Represents a scoped collection of subscriptions.
    /// </summary>
    public sealed class SubscriptionScope : IDisposable
    {
        private readonly List<ISubscriptionHandle> _handles = new();
        private bool _isDisposed = false;

        public int Count => _handles.Count;

        public T Add<T>(T handle)
            where T :ISubscriptionHandle
        {
            StateGuard.ThrowIfDisposed(_isDisposed, this);
            ArgumentGuard.ThrowIfNull(handle);

            _handles.Add(handle);
            return handle;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                _handles[i].Dispose();
            }

            _handles.Clear();
            _isDisposed = true;
        }
    }
}
