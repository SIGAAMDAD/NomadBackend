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

using System;
using System.Collections.Generic;
using Godot;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Interfaces
{
    /// <summary>
    ///
    /// </summary>
    public interface IAudioDevice : IDisposable
    {
        IEnumerable<string> GetAudioDrivers();

        void Update(float deltaTime);

        AudioResult LoadBank(string bankPath, out BankHandle bank);
        AudioResult UnloadBank(BankHandle bank);

        AudioResult CreateEvent(string assetPath, out EventHandle eventHandle);
        AudioResult TriggerEvent(EventHandle eventHandle, out ChannelHandle channel);
        AudioResult SetChannelVolume(ChannelHandle channel, float volume);
        AudioResult SetChannelPitch(ChannelHandle channel, float pitch);
        AudioResult GetChannelStatus(ChannelHandle channel, out ChannelStatus status);
        AudioResult StopChannel(ChannelHandle channel);

        AudioResult SetParameterValue(EventHandle eventHandle, string parameterName, float value);

        AudioResult GetChannelGroup(string groupname, out ChannelHandle group);
        AudioResult StopChannelGroup(ChannelHandle group);
        AudioResult SetChannelGroupVolume(ChannelHandle group, float value);
        AudioResult SetChannelGroupPitch(ChannelHandle group, float pitch);

        AudioResult SetListenerPosition(int listenerIndex, Vector2 position);
    }
}
