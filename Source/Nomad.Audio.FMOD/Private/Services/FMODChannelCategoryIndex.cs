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
	/// Per-category intrusive slot lists. The link arrays live in <see cref="FMODChannelStorage"/>
	/// so category-limit scans do not allocate or chase managed objects.
	/// </summary>
	internal unsafe sealed class FMODChannelCategoryIndex
	{
		private readonly FMODChannelStorage _storage;
		private int[] _headByCategoryId;

		public FMODChannelCategoryIndex( FMODChannelStorage storage, int initialCategoryCapacity = 16 )
		{
			_storage = storage;
			_headByCategoryId = new int[Math.Max( 1, initialCategoryCapacity )];
			Array.Fill( _headByCategoryId, FMODChannelConstants.InvalidIndex );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Link( int slot, ushort categoryId )
		{
			EnsureCategoryCapacity( categoryId + 1 );

			int head = _headByCategoryId[categoryId];
			_storage.SlotPrevInCategory[slot] = FMODChannelConstants.InvalidIndex;
			_storage.SlotNextInCategory[slot] = head;

			if ( head != FMODChannelConstants.InvalidIndex ) {
				_storage.SlotPrevInCategory[head] = slot;
			}

			_headByCategoryId[categoryId] = slot;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Unlink( int slot, ushort categoryId )
		{
			int prev = _storage.SlotPrevInCategory[slot];
			int next = _storage.SlotNextInCategory[slot];

			if ( prev != FMODChannelConstants.InvalidIndex ) {
				_storage.SlotNextInCategory[prev] = next;
			} else {
				_headByCategoryId[categoryId] = next;
			}

			if ( next != FMODChannelConstants.InvalidIndex ) {
				_storage.SlotPrevInCategory[next] = prev;
			}

			_storage.SlotPrevInCategory[slot] = FMODChannelConstants.InvalidIndex;
			_storage.SlotNextInCategory[slot] = FMODChannelConstants.InvalidIndex;
		}

		public int FindLowestPrioritySlot( ushort categoryId )
		{
			if ( (uint)categoryId >= (uint)_headByCategoryId.Length ) {
				return FMODChannelConstants.InvalidIndex;
			}

			int slot = _headByCategoryId[categoryId];
			int bestSlot = FMODChannelConstants.InvalidIndex;
			float lowestPriority = float.MaxValue;

			while ( slot != FMODChannelConstants.InvalidIndex ) {
				int next = _storage.SlotNextInCategory[slot];
				int dense = _storage.SlotToDense[slot];

				if ( dense != FMODChannelConstants.InvalidIndex &&
					(_storage.Flags[dense] & FMODChannelConstants.EssentialFlag) == 0 &&
					FMODChannelInstanceController.IsPlaying( _storage.InstancePtr[dense] ) &&
					_storage.CurrentPriority[dense] < lowestPriority ) {
					lowestPriority = _storage.CurrentPriority[dense];
					bestSlot = slot;
				}

				slot = next;
			}

			return bestSlot;
		}

		private void EnsureCategoryCapacity( int count )
		{
			if ( count <= _headByCategoryId.Length ) {
				return;
			}

			int oldLength = _headByCategoryId.Length;
			Array.Resize( ref _headByCategoryId, NextPowerOfTwo( count ) );
			for ( int i = oldLength; i < _headByCategoryId.Length; i++ ) {
				_headByCategoryId[i] = FMODChannelConstants.InvalidIndex;
			}
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
