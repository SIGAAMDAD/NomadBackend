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

using Godot;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Entities
{
    /*
	===================================================================================

	FMODEmitter

	===================================================================================
	*/
    /// <summary>
    /// A source of audio, an "emitter" in a sense.
    /// </summary>

    internal sealed class FMODEmitter(FMODChannelRepository channelRepository, string category) : IAudioEmitter
    {
        public Vector2 Positon
        {
            get => _position;
            set
            {
                if (_position == value)
                {
                    return;
                }
                _position = value;
                _channel?.SetPosition(value);
            }
        }
        private Vector2 _position = Vector2.Zero;

        public float Volume
        {
            get => _channel != null ? _channel.Volume : 0.0f;
            set
            {
                if (_channel == null)
                {
                    return;
                }
                _channel.Volume = value;
            }
        }

        public float Pitch
        {
            get => _channel != null ? _channel.Pitch : 0.0f;
            set
            {
                if (_channel == null)
                {
                    return;
                }
                _channel.Pitch = value;
            }
        }

        public ChannelGroupHandle Group
        {
            get => _channel != null ? _channel.Group : new(0);
            set
            {
                if (_channel == null)
                {
                    return;
                }
                _channel.Group = value;
            }
        }

        public ChannelStatus Status => _status;
        private ChannelStatus _status = ChannelStatus.Stopped;

        private FMODChannel? _channel;

        /// <summary>
        ///
        /// </summary>
        /// <param name="soundPath"></param>
        /// <param name="priority"></param>
        public void PlaySound(string soundPath, float priority = 0.5f)
        {
            _channel = channelRepository.AllocateChannel(new(new(soundPath)), _position, category, priority, false);
        }
    };
};
