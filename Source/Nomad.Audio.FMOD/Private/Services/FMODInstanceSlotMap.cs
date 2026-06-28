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
namespace Nomad.Audio.Fmod.Private.Services
{
	/// <summary>
	/// Hot-path open-addressed map from native FMOD event instance handles to channel slots.
	/// </summary>
	internal sealed class FMODInstanceSlotMap
	{
		private nint[] _keys;
		private int[] _values;
		private byte[] _state; // 0 empty, 1 occupied, 2 tombstone
		private int _count;
		private int _mask;

		public FMODInstanceSlotMap( int capacity )
		{
			int size = NextPowerOfTwo( Math.Max( 8, capacity ) );
			_keys = new nint[size];
			_values = new int[size];
			_state = new byte[size];
			_mask = size - 1;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Set( nint key, int value )
		{
			if ( (_count + 1) * 2 >= _keys.Length ) {
				Resize( _keys.Length << 1 );
			}

			InsertOrUpdate( key, value );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryGet( nint key, out int value )
		{
			int index = FindIndex( key );
			if ( index < 0 ) {
				value = FMODChannelConstants.InvalidIndex;
				return false;
			}

			value = _values[index];
			return true;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Remove( nint key )
		{
			int index = FindIndex( key );
			if ( index < 0 ) {
				return;
			}

			_state[index] = 2;
			_keys[index] = 0;
			_values[index] = 0;
			_count--;
		}

		private void InsertOrUpdate( nint key, int value )
		{
			int index = Hash( key ) & _mask;
			int firstTombstone = FMODChannelConstants.InvalidIndex;

			while ( true ) {
				byte state = _state[index];
				if ( state == 0 ) {
					int target = firstTombstone >= 0 ? firstTombstone : index;
					_state[target] = 1;
					_keys[target] = key;
					_values[target] = value;
					_count++;
					return;
				}

				if ( state == 2 ) {
					if ( firstTombstone < 0 ) {
						firstTombstone = index;
					}
				} else if ( _keys[index] == key ) {
					_values[index] = value;
					return;
				}

				index = (index + 1) & _mask;
			}
		}

		private int FindIndex( nint key )
		{
			int index = Hash( key ) & _mask;

			while ( true ) {
				byte state = _state[index];
				if ( state == 0 ) {
					return FMODChannelConstants.InvalidIndex;
				}

				if ( state == 1 && _keys[index] == key ) {
					return index;
				}

				index = (index + 1) & _mask;
			}
		}

		private void Resize( int newSize )
		{
			nint[] oldKeys = _keys;
			int[] oldValues = _values;
			byte[] oldState = _state;

			_keys = new nint[newSize];
			_values = new int[newSize];
			_state = new byte[newSize];
			_mask = newSize - 1;
			_count = 0;

			for ( int i = 0; i < oldKeys.Length; i++ ) {
				if ( oldState[i] == 1 ) {
					InsertOrUpdate( oldKeys[i], oldValues[i] );
				}
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static int Hash( nint value )
		{
			return value.GetHashCode();
		}

		private static int NextPowerOfTwo( int value )
		{
			if ( value <= 1 ) {
				return 1;
			}

			value--;
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			value++;
			return value > 0 ? value : 1 << 30;
		}
	}
}
