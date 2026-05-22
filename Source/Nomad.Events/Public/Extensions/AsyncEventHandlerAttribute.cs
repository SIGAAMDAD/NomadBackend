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
using Nomad.Core.Events;

namespace Nomad.Events.Extensions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class AsyncEventHandlerAttribute : Attribute
    {
        public string Name { get; }
        public string NameSpace { get; }
        public EventFlags Flags { get; init; } = EventFlags.Default;
        public bool Safe { get; init; } = true;

        public AsyncEventHandlerAttribute(string name, string nameSpace)
        {
            Name = name;
            NameSpace = nameSpace;
        }
    }
}
