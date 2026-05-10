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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class ResultObjectPayloadAttribute : Attribute
    {
        public string Name { get; }
        public Type Type { get; }
        public int Order { get; }

        /// <summary>
        /// Explicit C# type name for cases where typeof(...) cannot express the desired closed type.
        /// Prefer fully-qualified names, for example: global::System.Collections.Generic.IReadOnlyList&lt;MyApp.User&gt;.
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// When true, the generated property and factory parameter are nullable/defaultable.
        /// </summary>
        public bool IsOptional { get; set; }

        public ResultObjectPayloadAttribute(string name, Type type, int order)
        {
            Name = name;
            Type = type;
            Order = order;
        }
    }
}
