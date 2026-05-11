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
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Nomad.Networking.Private.Messaging
{
	/*
	===================================================================================

	NetworkMessageEnvelope

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class NetworkMessageEnvelope
	{
		public const int HEADER_SIZE = sizeof( ushort );

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="destination"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void Write( ushort id, Span<byte> destination )
		{
			BinaryPrimitives.WriteUInt16LittleEndian( destination, id );
		}

		/*
		===============
		TryRead
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <param name="id"></param>
		/// <param name="payload"></param>
		/// <returns></returns>
		public static bool TryRead( ReadOnlySpan<byte> source, out ushort id, out ReadOnlySpan<byte> payload )
		{
			if ( source.Length < HEADER_SIZE ) {
				id = default;
				payload = default;
				return false;
			}

			id = BinaryPrimitives.ReadUInt16LittleEndian( source );
			payload = source.Slice( HEADER_SIZE );
			return true;
		}
	};
};
