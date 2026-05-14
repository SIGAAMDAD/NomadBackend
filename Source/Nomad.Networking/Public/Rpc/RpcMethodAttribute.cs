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

namespace Nomad.Networking.Rpc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RpcMethodAttribute : Attribute
    {
        public string Name { get; }
        public string NameSpace { get; }

        public RpcMethodAttribute(string name, string nameSpace = null)
        {
            Name = name;
            NameSpace = nameSpace;
        }
    }
}
