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

using System.Collections.Generic;
using Nomad.CVars;
using Nomad.Core.CVars;
using Nomad.Core.Engine.Rendering;
using Nomad.EngineUtils.Settings.ValueObjects;
using Nomad.EngineUtils.Settings.Interfaces;
using Nomad.Core.Engine.Services;
using Nomad.Core.Engine.Windowing;

namespace Nomad.EngineUtils.Settings.Services
{
    /// <summary>
    ///
    /// </summary>
    public sealed class DisplaySettingsService : SettingsService<DisplayConfig, IDisplayConfig>
    {
        public IReadOnlyList<string> AntiAliasingModes
        {
            get
            {
                var modes = new List<string>();
                for (AntiAliasingMode antiAliasing = AntiAliasingMode.MSAA_2x; antiAliasing <= AntiAliasingMode.MSAA_8x; antiAliasing++)
                {
                    modes.Add(antiAliasing.ToString());
                }
                if (_service.SupportsFXAA)
                {
                    modes.Add(AntiAliasingMode.FXAA.ToString());
                }
                if (_service.SupportsSMAA)
                {
                    modes.Add(AntiAliasingMode.SMAA.ToString());
                }
                if (_service.SupportsTAA)
                {
                    modes.Add(AntiAliasingMode.TAA.ToString());
                }
                return modes;
            }
        }

        private readonly Dictionary<QualitySetting, DisplayConfig> _presets = new();
        private readonly IDisplayService _service;

        /// <summary>
        ///
        /// </summary>
        /// <param name="service"></param>
        /// <param name="cvarSystem"></param>
        public DisplaySettingsService(IDisplayService service, ICVarSystemService cvarSystem)
            : base(cvarSystem)
        {
            _service = service;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="preset"></param>
        public void AddPreset(QualitySetting setting, DisplayConfig preset)
        {
            _presets[setting] = preset;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="setting"></param>
        public void ApplyPreset(QualitySetting setting)
        {
            if (_presets.TryGetValue(setting, out var preset))
            {
                config = preset;
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected override void SaveInternal()
        {
            var windowMode = cvarSystem.GetCVarOrThrow<WindowMode>(Core.Constants.CVars.EngineUtils.Display.WINDOW_MODE);
            windowMode.Value = config.WindowMode;

            var monitorIndex = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Display.MONITOR);
            monitorIndex.Value = config.MonitorIndex;

            var maxFps = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Display.MAX_FPS);
            maxFps.Value = config.MaximumFrameRate;

            var brightness = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Display.BRIGHTNESS);
            brightness.Value = config.Brightness;

            var vsyncMode = cvarSystem.GetCVarOrThrow<VSyncMode>(Core.Constants.CVars.EngineUtils.Display.VSYNC_MODE);
            vsyncMode.Value = config.VSyncMode;

            var windowResolution = cvarSystem.GetCVarOrThrow<WindowResolution>(Core.Constants.CVars.EngineUtils.Display.WINDOW_RESOLUTION);
            windowResolution.Value = config.Resolution;

            var antiAliasing = cvarSystem.GetCVarOrThrow<AntiAliasingMode>(Core.Constants.CVars.EngineUtils.Display.ANTI_ALIASING);
            antiAliasing.Value = config.AntiAliasing;

            var aspectRatio = cvarSystem.GetCVarOrThrow<AspectRatio>(Core.Constants.CVars.EngineUtils.Display.ASPECT_RATIO);
            aspectRatio.Value = config.AspectRatio;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected override DisplayConfig CreateConfig() => new DisplayConfig
        {
            AntiAliasing = cvarSystem.GetCVarOrThrow<AntiAliasingMode>(Core.Constants.CVars.EngineUtils.Display.ANTI_ALIASING).Value,
            WindowMode = cvarSystem.GetCVarOrThrow<WindowMode>(Core.Constants.CVars.EngineUtils.Display.WINDOW_MODE).Value,
            Resolution = cvarSystem.GetCVarOrThrow<WindowResolution>(Core.Constants.CVars.EngineUtils.Display.WINDOW_RESOLUTION).Value,
            MonitorIndex = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Display.MONITOR).Value,
            AspectRatio = cvarSystem.GetCVarOrThrow<AspectRatio>(Core.Constants.CVars.EngineUtils.Display.ASPECT_RATIO).Value,
            MaximumFrameRate = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Display.MAX_FPS).Value,
            VSyncMode = cvarSystem.GetCVarOrThrow<VSyncMode>(Core.Constants.CVars.EngineUtils.Display.VSYNC_MODE).Value,
            Brightness = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Display.BRIGHTNESS).Value,
        };

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected override DisplayConfig CreateDefault() => new DisplayConfig
        {
            AntiAliasing = cvarSystem.GetCVarOrThrow<AntiAliasingMode>(Core.Constants.CVars.EngineUtils.Display.ANTI_ALIASING).DefaultValue,
            WindowMode = cvarSystem.GetCVarOrThrow<WindowMode>(Core.Constants.CVars.EngineUtils.Display.WINDOW_MODE).DefaultValue,
            Resolution = cvarSystem.GetCVarOrThrow<WindowResolution>(Core.Constants.CVars.EngineUtils.Display.WINDOW_RESOLUTION).DefaultValue,
            MonitorIndex = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Display.MONITOR).DefaultValue,
            AspectRatio = cvarSystem.GetCVarOrThrow<AspectRatio>(Core.Constants.CVars.EngineUtils.Display.ASPECT_RATIO).DefaultValue,
            MaximumFrameRate = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Display.MAX_FPS).DefaultValue,
            VSyncMode = cvarSystem.GetCVarOrThrow<VSyncMode>(Core.Constants.CVars.EngineUtils.Display.VSYNC_MODE).DefaultValue,
            Brightness = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Display.BRIGHTNESS).DefaultValue,
        };
    }
}
