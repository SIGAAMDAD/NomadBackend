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
using Nomad.Core.Logger;

namespace Nomad.Logger.Extensions
{
    public readonly struct LogScope : IDisposable
    {
        private readonly ILoggerCategory? _category;
        private readonly string _name;
        private readonly long _startTicks;

        public LogScope(ILoggerCategory category, string name)
        {
            _category = category;
            _name = name;
            _startTicks = Environment.TickCount64;

            _category.PrintLine($"Begin: {_name}");
        }

        public void Dispose()
        {
            if (_category == null)
            {
                return;
            }

            long elapsedMS = Environment.TickCount64 - _startTicks;
            _category.PrintLine($"End: {_name} ({elapsedMS}ms)");
        }
    }
}
