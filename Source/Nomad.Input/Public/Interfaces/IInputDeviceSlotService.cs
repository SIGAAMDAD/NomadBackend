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

using System;
using System.Collections.Generic;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Interfaces
{
	public interface IInputDeviceSlotService
	{
		const int MaxLocalSlots = 4;

		event Action<InputDeviceSlot, int>? DeviceSlotChanged;

		void AssignDevice( InputDeviceSlot deviceSlot, int localSlot );
		void AssignDeviceExclusive( InputDeviceSlot deviceSlot, int localSlot );
		bool UnassignDevice( InputDeviceSlot deviceSlot );
		void SwapLocalSlots( int firstLocalSlot, int secondLocalSlot );
		void ClearAssignments();
		void ResetToDefaultAssignments();

		bool TryGetLocalSlot( InputDeviceSlot deviceSlot, out int localSlot );
		bool IsAssignedToLocalSlot( InputDeviceSlot deviceSlot, int localSlot );
		IReadOnlyList<InputDeviceSlot> GetDevicesForLocalSlot( int localSlot );
	}
}
