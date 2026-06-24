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
using Nomad.Core.Util;

namespace Nomad.Events.Private
{
	/*
	===================================================================================

	EventKey

	===================================================================================
	*/
	/// <summary>
	/// Represents a game event's unique identifier.
	/// </summary>

	internal readonly struct EventKey : IEquatable<EventKey>
	{
		private readonly int _hashCode;

		/*
		===============
		EventKey
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="nameSpace"></param>
		/// <param name="argsType"></param>
		public EventKey( InternString name, InternString nameSpace, Type argsType )
		{
			_hashCode = HashCode.Combine(
				nameSpace.GetHashCode(),
				name.GetHashCode(),
				argsType.TypeHandle.GetHashCode()
			);
		}

		/*
		===============
		Equals
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( EventKey other )
		{
			return _hashCode == other._hashCode;
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
			return _hashCode;
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
		public override bool Equals( object? obj )
		{
			return obj is EventKey key && Equals( key );
		}
	};
};
