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
using System.Runtime.CompilerServices;
using Nomad.Core.Input;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Extensions
{
	internal static class GamepadButtonExtensions
	{
		/*
		===============
		ToControlId
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="button"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static InputControlId ToControlId( this GamepadButton button ) => button switch {
			GamepadButton.A => InputControlId.GamepadA,
			GamepadButton.B => InputControlId.GamepadB,
			GamepadButton.X => InputControlId.GamepadX,
			GamepadButton.Y => InputControlId.GamepadY,
			GamepadButton.Back => InputControlId.Back,
			GamepadButton.Guide => InputControlId.Guide,
			GamepadButton.Start => InputControlId.Start,
			GamepadButton.LeftStick => InputControlId.LeftStickButton,
			GamepadButton.RightStick => InputControlId.RightStickButton,
			GamepadButton.LeftShoulder => InputControlId.LeftShoulder,
			GamepadButton.RightShoulder => InputControlId.RightShoulder,
			GamepadButton.DPadUp => InputControlId.DPadUp,
			GamepadButton.DPadDown => InputControlId.DPadDown,
			GamepadButton.DPadLeft => InputControlId.DPadLeft,
			GamepadButton.DPadRight => InputControlId.DPadRight,
			_ => throw new ArgumentOutOfRangeException( nameof( button ) )
		};
	};
};
