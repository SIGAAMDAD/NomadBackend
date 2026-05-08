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
	internal static class MouseButtonExtensions
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
		public static InputControlId ToControlId( this MouseButton button ) => button switch {
			MouseButton.Left => InputControlId.Left,
			MouseButton.Right => InputControlId.Right,
			MouseButton.Middle => InputControlId.Middle,
			MouseButton.WheelDown => InputControlId.WheelDown,
			MouseButton.WheelUp => InputControlId.WheelUp,
			MouseButton.X1 => InputControlId.X1,
			MouseButton.X2 => InputControlId.X2,
			_ => throw new ArgumentOutOfRangeException( nameof( button ) )
		};
	};
};
