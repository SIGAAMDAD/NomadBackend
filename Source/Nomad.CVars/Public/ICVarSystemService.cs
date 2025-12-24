/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;

namespace Nomad.CVars
{
    /// <summary>
    ///
    /// </summary>
    public interface ICVarSystemService : IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="cvar"></param>
        void Register(ICVar cvar);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createInfo"></param>
        /// <returns></returns>
        ICVar<T> Register<T>(in CVarCreateInfo<T> createInfo);

        /// <summary>
        ///
        /// </summary>
        /// <param name="cvar"></param>
        void Unregister(ICVar cvar);

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool CVarExists(string name);

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        ICVar<T>? GetCVar<T>(string name);

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ICVar? GetCVar(string name);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        ICVar[]? GetCVars();

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ICVar<T>[]? GetCVarsWithValueType<T>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        ICVar[]? GetCVarsInGroup(string groupName);

        /// <summary>
        ///
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        bool GroupExists(string groupName);

        /// <summary>
        ///
        /// </summary>
        void Restart();

        /// <summary>
        ///
        /// </summary>
        /// <param name="configFile"></param>
        void Load(string configFile);

        /// <summary>
        ///
        /// </summary>
        /// <param name="configFile"></param>
        void Save(string configFile);
    }
}
