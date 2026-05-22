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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Logger;

namespace Nomad.Logger.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public static class LoggerCategoryFormattingExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Info(
            this ILoggerCategory category,
            string format,
            params object[]? args
        )
        {
            category.PrintLine(string.Format(format, args));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Warning(
            this ILoggerCategory category,
            string format,
            params object[]? args
        )
        {
            category.PrintWarning(string.Format(format, args));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Error(
            this ILoggerCategory category,
            string format,
            params object[]? args
        )
        {
            category.PrintError(string.Format(format, args));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Debug(
            this ILoggerCategory category,
            string format,
            params object[]? args
        )
        {
            category.PrintDebug(string.Format(format, args));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        public static void Error(
            this ILoggerCategory category,
            Exception exception,
            string message
        )
        {
            category.PrintError($"{message}{Environment.NewLine}{exception}");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="category"></param>
        /// <param name="exception"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Error(
            this ILoggerCategory category,
            Exception exception,
            string format,
            params object?[] args
        )
        {
            category.PrintError(
                $"{string.Format(format, args)}{Environment.NewLine}{exception}"
            );
        }

        public static void Exception(
            this ILoggerCategory category,
            Exception exception
        )
        {
            ArgumentGuard.ThrowIfNull(category);
            ArgumentGuard.ThrowIfNull(exception);

            category.PrintError(exception.ToString());
        }

        public static void Exception(
            this ILoggerCategory category,
            Exception exception,
            string message
        )
        {
            ArgumentGuard.ThrowIfNull(category);
            ArgumentGuard.ThrowIfNull(exception);

            category.PrintError($"{message}{Environment.NewLine}{exception}");
        }

        public static void Exception(
            this ILoggerCategory category,
            Exception exception,
            string format,
            params object?[] args
        )
        {
            ArgumentGuard.ThrowIfNull(category);
            ArgumentGuard.ThrowIfNull(exception);

            category.PrintError(
                $"{string.Format(format, args)}{Environment.NewLine}{exception}"
            );
        }
    }
}
