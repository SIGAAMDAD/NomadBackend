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
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Nomad.Save.Private.ValueObjects
{
	/*
	===================================================================================

	Checksum

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal readonly struct Checksum
	{
		/// <summary>
		///
		/// </summary>
		public static readonly Checksum Empty = default;

		/// <summary>
		///
		/// </summary>
		public readonly ulong Value { get; }

		/*
		===============
		Checksum
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		public Checksum( ulong value )
		{
			Value = value;
		}

		/*
		===============
		Compute
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static Checksum Compute( ReadOnlySpan<byte> buffer )
		{
			Crc64 checksum64 = new Crc64();
			checksum64.Append( buffer );
			return new Checksum( checksum64.GetCurrentHashAsUInt64() );
		}

		/*
		===============
		Equals
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override bool Equals( [NotNullWhen( true )] object? obj )
		{
			return obj is Checksum checksum && checksum.Value == Value;
		}

		/*
		===============
		GetHashCode
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/*
		===============
		operator ==
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool operator ==( Checksum a, Checksum b )
		{
			return a.Value == b.Value;
		}

		/*
		===============
		operator !=
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool operator !=( Checksum a, Checksum b )
		{
			return a.Value != b.Value;
		}
	};
};
