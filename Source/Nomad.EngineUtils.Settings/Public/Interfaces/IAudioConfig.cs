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

namespace Nomad.EngineUtils.Settings.Interfaces
{
    /// <summary>
    ///
    /// </summary>
    public interface IAudioConfig
    {
        /// <summary>
        ///
        /// </summary>
        int AudioDriver { get; set; }

        /// <summary>
        ///
        /// </summary>
        int OutputDeviceIndex { get; set; }

        /// <summary>
        ///
        /// </summary>
        int SpeakerMode { get; set; }

        /// <summary>
        ///
        /// </summary>
        int MaxChannels { get; set; }

        /// <summary>
        ///
        /// </summary>
        float MusicVolume { get; set; }

        /// <summary>
        ///
        /// </summary>
        float SoundEffectsVolume { get; set; }

        /// <summary>
        ///
        /// </summary>
        bool MusicOn { get; set; }

        /// <summary>
        ///
        /// </summary>
        bool SoundEffectsOn { get; set; }
    }
}
