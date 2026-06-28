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
using Nomad.CVars;
using Nomad.EngineUtils.Settings.ValueObjects;
using Nomad.EngineUtils.Settings.Interfaces;
using Nomad.Audio.Interfaces;
using System;
using Nomad.Audio.ValueObjects;

namespace Nomad.EngineUtils.Settings.Services
{
    /// <summary>
    ///
    /// </summary>
    public sealed class AudioSettingsService : SettingsService<AudioConfig, IAudioConfig>
    {
        private readonly IAudioDevice _device;

        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        /// <param name="cvarSystem"></param>
        public AudioSettingsService(IAudioDevice device, ICVarSystemService cvarSystem)
            : base(cvarSystem)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        /// <summary>
        ///
        /// </summary>
        protected override void SaveInternal()
        {
            var audioDriver = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.AUDIO_DRIVER);
            audioDriver.Value = config.AudioDriver;

            var outputDeviceIndex = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.OUTPUT_DEVICE_INDEX);
            outputDeviceIndex.Value = config.OutputDeviceIndex;

            var speakerMode = cvarSystem.GetCVarOrThrow<SpeakerMode>(Core.Constants.CVars.EngineUtils.Audio.SPEAKER_MODE);
            speakerMode.Value = config.SpeakerMode;

            var maxChannels = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS);
            maxChannels.Value = config.MaxChannels;

            var masterVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.MASTER_VOLUME);
            masterVolume.Value = config.MasterVolume;

            var musicVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.MUSIC_VOLUME);
            musicVolume.Value = config.MusicVolume;

            var soundEffectsVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME);
            soundEffectsVolume.Value = config.SoundEffectsVolume;

            var musicOn = cvarSystem.GetCVarOrThrow<bool>(Core.Constants.CVars.EngineUtils.Audio.MUSIC_ON);
            musicOn.Value = config.MusicOn;

            var soundEFfectsOn = cvarSystem.GetCVarOrThrow<bool>(Core.Constants.CVars.EngineUtils.Audio.EFFECTS_ON);
            soundEFfectsOn.Value = config.SoundEffectsOn;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected override AudioConfig CreateConfig() => new AudioConfig
        {
            AudioDriver = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.AUDIO_DRIVER).Value,
            OutputDeviceIndex = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.OUTPUT_DEVICE_INDEX).Value,
            SpeakerMode = cvarSystem.GetCVarOrThrow<SpeakerMode>(Core.Constants.CVars.EngineUtils.Audio.SPEAKER_MODE).Value,
            MaxChannels = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS).Value,
            MasterVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.MASTER_VOLUME).Value,
            MusicVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.MUSIC_VOLUME).Value,
            SoundEffectsVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME).Value,
            MusicOn = cvarSystem.GetCVarOrThrow<bool>(Core.Constants.CVars.EngineUtils.Audio.MUSIC_ON).Value,
            SoundEffectsOn = cvarSystem.GetCVarOrThrow<bool>(Core.Constants.CVars.EngineUtils.Audio.EFFECTS_ON).Value
        };

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected override AudioConfig CreateDefault() => new AudioConfig
        {
            AudioDriver = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.AUDIO_DRIVER).DefaultValue,
            OutputDeviceIndex = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.OUTPUT_DEVICE_INDEX).DefaultValue,
            SpeakerMode = cvarSystem.GetCVarOrThrow<SpeakerMode>(Core.Constants.CVars.EngineUtils.Audio.SPEAKER_MODE).DefaultValue,
            MaxChannels = cvarSystem.GetCVarOrThrow<int>(Core.Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS).DefaultValue,
            MasterVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.MASTER_VOLUME).DefaultValue,
            MusicVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.MUSIC_VOLUME).DefaultValue,
            SoundEffectsVolume = cvarSystem.GetCVarOrThrow<float>(Core.Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME).DefaultValue,
            MusicOn = cvarSystem.GetCVarOrThrow<bool>(Core.Constants.CVars.EngineUtils.Audio.MUSIC_ON).DefaultValue,
            SoundEffectsOn = cvarSystem.GetCVarOrThrow<bool>(Core.Constants.CVars.EngineUtils.Audio.EFFECTS_ON).DefaultValue
        };
    }
}
