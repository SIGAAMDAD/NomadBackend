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
using Godot;
using Nomad.Core.Abstractions;

namespace Nomad.Audio.Fmod.Entities
{
    /*
	===================================================================================

	FMODChannelResource

	===================================================================================
	*/
    /// <summary>
    ///
    /// </summary>

    internal readonly record struct FMODChannelResource(FMOD.Studio.EventInstance instance) : IDisposable, IValueObject<FMODChannelResource>
    {
        public FMOD.Studio.PLAYBACK_STATE PlaybackState
        {
            get
            {
                FMODValidator.ValidateCall(instance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state));
                return state;
            }
        }
        public uint ListenerMask
        {
            get
            {
                FMODValidator.ValidateCall(instance.getListenerMask(out uint mask));
                return mask;
            }
            set
            {
                FMODValidator.ValidateCall(instance.setListenerMask(value));
            }
        }
        public Vector2 Position
        {
            get
            {
                FMODValidator.ValidateCall(instance.get3DAttributes(out FMOD.ATTRIBUTES_3D attributes));
                return new Vector2() { X = attributes.position.x, Y = attributes.position.y };
            }
            set
            {
                FMODValidator.ValidateCall(instance.set3DAttributes(
                    new FMOD.ATTRIBUTES_3D
                    {
                        position = new FMOD.VECTOR
                        {
                            x = value.X,
                            y = value.Y,
                            z = 1.0f,
                        }
                    }
                ));
            }
        }
        public float Volume
        {
            get
            {
                FMODValidator.ValidateCall(instance.getVolume(out float volume));
                return volume;
            }
            set
            {
                FMODValidator.ValidateCall(instance.setVolume(value));
            }
        }
        public float Pitch
        {
            get
            {
                FMODValidator.ValidateCall(instance.getPitch(out float pitch));
                return pitch;
            }
            set
            {
                FMODValidator.ValidateCall(instance.setPitch(value));
            }
        }

        public FMOD.Studio.MEMORY_USAGE MemoryUsage
        {
            get
            {
                FMODValidator.ValidateCall(instance.getMemoryUsage(out FMOD.Studio.MEMORY_USAGE memoryUsage));
                return memoryUsage;
            }
        }
        public FMOD.Studio.EventDescription Description
        {
            get
            {
                FMODValidator.ValidateCall(instance.getDescription(out FMOD.Studio.EventDescription description));
                return description;
            }
        }
        public bool IsPlaying => PlaybackState == FMOD.Studio.PLAYBACK_STATE.PLAYING;

        /*
		===============
		Dispose
		===============
		*/
        /// <summary>
        /// Clears the unmanaged FMOD EventInstance.
        /// </summary>
        public void Dispose()
        {
            if (instance.isValid())
            {
                FMODValidator.ValidateCall(instance.release());
                instance.clearHandle();
            }
        }
    };
};
