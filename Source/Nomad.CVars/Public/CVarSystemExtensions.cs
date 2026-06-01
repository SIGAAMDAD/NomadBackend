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

using Nomad.Core.CVars;
using Nomad.Core.Exceptions;

namespace Nomad.CVars
{
    /// <summary>
    ///
    /// </summary>
    public static class CVarSystemExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cvarSystem"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="CVarMissing"></exception>
        public static ICVar<T> GetCVarOrThrow<T>(this ICVarSystemService cvarSystem, string name)
        {
            return cvarSystem.GetCVar<T>(name) ?? throw new CVarMissing(name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cvarSystem"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static ICVar<T>? GetCVar<T>(this ICVarSystemService cvarSystem, CVarDefinition<T> definition)
        {
            return cvarSystem.GetCVar<T>(definition.Name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cvarSystem"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        /// <exception cref="CVarMissing"></exception>
        public static ICVar<T> GetCVarOrThrow<T>(this ICVarSystemService cvarSystem, CVarDefinition<T> definition)
        {
            return cvarSystem.GetCVar<T>(definition.Name) ?? throw new CVarMissing(definition.Name);
        }
    }
}
