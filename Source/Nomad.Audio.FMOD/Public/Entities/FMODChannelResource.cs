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
using System.Runtime.CompilerServices;
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

    internal readonly record struct FMODChannelResource : IDisposable, IValueObject<FMODChannelResource>
    {
        private readonly FMOD.Studio.EventInstance _instance;

        public FMODChannelResource(FMOD.Studio.EventInstance instance)
        {
            _instance = instance;
        }

        /*
		===============
		Dispose
		===============
		*/
        public void Dispose()
        {
            if (_instance.isValid())
            {
                _instance.release();
                _instance.clearHandle();
            }
        }

        /*
		===============
		GetPlaybackState
		===============
		*/
        public FMOD.Studio.PLAYBACK_STATE GetPlaybackState()
        {
            if (!_instance.isValid())
            {
                return FMOD.Studio.PLAYBACK_STATE.STOPPED;
            }
            FMODValidator.ValidateCall(_instance.getPlaybackState(out var state));
            return state;
        }

        /*
		===============
		SetPosition
		===============
		*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector2 position)
        {
            _instance.set3DAttributes(new FMOD.ATTRIBUTES_3D { position = new FMOD.VECTOR { x = position.X, y = position.Y, z = 0.0f } });
        }

        /*
		===============
		SetVolume
		===============
		*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVolume(float volume)
        {
            _instance.setVolume(volume);
        }
    };
};
