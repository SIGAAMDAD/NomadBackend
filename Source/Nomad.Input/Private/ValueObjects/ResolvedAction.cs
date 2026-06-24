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
using System.Runtime.CompilerServices;
using Nomad.Core.Util;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.ValueObjects
{
	internal readonly struct ResolvedAction
	{
		public readonly InternString ActionId;
		public readonly int ActionIndex;
		public readonly long TimeStamp;
		public readonly InputDeviceSlot DeviceSlot;
		public readonly int LocalSlot;

		public readonly Vector2 Vector2Value;
		public readonly float FloatValue;

		public readonly InputValueType ValueType;
		public readonly InputActionPhase Phase;
		public readonly bool ButtonValue;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ResolvedAction(
			InternString actionId,
			int actionIndex,
			InputValueType valueType,
			InputActionPhase phase,
			long timeStamp,
			InputDeviceSlot deviceSlot = InputDeviceSlot.Keyboard,
			int localSlot = 0,
			bool buttonValue = default,
			float floatValue = default,
			Vector2 vector2Value = default
		)
		{
			ActionId = actionId;
			ActionIndex = actionIndex;
			TimeStamp = timeStamp;
			DeviceSlot = deviceSlot;
			LocalSlot = localSlot;
			Vector2Value = vector2Value;
			FloatValue = floatValue;
			ValueType = valueType;
			Phase = phase;
			ButtonValue = buttonValue;
		}
	}
}
