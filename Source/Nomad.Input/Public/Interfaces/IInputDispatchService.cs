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
using Nomad.Core.Util;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Interfaces
{
    public interface IInputDispatchService
    {
        [Event("Nomad.Input")]
        [EventPayload("ActionId", typeof(InternString), Order = 1)]
        [EventPayload("Phase", typeof(InputActionPhase), Order = 2)]
        [EventPayload("Value", typeof(bool), Order = 3)]
        [EventPayload("TimeStamp", typeof(long), Order = 4)]
        [EventPayload("DeviceSlot", typeof(InputDeviceSlot), Order = 5)]
        [EventPayload("LocalSlot", typeof(int), Order = 6)]
        IGameEvent<ButtonActionEventArgs> ButtonAction { get; }

        [Event("Nomad.Input")]
        [EventPayload("ActionId", typeof(InternString), Order = 1)]
        [EventPayload("Phase", typeof(InputActionPhase), Order = 2)]
        [EventPayload("Value", typeof(float), Order = 3)]
        [EventPayload("TimeStamp", typeof(long), Order = 4)]
        [EventPayload("DeviceSlot", typeof(InputDeviceSlot), Order = 5)]
        [EventPayload("LocalSlot", typeof(int), Order = 6)]
        IGameEvent<FloatActionEventArgs> FloatAction { get; }

        [Event("Nomad.Input")]
        [EventPayload("ActionId", typeof(InternString), Order = 1)]
        [EventPayload("Phase", typeof(InputActionPhase), Order = 2)]
        [EventPayload("Value", typeof(Vector2), Order = 3)]
        [EventPayload("TimeStamp", typeof(long), Order = 4)]
        [EventPayload("DeviceSlot", typeof(InputDeviceSlot), Order = 5)]
        [EventPayload("LocalSlot", typeof(int), Order = 6)]
        IGameEvent<AxisActionEventArgs> AxisAction { get; }
    }
}
