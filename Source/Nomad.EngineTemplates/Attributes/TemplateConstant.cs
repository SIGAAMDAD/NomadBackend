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

namespace Nomad.EngineTemplates.Attributes
{
    /// <summary>
    /// Declares a static readonly field (or constant) to be emitted in the generated engine wrapper.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal class TemplateConstant : Attribute
    {
        /// <summary>
        /// The name of the constant/field.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the constant/field.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// The default value expression (used when no engine‑specific override is provided).
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Engine‑specific override for Godot.
        /// </summary>
        public string? GodotValue { get; set; }

        /// <summary>
        /// Engine‑specific override for Unity.
        /// </summary>
        public string? UnityValue { get; set; }

        /// <summary>
        /// Engine‑specific override for Unreal.
        /// </summary>
        public string? UnrealValue { get; set; }

        /// <summary>
        /// If true, emits as <c>public const</c> (value must be compile‑time constant).
        /// Defaults to false, emitting <c>public static readonly</c>.
        /// </summary>
        public bool IsConstant { get; set; } = false;

        /// <summary>
        /// Optional documentation summary for the generated member.
        /// </summary>
        public string? Documentation { get; set; }
    }
}
