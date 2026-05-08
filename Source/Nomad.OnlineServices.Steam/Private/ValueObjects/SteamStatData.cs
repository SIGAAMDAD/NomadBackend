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

using System.Runtime.InteropServices;
using Nomad.Core.Util;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	/*
	===================================================================================
	
	SteamStatData
	
	===================================================================================
	*/
	/// <summary>
	/// Stores stat data for a steam value.
	/// </summary>

	internal record SteamStatData
	{
		[StructLayout( LayoutKind.Explicit, Pack = 1, Size = 4 )]
		public struct Data
		{
			[FieldOffset( 0 )] public float FloatValue;
			[FieldOffset( 0 )] public int IntValue;
		};

		public InternString Name { get; init; }
		public Data Value { get; set; }
		public bool IsFloat { get; init; }
		public bool IsDirty { get; set; }
	};
};
