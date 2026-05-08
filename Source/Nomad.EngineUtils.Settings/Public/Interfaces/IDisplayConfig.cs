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

using Nomad.Core.Engine.Rendering;
using Nomad.Core.Engine.Windowing;

namespace Nomad.EngineUtils.Settings.Interfaces
{
    /// <summary>
    ///
    /// </summary>
    public interface IDisplayConfig
    {
        /// <summary>
        ///
        /// </summary>
        int MonitorIndex { get; set; }

        /// <summary>
        ///
        /// </summary>
        int MaximumFrameRate { get; set; }

        /// <summary>
        ///
        /// </summary>
        float Brightness { get; set; }

        /// <summary>
        ///
        /// </summary>
        VSyncMode VSyncMode { get; set; }

        /// <summary>
        ///
        /// </summary>
        WindowMode WindowMode { get; set; }

        /// <summary>
        ///
        /// </summary>
        WindowResolution Resolution { get; set; }

        /// <summary>
        ///
        /// </summary>
        AntiAliasingMode AntiAliasing { get; set; }

        /// <summary>
        ///
        /// </summary>
        AspectRatio AspectRatio { get; set; }

        /// <summary>
        ///
        /// </summary>
        bool HDREnabled { get; set; }
    }
}
