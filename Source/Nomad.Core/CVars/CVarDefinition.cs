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
    /// A typed reference to a CVar declaration.
    /// </summary>
    /// <typeparam name="T">The CVar value type.</typeparam>
    public readonly struct CVarDefinition<T>
    {
        public CVarDefinition(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// The registered CVar name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates registration information for this CVar definition.
        /// </summary>
        /// <param name="defaultValue">The CVar default value.</param>
        /// <param name="description">The CVar description.</param>
        /// <param name="group">The CVar group.</param>
        /// <param name="flags">The CVar flags.</param>
        /// <param name="validator">The optional validator.</param>
        /// <returns>The CVar creation information.</returns>
        public CVarCreateInfo<T> CreateInfo(
            T defaultValue,
            string description = "",
            string group = "Default",
            CVarFlags flags = CVarFlags.None,
            Func<T, bool>? validator = null)
        {
            return new CVarCreateInfo<T> {
                Name = Name,
                DefaultValue = defaultValue,
                Description = description,
                Group = group,
                Flags = flags,
                Validator = validator
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
