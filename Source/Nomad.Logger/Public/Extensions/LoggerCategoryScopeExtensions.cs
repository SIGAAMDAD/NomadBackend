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

using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Logger;

namespace Nomad.Logger.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public static class LoggerCategoryScopeExtensions
    {
        public static LogScope Scope(
            this ILoggerCategory category,
            string name
        )
        {
            ArgumentGuard.ThrowIfNull(category);
            return new LogScope(category, name);
        }

        public static LogScope Scope(
            this ILoggerCategory category,
            string format,
            params object?[] args
        )
        {
            ArgumentGuard.ThrowIfNull(category);
            return new LogScope(category, string.Format(format, args));
        }
    }
}
