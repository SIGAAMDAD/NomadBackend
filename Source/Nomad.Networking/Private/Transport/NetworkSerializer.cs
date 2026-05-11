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
using System.Runtime.InteropServices;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Private.Transport
{
	/*
	===================================================================================

	NetworkSerializer

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class NetworkSerializer : INetworkSerializer
	{
		/*
		===============
		GetMaxSize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public int GetMaxSize<T>()
			where T : struct
		{
			return Marshal.SizeOf<T>();
		}

		/*
		===============
		Serialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <param name="destination"></param>
		/// <param name="bytesWritten"></param>
		/// <returns></returns>
		public bool Serialize<T>( in T value, Span<byte> destination, out int bytesWritten )
			where T : struct
		{
			int size = GetMaxSize<T>();
			if ( destination.Length < size ) {
				bytesWritten = 0;
				return false;
			}

			T copy = value;
			MemoryMarshal.Write( destination, ref copy );
			bytesWritten = size;
			return true;
		}

		/*
		===============
		Deserialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool Deserialize<T>( ReadOnlySpan<byte> source, out T value )
			where T : struct
		{
			int size = GetMaxSize<T>();
			if ( source.Length < size ) {
				value = default;
				return false;
			}

			value = MemoryMarshal.Read<T>( source );
			return true;
		}
	};
};
