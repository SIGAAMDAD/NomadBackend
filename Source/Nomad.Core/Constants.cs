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

namespace Nomad.Core
{
    public static class Constants
    {
        public static class CVars
        {
            public static partial class Audio
            {
                public const string EFFECTS_VOLUME = "audio.EffectsVolume";
                public const string EFFECTS_ON = "audio.EffectsOn";

                public const string MUSIC_VOLUME = "audio.MusicVolume";
                public const string MUSIC_ON = "audio.MusicOn";

                public const string MAX_CHANNELS = "audio.MaxChannels";
                public const string MAX_ACTIVE_CHANNELS = "audio.MaxActiveChannels";

                public const string DISTANCE_FALLOFF_START = "audio.DistanceFalloffStart";
                public const string DISTANCE_FALLOFF_END = "audio.DistanceFalloffEnd";

                public const string MIN_TIME_BETWEEN_CHANNEL_STEALS = "audio.MinTimeBetweenChannelSteals";
                public const string FREQUENCY_PENALTY = "audio.FrequencyPenalty";
                public const string VOLUME_PENALTY = "audio.VolumePenalty";
                public const string DISTANCE_WEIGHT = "audio.DistanceWeight";

                public const string SPEAKER_MODE = "audio.SpeakerMode";
                public const string OUTPUT_DEVICE_INDEX = "audio.OutputDeviceIndex";
                public const string AUDIO_DRIVER = "audio.AudioDriver";

                public static class FMOD
                {
                    public const string STREAM_BUFFER_SIZE = "audio.fmod.StreamBufferSize";
                    public const string DSP_BUFFER_SIZE = "audio.fmod.DSPBufferSize";
                    public const string DSP_BUFFER_COUNT = "audio.fmod.DSPBufferCount";
                    public const string LOGGING = "audio.fmod.LoggingEnabled";
                    public const string BANK_LOADING_STRATEGY = "audio.fmod.BankLoadingStrategy";
                }
            }
            public static partial class Graphics
            {
                public const string SHADOW_FILTER_TYPE = "r.ShadowFilterType";
                public const string SHADOW_FILTER_SMOOTH = "r.ShadowFilterSmooth";
                public const string FORCE_VERTEX_SHADING = "r.ForceVertexShading";
                public const string PHYSICALLY_BASED_RENDERING = "r.PhysicallyBasedRendering";
            }
            public static partial class Display
            {
                public const string WINDOW_MODE = "display.WindowMode";
                public const string WINDOW_RESOLUTION = "display.WindowResolution";
                public const string ANTI_ALIASING = "display.AntiAliasing";
                public const string MAX_FPS = "display.MaxFps";
                public const string SEPARATE_RENDERING_THREAD = "r.SeparateRenderingThread";
                public const string VSYNC_MODE = "display.VSyncMode";
            }
            public static partial class Accessibility
            {
                public const string HAPTIC_STRENGTH = "input.HapticStrength";
                public const string HAPTIC_ENABLED = "input.HapticEnabled";
                public const string COLORBLIND_MODE = "accessibility.ColorblindMode";
                public const string DYSLEXIA_MODE = "accessibility.DyslexiaMode";
                public const string UI_SCALE = "accessibility.UIScale";
                public const string AUTO_AIM_MODE = "accessibility.AutoAimMode";
                public const string TEXT_TO_SPEECH = "accessibility.TextToSpeech";
            }
            public static partial class Console
            {
                public const string DEFAULT_CONFIG_FILE = "console.DefaultConfig";
                public const string CONSOLE_LOG_LEVEL = "console.LogLevel";
            }
        }
        public static class Events
        {
            public static partial class Console
            {
                public const string CONSOLE_OPENED_EVENT = "ConsoleSystem:ConsoleOpened";
                public const string CONSOLE_CLOSED_EVENT = "ConsoleSystem:ConsoleClosed";
                public const string TEXT_ENTERED_EVENT = "ConsoleSystem:TextEntered";
                public const string HISTORY_PREV_EVENT = "ConsoleSystem:HistoryPrev";
                public const string HISTORY_NEXT_EVENT = "ConsoleSystem:HistoryNext";
                public const string AUTOCOMPLETE_EVENT = "ConsoleSystem:AutoComplete";
                public const string PAGE_UP_EVENT = "ConsoleSystem:PageUp";
                public const string PAGE_DOWN_EVENT = "ConsoleSystem:PageDown";
                public const string UNKNOWN_COMMAND_EVENT = "ConsoleSystem:UnknownCommand";
                public const string COMMAND_EXECUTED_EVENT = "ConsoleSystem:CommandExecuted";
            }
            public static partial class ResourceCache
            {
                public const string RESOURCE_LOADED_EVENT = "ResourceCache:ResourceLoaded";
                public const string RESOURCE_LOAD_FAILED_EVENT = "ResourceCache:ResourceLoadFailed";
                public const string RESOURCE_LOAD_PROGRESS_EVENT = "ResourceCache:ResourceLoadProgress";
                public const string RESOURCE_UNLOADED_EVENT = "ResourceCache:ResourceUnloaded";
            }
            public static partial class CVars
            {
                public const string CVAR_VALUE_CHANGED_EVENT = "CVarSystem:CVarValueChanged";
            }
        }
        public static class Audio
        {
            public const int MAX_AUDIO_CHANNELS = 512;
            public const int MIN_AUDIO_CHANNELS = 64;
        }
    }
}
