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

namespace Nomad.Core.CVars
{
    /// <summary>
    /// Declares a CVar for source-generated registry creation.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Struct |
        AttributeTargets.Field |
        AttributeTargets.Property,
        AllowMultiple = true,
        Inherited = false)]
    public sealed class CVarAttribute : Attribute
    {
        public CVarAttribute(string name, object defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public CVarAttribute(string name, bool defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public CVarAttribute(string name, int defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public CVarAttribute(string name, uint defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public CVarAttribute(string name, float defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public CVarAttribute(string name, string defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public object DefaultValue { get; }

        public Type? ValueType { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Group { get; set; } = "Default";

        public CVarFlags Flags { get; set; } = CVarFlags.None;

        public string? ValidatorExpression { get; set; }

        public string? AccessorName { get; set; }
    }
}
