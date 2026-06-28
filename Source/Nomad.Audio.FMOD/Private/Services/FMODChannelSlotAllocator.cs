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
using Nomad.Audio.Fmod.Private.Entities;

namespace Nomad.Audio.Fmod.Private.Services
{
	/// <summary>
	/// Owns channel slot lifetime and handle generation. Dense packing is still owned by
	/// <see cref="FMODChannelService"/> because stealing can reuse a stopped slot immediately.
	/// </summary>
	internal unsafe sealed class FMODChannelSlotAllocator
	{
		private readonly FMODChannelStorage _storage;
		private int _freeTop;

		public FMODChannelSlotAllocator( FMODChannelStorage storage )
		{
			_storage = storage;
			Initialize();
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryAcquire( out int slot )
		{
			if ( _freeTop <= 0 ) {
				slot = FMODChannelConstants.InvalidIndex;
				return false;
			}

			slot = _storage.FreeSlots[--_freeTop];
			return true;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Release( int slot )
		{
			_storage.FreeSlots[_freeTop++] = slot;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public FMODChannelHandle CreateHandle( int slot )
		{
			uint generation = ++_storage.Generation[slot];
			return new FMODChannelHandle( slot, generation );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool IsAlive( FMODChannelHandle handle )
		{
			int slot = handle.Slot;
			return handle.IsValid &&
				(uint)slot < (uint)_storage.Capacity &&
				_storage.Generation[slot] == handle.Generation &&
				_storage.SlotToDense[slot] != FMODChannelConstants.InvalidIndex;
		}

		private void Initialize()
		{
			new Span<int>( _storage.SlotToDense, _storage.Capacity ).Fill( FMODChannelConstants.InvalidIndex );
			new Span<int>( _storage.SlotNextInCategory, _storage.Capacity ).Fill( FMODChannelConstants.InvalidIndex );
			new Span<int>( _storage.SlotPrevInCategory, _storage.Capacity ).Fill( FMODChannelConstants.InvalidIndex );

			for ( int i = 0; i < _storage.Capacity; i++ ) {
				_storage.FreeSlots[i] = _storage.Capacity - 1 - i;
			}

			_freeTop = _storage.Capacity;
		}
	}
}
