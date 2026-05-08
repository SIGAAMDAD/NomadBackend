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

using Nomad.Input.ValueObjects;
using System;

namespace Nomad.Input.Private.ValueObjects
{
	internal readonly struct AxisLookupKey : IEquatable<AxisLookupKey>
	{
		/// <summary>
		/// 
		/// </summary>
		public InputDeviceSlot Device => _device;
		private readonly InputDeviceSlot _device;

		/// <summary>
		/// 
		/// </summary>
		public InputControlId Control => _control;
		private readonly InputControlId _control;

		public AxisLookupKey( InputDeviceSlot device, InputControlId control )
		{
			_device = device;
			_control = control;
		}

		public bool Equals( AxisLookupKey other )
		{
			return _device == other._device && _control == other._control;
		}

		public override bool Equals( object? obj )
		{
			return obj is AxisLookupKey other && Equals( other );
		}

		public override int GetHashCode()
		{
			return HashCode.Combine( (int)_device, (int)_control );
		}
	};
};
