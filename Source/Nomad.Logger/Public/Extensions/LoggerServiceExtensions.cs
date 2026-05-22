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
    public static class LoggerServiceExtensions
    {
        public static ILoggerCategory For<T>(
            this ILoggerService logger,
            LogLevel level = LogLevel.Info,
            bool enabled = true
        )
        {
            ArgumentGuard.ThrowIfNull(logger);

            return logger.CreateCategory(
                typeof(T).Name,
                level,
                enabled
            );
        }

        public static ILoggerCategory For(
            this ILoggerService logger,
            string name,
            LogLevel level = LogLevel.Info,
            bool enabled = true
        )
        {
            ArgumentGuard.ThrowIfNull(logger);

            return logger.CreateCategory(
                name,
                level,
                enabled
            );
        }
    }
}
