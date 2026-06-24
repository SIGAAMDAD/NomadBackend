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

using System.Numerics;
using Nomad.Core.Events;
using Nomad.Core.Input;
using Nomad.Core.Input.ValueObjects;

namespace Nomad.Core.Engine.Services
{
    /// <summary>
    ///
    /// </summary>
    public interface IInputAdapter
    {
        /// <summary>
        ///
        /// </summary>
        [Event("Nomad.Core.Input")]
        [EventPayload("Stick", typeof(GamepadStick), Order = 1)]
        [EventPayload("TimeStamp", typeof(long), Order = 2)]
        [EventPayload("DeviceId", typeof(int), Order = 3)]
        [EventPayload("Value", typeof(Vector2), Order = 4)]
        IGameEvent<GamepadAxisEventArgs> GamepadAxis { get; }

        /// <summary>
        ///
        /// </summary>
        [Event("Nomad.Core.Input")]
        [EventPayload("Button", typeof(GamepadButton), Order = 1)]
        [EventPayload("TimeStamp", typeof(long), Order = 2)]
        [EventPayload("DeviceId", typeof(int), Order = 3)]
        [EventPayload("Pressed", typeof(bool), Order = 4)]
        IGameEvent<GamepadButtonEventArgs> GamepadButton { get; }

        /// <summary>
        ///
        /// </summary>
        [Event("Nomad.Core.Input")]
        [EventPayload("KeyNum", typeof(KeyNum), Order = 1)]
        [EventPayload("TimeStamp", typeof(long), Order = 2)]
        [EventPayload("Pressed", typeof(bool), Order = 3)]
        IGameEvent<KeyboardEventArgs> Keyboard { get; }

        /// <summary>
        ///
        /// </summary>
        [Event("Nomad.Core.Input")]
        [EventPayload("Button", typeof(MouseButton), Order = 1)]
        [EventPayload("TimeStamp", typeof(long), Order = 2)]
        [EventPayload("Pressed", typeof(bool), Order = 3)]
        IGameEvent<MouseButtonEventArgs> MouseButton { get; }

        /// <summary>
        ///
        /// </summary>
        [Event("Nomad.Core.Input")]
        [EventPayload("TimeStamp", typeof(long), Order = 1)]
        [EventPayload("RelativeX", typeof(float), Order = 2)]
        [EventPayload("RelativeY", typeof(float), Order = 3)]
        IGameEvent<MouseMotionEventArgs> MouseMotion { get; }

        /// <summary>
        ///
        /// </summary>
        [Event("Nomad.Core.Input")]
        [EventPayload("TimeStamp", typeof(long), Order = 1)]
        [EventPayload("PositionX", typeof(int), Order = 2)]
        [EventPayload("PositionY", typeof(int), Order = 3)]
        IGameEvent<MousePositionChangedEventArgs> MousePositionChanged { get; }
    }
}
