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

using System.Collections.Generic;

namespace Nomad.Input.Private.ValueObjects
{
	internal class InputMapping
	{
		/// <summary>
		/// 
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// Which device this binding belongs to. Must be "Keyboard", "Mouse", or "Gamepad".
		/// </summary>
		public string? DeviceId { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public string? ButtonId { get; set; }

		/// <summary>
		/// The value a <see cref="InputEventData"/> value must match in order to trigger the bind.
		/// </summary>
		/// <remarks>
		/// The value 1.0f represents both a complete trigger pull/joystick movement as well as the button's
		/// pressed status.
		/// </remarks>
		public float Value { get; set; } = 0.0f;

		/// <summary>
		/// 
		/// </summary>
		public List<string>? Modifiers { get; set; }
	};
};
