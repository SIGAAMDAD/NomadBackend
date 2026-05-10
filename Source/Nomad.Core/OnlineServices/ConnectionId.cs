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

namespace Nomad.Core.OnlineServices
{
	public readonly struct ConnectionId : IEquatable<ConnectionId>
	{
		public static readonly ConnectionId Invalid = new ConnectionId( ushort.MaxValue );

		public readonly ushort Value;

		public bool IsValid { get { return Value != ushort.MaxValue; } }

		public ConnectionId( ushort value )
		{
			Value = value;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( ConnectionId other )
		{
			return Value == other.Value;
		}

		public override bool Equals( object? obj )
		{
			return obj is ConnectionId other && Equals( other );
		}

		public override int GetHashCode()
		{
			return Value;
		}
	}
}
