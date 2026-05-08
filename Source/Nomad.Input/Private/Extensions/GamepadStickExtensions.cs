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
	internal static class GamepadStickExtensions
	{
		/*
		===============
		ToControlId
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stick"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static InputControlId ToControlId( this GamepadStick stick ) => stick switch {
			GamepadStick.Left => InputControlId.LeftStick,
			GamepadStick.Right => InputControlId.RightStick,
			_ => throw new ArgumentOutOfRangeException( nameof( stick ) )
		};
	};
};
