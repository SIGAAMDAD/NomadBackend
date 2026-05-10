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

namespace Nomad.Core.Util
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ResultObjectAttribute : Attribute
    {
        public string? Name { get; }
        public bool IsRecord { get; }

        /// <summary>
        /// Overrides the namespace where the generated result object is emitted.
        /// Leave null to use the annotated method's namespace. Set to an empty string to emit into the global namespace.
        /// </summary>
        public string? Namespace { get; set; }

        public ResultObjectAttribute(string? name = null, bool isRecord = false)
        {
            Name = name;
            IsRecord = isRecord;
        }
    }
}
