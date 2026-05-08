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
using Nomad.Core.Input.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Extensions
{
	/*
	===================================================================================
	
	KeyNumExtensions
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal static class KeyNumExtensions
	{
		/*
		===============
		ToControlId
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyNum"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static InputControlId ToControlId( this KeyNum keyNum )
		{
			return (byte)keyNum < (byte)KeyNum.Count ? (InputControlId)((byte)keyNum + 1) : throw new ArgumentOutOfRangeException( nameof( keyNum ) );
		}
	};
};
