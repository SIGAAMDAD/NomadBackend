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
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Core.Collections;

namespace Nomad.Audio.Fmod.Private.Services
{
	/// <summary>
	/// Assigns compact numeric IDs to event/category names and keeps cold metadata out of
	/// the dense channel arrays. Event steal decay uses <see cref="DirtySet"/> so update only
	/// touches event IDs that were actually stolen.
	/// </summary>
	internal sealed class FMODChannelRegistry
	{
		private readonly StringIdTable _eventIds;
		private readonly StringIdTable _categoryIds;
		private readonly DirtySet _dirtyStealEvents;

		private float[] _lastPlayTimeByEventId;
		private ushort[] _consecutiveStealCountByEventId;

		private ushort[] _activeCountByCategoryId;
		private ushort[] _maxSimultaneousByCategoryId;
		private float[] _priorityScaleByCategoryId;
		private float[] _stealProtectionByCategoryId;
		private byte[] _allowStealFromSameCategoryById;

		private bool _shouldDecayStealCounts;

		public int EventCount {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _eventIds.Count;
		}

		public int CategoryCount {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _categoryIds.Count;
		}

		public FMODChannelRegistry( int initialEventCapacity = 16, int initialCategoryCapacity = 16 )
		{
			_eventIds = new StringIdTable( initialEventCapacity, StringComparer.Ordinal );
			_categoryIds = new StringIdTable( initialCategoryCapacity, StringComparer.Ordinal );
			_dirtyStealEvents = new DirtySet( initialEventCapacity, initialEventCapacity );

			_lastPlayTimeByEventId = new float[initialEventCapacity];
			_consecutiveStealCountByEventId = new ushort[initialEventCapacity];

			_activeCountByCategoryId = new ushort[initialCategoryCapacity];
			_maxSimultaneousByCategoryId = new ushort[initialCategoryCapacity];
			_priorityScaleByCategoryId = new float[initialCategoryCapacity];
			_stealProtectionByCategoryId = new float[initialCategoryCapacity];
			_allowStealFromSameCategoryById = new byte[initialCategoryCapacity];
		}

		public ushort GetOrCreateEventId( string eventName )
		{
			if ( _eventIds.TryGetId( eventName, out int existing ) ) {
				return ToUShortId( existing );
			}

			int id = _eventIds.GetOrAdd( eventName );
			EnsureEventCapacity( id + 1 );
			return ToUShortId( id );
		}

		public ushort GetOrCreateCategoryId( SoundCategory category )
		{
			string categoryName = category.Config.Name;
			if ( _categoryIds.TryGetId( categoryName, out int existing ) ) {
				return ToUShortId( existing );
			}

			int id = _categoryIds.GetOrAdd( categoryName );
			EnsureCategoryCapacity( id + 1 );

			_maxSimultaneousByCategoryId[id] = (ushort)Math.Clamp( category.Config.MaxSimultaneous, 0, ushort.MaxValue );
			_priorityScaleByCategoryId[id] = category.Config.PriorityScale;
			_stealProtectionByCategoryId[id] = category.Config.StealProtectionTime;
			_allowStealFromSameCategoryById[id] = category.Config.AllowStealingFromSameCategory ? (byte)1 : (byte)0;

			return ToUShortId( id );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public string GetEventName( ushort eventId )
		{
			return _eventIds.GetString( eventId );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ushort GetActiveCount( ushort categoryId )
		{
			return _activeCountByCategoryId[categoryId];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void IncrementActiveCount( ushort categoryId )
		{
			_activeCountByCategoryId[categoryId]++;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void DecrementActiveCount( ushort categoryId )
		{
			if ( _activeCountByCategoryId[categoryId] > 0 ) {
				_activeCountByCategoryId[categoryId]--;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ushort GetMaxSimultaneous( ushort categoryId )
		{
			return _maxSimultaneousByCategoryId[categoryId];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetPriorityScale( ushort categoryId )
		{
			return _priorityScaleByCategoryId[categoryId];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetStealProtection( ushort categoryId )
		{
			return _stealProtectionByCategoryId[categoryId];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool AllowsStealFromSameCategory( ushort categoryId )
		{
			return _allowStealFromSameCategoryById[categoryId] != 0;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool IsCategoryOverLimit( ushort categoryId )
		{
			return _activeCountByCategoryId[categoryId] > _maxSimultaneousByCategoryId[categoryId];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool RejectsNewSoundAtLimit( ushort categoryId )
		{
			return _activeCountByCategoryId[categoryId] >= _maxSimultaneousByCategoryId[categoryId] &&
				_allowStealFromSameCategoryById[categoryId] == 0;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetLastPlayTime( ushort eventId )
		{
			return _lastPlayTimeByEventId[eventId];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void RecordPlayTime( ushort eventId, float timeSeconds )
		{
			_lastPlayTimeByEventId[eventId] = timeSeconds;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ushort GetConsecutiveStealCount( ushort eventId )
		{
			return _consecutiveStealCountByEventId[eventId];
		}

		public void RecordSteal( ushort eventId )
		{
			if ( _consecutiveStealCountByEventId[eventId] < ushort.MaxValue ) {
				_consecutiveStealCountByEventId[eventId]++;
			}

			_dirtyStealEvents.MarkDirty( eventId );
			_shouldDecayStealCounts = true;
		}

		public void DecayStealCountsIfNeeded()
		{
			if ( !_shouldDecayStealCounts ) {
				return;
			}

			_shouldDecayStealCounts = false;

			for ( int i = _dirtyStealEvents.Count - 1; i >= 0; i-- ) {
				int eventId = _dirtyStealEvents.GetDirtyId( i );
				ushort value = _consecutiveStealCountByEventId[eventId];
				if ( value > 0 ) {
					value--;
					_consecutiveStealCountByEventId[eventId] = value;
				}

				if ( value == 0 ) {
					_dirtyStealEvents.MarkClean( eventId );
				}
			}
		}

		private void EnsureEventCapacity( int count )
		{
			if ( count <= _lastPlayTimeByEventId.Length ) {
				return;
			}

			int newSize = NextPowerOfTwo( count );
			Array.Resize( ref _lastPlayTimeByEventId, newSize );
			Array.Resize( ref _consecutiveStealCountByEventId, newSize );
		}

		private void EnsureCategoryCapacity( int count )
		{
			if ( count <= _activeCountByCategoryId.Length ) {
				return;
			}

			int newSize = NextPowerOfTwo( count );
			Array.Resize( ref _activeCountByCategoryId, newSize );
			Array.Resize( ref _maxSimultaneousByCategoryId, newSize );
			Array.Resize( ref _priorityScaleByCategoryId, newSize );
			Array.Resize( ref _stealProtectionByCategoryId, newSize );
			Array.Resize( ref _allowStealFromSameCategoryById, newSize );
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

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static ushort ToUShortId( int id )
		{
			if ( (uint)id > ushort.MaxValue ) {
				throw new InvalidOperationException( "FMOD channel registry exceeded 65535 compact IDs." );
			}

			return (ushort)id;
		}
	}
}
