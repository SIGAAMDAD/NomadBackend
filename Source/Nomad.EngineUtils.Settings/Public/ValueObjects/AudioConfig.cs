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

using Nomad.Audio.ValueObjects;
using Nomad.EngineUtils.Settings.Interfaces;

namespace Nomad.EngineUtils.Settings.ValueObjects
{
    /// <summary>
    ///
    /// </summary>
    public record AudioConfig : IAudioConfig
    {
        /// <summary>
        ///
        /// </summary>
        public int AudioDriver { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int OutputDeviceIndex { get; set; }

        /// <summary>
        ///
        /// </summary>
        public SpeakerMode SpeakerMode { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int MaxChannels { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float MasterVolume { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float MusicVolume { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float SoundEffectsVolume { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool MusicOn { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool SoundEffectsOn { get; set; }
    }
}
