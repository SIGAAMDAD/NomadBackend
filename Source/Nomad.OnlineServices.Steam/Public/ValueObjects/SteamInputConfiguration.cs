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
using System.Collections.Generic;

namespace Nomad.OnlineServices.Steam.ValueObjects
{
    public sealed record SteamInputConfiguration
    {
        public string ManifestPath { get; }
        public string DefaultActionSet { get; }
        public IReadOnlyList<SteamInputDigitalBinding> DigitalBindings { get; }
        public IReadOnlyList<SteamInputFloatBinding> FloatBindings { get; }
        public IReadOnlyList<SteamInputAxisBinding> AxisBindings { get; }
        public bool ExplicitRunFrame { get; }

        public SteamInputConfiguration(
            string manifestPath,
            string defaultActionSet,
            IReadOnlyList<SteamInputDigitalBinding> digitalBindings,
            IReadOnlyList<SteamInputFloatBinding> floatBindings,
            IReadOnlyList<SteamInputAxisBinding> axisBindings,
            bool explicitRunFrame = false
        )
        {
            ManifestPath = manifestPath ?? throw new ArgumentNullException(nameof(manifestPath));
            DefaultActionSet = defaultActionSet ?? throw new ArgumentNullException(nameof(defaultActionSet));
            DigitalBindings = digitalBindings ?? throw new ArgumentNullException(nameof(digitalBindings));
            FloatBindings = floatBindings ?? throw new ArgumentNullException(nameof(floatBindings));
            AxisBindings = axisBindings ?? throw new ArgumentNullException(nameof(axisBindings));
            ExplicitRunFrame = explicitRunFrame;
        }
    }
}
