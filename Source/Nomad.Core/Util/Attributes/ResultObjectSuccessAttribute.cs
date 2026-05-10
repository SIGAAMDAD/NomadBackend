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
    public sealed class ResultObjectSuccessAttribute : Attribute
    {
        public string[] FieldNames { get; }

        /// <summary>
        /// Name of the generated success factory method. Defaults to Success.
        /// </summary>
        public string MethodName { get; set; } = "Success";

        /// <summary>
        /// Selects which ResultObjectPayload fields are accepted by the generated success factory method.
        /// Omitted fields are initialized to default in success results.
        /// </summary>
        public ResultObjectSuccessAttribute(params string[] fieldNames)
        {
            FieldNames = fieldNames ?? Array.Empty<string>();
        }
    }
}
