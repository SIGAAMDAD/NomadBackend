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

using Nomad.Core.Events;

namespace Nomad.Core.Scene.GameObjects
{
    /// <summary>
    /// Represents a 3D sprite that can play frame-based animations.
    /// </summary>
    public interface IAnimatedSprite3D : ISprite3D
    {
        /// <summary>
        /// The active animation name.
        /// </summary>
        string Animation { get; set; }

        /// <summary>
        /// The current frame index.
        /// </summary>
        int Frame { get; set; }

        /// <summary>
        /// Playback speed multiplier.
        /// </summary>
        float SpeedScale { get; set; }

        /// <summary>
        /// Whether the sprite is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Fired when the current animation finishes.
        /// </summary>
        IGameEvent<EmptyEventArgs> AnimationFinished { get; }

        /// <summary>
        /// Fired when the current animation loops.
        /// </summary>
        IGameEvent<EmptyEventArgs> AnimationLooped { get; }

        /// <summary>
        /// Starts playback of the current animation.
        /// </summary>
        void Play();

        /// <summary>
        /// Pauses playback.
        /// </summary>
        void Pause();

        /// <summary>
        /// Stops playback.
        /// </summary>
        void Stop();
    }
}
