/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

namespace NomadCore.Domain
{
    public static class Constants
    {
        public static class CVars
        {
            public static class Audio
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
                    public const string LOGGING = "audio.fmod.LoggingEnabled";
                    public const string BANK_LOADING_STRATEGY = "audio.fmod.BankLoadingStrategy";
                };
            };
            public static class Graphics
            {
            };
            public static class Accessibility
            {
                public const string HAPTIC_STRENGTH = "input.HapticStrength";
                public const string HAPTIC_ENABLED = "input.HapticEnabled";
                public const string COLORBLIND_MODE = "accessibility.ColorblindMode";
                public const string DYSLEXIA_MODE = "accessibility.DyslexiaMode";
                public const string UI_SCALE = "accessibility.UIScale";
                public const string AUTO_AIM_MODE = "accessibility.AutoAimMode";
                public const string TEXT_TO_SPEECH = "accessibility.TextToSpeech";
            };
            public static class Console
            {
                public const string DEFAULT_CONFIG_FILE = "console.DefaultConfig";
                public const string CONSOLE_LOG_LEVEL = "console.LogLevel";
            };
        };
        public static class Events
        {
            public static class Console
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
            };
        };
        public static class Audio
        {
            public const int MAX_AUDIO_CHANNELS = 512;
            public const int MIN_AUDIO_CHANNELS = 64;
        };
    };
};