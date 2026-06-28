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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Core;
using Nomad.Core.Collections;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.CVars;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================

	FMODChannelService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal unsafe sealed class FMODChannelService : IChannelRepository
	{
		private const int INVALID_INDEX = -1;
		private const byte FLAG_ESSENTIAL = 1 << 0;

		private readonly ILoggerCategory _log;
		private readonly IListenerService _listenerService;
		private readonly IResourceCacheService<IAudioResource, string> _eventRepository;
		private readonly FMODPriorityCalculator _priorityCalculator;
		private readonly FMOD.Studio.EVENT_CALLBACK _finishedCallback;
		private readonly ConcurrentQueue<nint> _finishedInstances = new ConcurrentQueue<nint>();

		private readonly int _capacity;
		private int _freeTop;
		private int _denseCount;

		private readonly FMODChannelStorage _arena;

		// Cold-path registries (acceptable here; not used in per-frame hot loops)
		private readonly Dictionary<string, ushort> _eventIds = new Dictionary<string, ushort>( StringComparer.Ordinal );
		private readonly Dictionary<string, ushort> _categoryIds = new Dictionary<string, ushort>( StringComparer.Ordinal );
		private string[] _eventNames;
		private string[] _categoryNames;
		private int _eventCount;
		private int _categoryCount;

		// Event stats by numeric event id
		private float[] _lastPlayTimeByEventId;
		private ushort[] _consecutiveStealCountByEventId;
		private ushort[] _dirtyStealEventIds;
		private byte[] _dirtyStealEventMarks;
		private int _dirtyStealEventCount;
		private bool _shouldDecay;

		// Category metadata by numeric category id
		private int[] _categoryHeadById;
		private ushort[] _activeCountByCategoryId;
		private ushort[] _maxSimultaneousByCategoryId;
		private float[] _priorityScaleByCategoryId;
		private float[] _stealProtectionByCategoryId;
		private byte[] _allowStealFromSameCategoryById;

		private readonly InstanceToSlotMap _instanceToSlot;
		private readonly ICVar<int> _maxChannels;
		private readonly ICVar<int> _maxActiveChannels;
		private readonly ISubscriptionHandle _maxActiveChannelsChanged;
		private ISubscriptionHandle _minTimeBetweenChannelStealsChanged;
		private ISubscriptionHandle _distanceWeightChanged;
		private ISubscriptionHandle _volumeWeightChanged;

		private float _elapsedSeconds;
		private float _distanceWeight;
		private float _volumeWeight;
		private float _minTimeBetweenChannelSteals;
		private bool _isDisposed;

		public FMODBusRepository BusRepository { get; }
		public int ActiveCount => _denseCount;
		public int Capacity => _capacity;

		public FMODChannelService(
			ILoggerService logger,
			ICVarSystemService cvarSystem,
			IListenerService listenerService,
			FMODDevice fmodSystem )
		{
			_listenerService = listenerService;
			_eventRepository = fmodSystem.EventRepository;
			_priorityCalculator = new FMODPriorityCalculator( cvarSystem, listenerService );
			BusRepository = new FMODBusRepository( fmodSystem );

			_maxChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_CHANNELS );
			_maxActiveChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS );

			if ( _maxChannels.Value <= 0 ) {
				throw new InvalidOperationException( "MAX_CHANNELS must be > 0." );
			}
			if ( _maxActiveChannels.Value <= 0 || _maxActiveChannels.Value > _maxChannels.Value ) {
				throw new InvalidOperationException( "MAX_ACTIVE_CHANNELS must be in range [1, MAX_CHANNELS]." );
			}

			_capacity = _maxChannels.Value;
			_arena = new FMODChannelStorage( _capacity );

			new Span<int>( _arena.SlotToDense, _arena.Capacity ).Fill( INVALID_INDEX );
			new Span<int>( _arena.SlotNextInCategory, _arena.Capacity ).Fill( INVALID_INDEX );
			new Span<int>( _arena.SlotPrevInCategory, _arena.Capacity ).Fill( INVALID_INDEX );

			for ( int i = 0; i < _capacity; i++ ) {
				_arena.FreeSlots[i] = _capacity - 1 - i;
			}
			_freeTop = _capacity;

			_eventNames = new string[16];
			_categoryNames = new string[16];
			_lastPlayTimeByEventId = new float[16];
			_consecutiveStealCountByEventId = new ushort[16];
			_dirtyStealEventIds = new ushort[16];
			_dirtyStealEventMarks = new byte[16];
			_categoryHeadById = new int[16];
			_activeCountByCategoryId = new ushort[16];
			_maxSimultaneousByCategoryId = new ushort[16];
			_priorityScaleByCategoryId = new float[16];
			_stealProtectionByCategoryId = new float[16];
			_allowStealFromSameCategoryById = new byte[16];
			Array.Fill( _categoryHeadById, INVALID_INDEX );

			_instanceToSlot = new InstanceToSlotMap( _capacity * 2 );
			_finishedCallback = SoundFinishedCallback;
			_log = logger.CreateCategory( "FMODChannelService", LogLevel.Debug, true );

			InitConfig( cvarSystem );
			_maxActiveChannelsChanged = _maxActiveChannels.ValueChanged.Subscribe( OnMaxActiveChannelsValueChanged );
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			for ( int dense = _denseCount - 1; dense >= 0; dense-- ) {
				ForceStopDense( dense, false );
			}

			_maxActiveChannelsChanged?.Dispose();
			_minTimeBetweenChannelStealsChanged?.Dispose();
			_distanceWeightChanged?.Dispose();
			_volumeWeightChanged?.Dispose();
			_priorityCalculator?.Dispose();
			_arena?.Dispose();

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		public FMODChannelHandle? AllocateChannel(
			string eventName,
			Vector3 position,
			SoundCategory category,
			float basePriority = 0.5f,
			bool isEssential = false )
		{

			ProcessFinishedInstances();

			ushort categoryNumericId = GetOrCreateCategoryId( category );
			ushort eventNumericId = GetOrCreateEventId( eventName );

			if ( _activeCountByCategoryId[categoryNumericId] >= _maxSimultaneousByCategoryId[categoryNumericId] &&
				 _allowStealFromSameCategoryById[categoryNumericId] == 0 ) {
				return null;
			}

			float now = _elapsedSeconds;
			float actualPriority = CalculateActualPriority(
				now,
				eventNumericId,
				categoryNumericId,
				position.X,
				position.Y,
				position.Z,
				basePriority );

			int slot = AcquireSlotOrSteal( now, actualPriority, categoryNumericId, isEssential );
			if ( slot == INVALID_INDEX ) {
				_log.PrintError( $"AllocateChannel: no channel available for '{eventName}'." );
				return null;
			}

			FMODEventResource resource = CreateSoundInstance( eventName );
			resource.CreateInstance( out var instance );
			instance.Position = position;
			FMODValidator.ValidateCall( instance.SetFinishedCallback( _finishedCallback ) );
			FMODValidator.ValidateCall( instance.Start() );

			uint generation = ++_arena.Generation[slot];
			int dense = _denseCount++;
			_arena.SlotToDense[slot] = dense;
			_arena.DenseToSlot[dense] = slot;

			nint instanceHandle = (nint)instance;
			_arena.InstancePtr[dense] = instanceHandle;
			_arena.PosX[dense] = position.X;
			_arena.PosY[dense] = position.Y;
			_arena.PosZ[dense] = position.Z;
			_arena.BasePriority[dense] = basePriority;
			_arena.CurrentPriority[dense] = actualPriority;
			_arena.StartTime[dense] = now;
			_arena.LastStolenTime[dense] = -999999.0f;
			_arena.Volume[dense] = 1.0f;
			_arena.EventId[dense] = eventNumericId;
			_arena.CategoryId[dense] = categoryNumericId;
			_arena.Flags[dense] = isEssential ? FLAG_ESSENTIAL : (byte)0;
			_arena.UserVolume[dense] = 1.0f;
			_arena.Pitch[dense] = 1.0f;
			_arena.Attenuation[dense] = 1.0f;
			instance.Volume = CalculateInstanceVolume( dense );

			_instanceToSlot.Set( instanceHandle, slot );
			LinkSlotIntoCategory( slot, categoryNumericId );
			_activeCountByCategoryId[categoryNumericId]++;
			_lastPlayTimeByEventId[eventNumericId] = now;

			return new FMODChannelHandle( slot, generation );
		}

		public void Update( float deltaTime )
		{
			_elapsedSeconds += deltaTime;

			ProcessFinishedInstances();
			DecayStealCountsIfNeeded();
			UpdatePrioritiesAndVolumes();
			CleanupStoppedInstances();
			EnforceCategoryLimits();
		}

		public bool IsAlive( FMODChannelHandle handle )
		{
			if ( !handle.IsValid ) {
				return false;
			}
			int slot = handle.Slot;
			if ( (uint)slot >= (uint)_capacity ) {
				return false;
			}
			return _arena.Generation[slot] == handle.Generation && _arena.SlotToDense[slot] != INVALID_INDEX;
		}

		public bool TryStopChannel( FMODChannelHandle handle, bool wasStolen = false )
		{
			if ( !IsAlive( handle ) ) {
				return false;
			}
			ForceStopSlot( handle.Slot, wasStolen );
			return true;
		}

		public bool TryGetChannelView( FMODChannelHandle handle, out FMODChannelView view )
		{
			view = default;
			if ( !IsAlive( handle ) ) {
				return false;
			}

			int dense = _arena.SlotToDense[handle.Slot];
			ushort ev = _arena.EventId[dense];
			ushort cat = _arena.CategoryId[dense];
			bool playing = IsInstancePlaying( _arena.InstancePtr[dense] );

			view = new FMODChannelView(
				handle,
				_eventNames[ev],
				ev,
				cat,
				new Vector3( _arena.PosX[dense], _arena.PosY[dense], _arena.PosZ[dense] ),
				_arena.BasePriority[dense],
				_arena.CurrentPriority[dense],
				_arena.StartTime[dense],
				_arena.LastStolenTime[dense],
				_arena.Volume[dense],
				(_arena.Flags[dense] & FLAG_ESSENTIAL) != 0,
				playing );
			return true;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryResolveDense( FMODChannelHandle handle, out int dense )
		{
			dense = INVALID_INDEX;

			int slot = handle.Slot;
			if ( (uint)slot >= (uint)_capacity ) {
				return false;
			}
			if ( _arena.Generation[slot] != handle.Generation ) {
				return false;
			}
			dense = _arena.SlotToDense[slot];
			return dense != INVALID_INDEX;
		}

		public bool TrySetPosition( FMODChannelHandle handle, Vector3 position )
		{
			if ( !TryResolveDense( handle, out int dense ) ) {
				return false;
			}

			_arena.PosX[dense] = position.X;
			_arena.PosY[dense] = position.Y;
			_arena.PosZ[dense] = position.Z;

			var instance = new FMOD.Studio.EventInstance( _arena.InstancePtr[dense] );
			if ( instance.isValid() ) {
				instance.set3DAttributes( position.Make3D() );
			}

			return true;
		}

		public bool TrySetVolume( FMODChannelHandle handle, float volume )
		{
			if ( !TryResolveDense( handle, out int dense ) ) {
				return false;
			}

			_arena.UserVolume[dense] = volume;

			var instance = new FMOD.Studio.EventInstance( _arena.InstancePtr[dense] );
			if ( instance.isValid() ) {
				instance.setVolume( CalculateInstanceVolume( dense ) );
			}
			return true;
		}

		public bool TrySetPitch( FMODChannelHandle handle, float pitch )
		{
			if ( !TryResolveDense( handle, out int dense ) ) {
				return false;
			}

			_arena.Pitch[dense] = pitch;

			var instance = new FMOD.Studio.EventInstance( _arena.InstancePtr[dense] );
			if ( instance.isValid() ) {
				instance.setPitch( pitch );
			}

			return true;
		}

		public bool IsPlaying( FMODChannelHandle handle )
		{
			return TryResolveDense( handle, out int dense ) && IsInstancePlaying( _arena.InstancePtr[dense] );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private int AcquireSlotOrSteal( float now, float incomingPriority, ushort incomingCategoryId, bool isEssential )
		{
			if ( !isEssential && _denseCount >= _maxActiveChannels.Value ) {
				return StealBestCandidate( now, incomingPriority, incomingCategoryId );
			}

			if ( _freeTop > 0 ) {
				return _arena.FreeSlots[--_freeTop];
			}

			return StealBestCandidate( now, incomingPriority, incomingCategoryId );
		}

		private int StealBestCandidate( float now, float incomingPriority, ushort incomingCategoryId )
		{
			float bestScore = float.MinValue;
			int bestDense = INVALID_INDEX;

			for ( int dense = 0; dense < _denseCount; dense++ ) {
				if ( (_arena.Flags[dense] & FLAG_ESSENTIAL) != 0 ) {
					continue;
				}
				if ( !IsInstancePlaying( _arena.InstancePtr[dense] ) ) {
					continue;
				}

				ushort cat = _arena.CategoryId[dense];
				float age = now - _arena.StartTime[dense];
				if ( age < _stealProtectionByCategoryId[cat] ) {
					continue;
				}
				if ( now - _arena.LastStolenTime[dense] < _minTimeBetweenChannelSteals ) {
					continue;
				}

				float stealScore = CalculateStealScore( dense, now, incomingPriority, incomingCategoryId );
				if ( stealScore > bestScore ) {
					bestScore = stealScore;
					bestDense = dense;
				}
			}

			if ( bestDense == INVALID_INDEX || bestScore <= 0.0f ) {
				return INVALID_INDEX;
			}

			ushort stolenEventId = _arena.EventId[bestDense];
			int reusedSlot = _arena.DenseToSlot[bestDense];
			ForceStopDense( bestDense, true, false );
			RecordSteal( stolenEventId );
			return reusedSlot;
		}

		private void ProcessFinishedInstances()
		{
			while ( _finishedInstances.TryDequeue( out nint instanceHandle ) ) {
				if ( _instanceToSlot.TryGet( instanceHandle, out int slot ) ) {
					if ( _arena.SlotToDense[slot] != INVALID_INDEX ) {
						ForceStopSlot( slot, false );
					}
				}
			}
		}

		private void DecayStealCountsIfNeeded()
		{
			if ( !_shouldDecay ) {
				return;
			}
			_shouldDecay = false;

			int write = 0;
			for ( int i = 0; i < _dirtyStealEventCount; i++ ) {
				ushort eventNumericId = _dirtyStealEventIds[i];
				ushort value = _consecutiveStealCountByEventId[eventNumericId];
				if ( value > 0 ) {
					value--;
					_consecutiveStealCountByEventId[eventNumericId] = value;
				}
				if ( value > 0 ) {
					_dirtyStealEventIds[write++] = eventNumericId;
				} else {
					_dirtyStealEventMarks[eventNumericId] = 0;
				}
			}
			_dirtyStealEventCount = write;
		}

		private void UpdatePrioritiesAndVolumes()
		{
			Vector3 listener = _listenerService.ActiveListener;
			float lx = listener.X;
			float ly = listener.Y;
			float lz = listener.Z;
			for ( int dense = 0; dense < _denseCount; dense++ ) {
				nint ptr = _arena.InstancePtr[dense];
				if ( !IsInstancePlaying( ptr ) ) {
					continue;
				}

				float dx = _arena.PosX[dense] - lx;
				float dy = _arena.PosY[dense] - ly;
				float dz = _arena.PosZ[dense] - lz;
				float distance = MathF.Sqrt( dx * dx + dy * dy + dz * dz );
				float distanceFactor = _priorityCalculator.CalculateDistanceFactor( distance );

				_arena.Attenuation[dense] = distanceFactor;
				_arena.CurrentPriority[dense] =
					_arena.BasePriority[dense] *
					_priorityScaleByCategoryId[_arena.CategoryId[dense]] *
					distanceFactor;

				var instance = new FMOD.Studio.EventInstance( ptr );
				instance.setVolume( CalculateInstanceVolume( dense ) );
			}
		}

		private void CleanupStoppedInstances()
		{
			for ( int dense = _denseCount - 1; dense >= 0; dense-- ) {
				if ( !IsInstancePlaying( _arena.InstancePtr[dense] ) ) {
					ForceStopDense( dense, false );
				}
			}
		}

		private void EnforceCategoryLimits()
		{
			for ( ushort categoryNumericId = 0; categoryNumericId < _categoryCount; categoryNumericId++ ) {
				while ( _activeCountByCategoryId[categoryNumericId] > _maxSimultaneousByCategoryId[categoryNumericId] ) {
					int victimSlot = FindLowestPrioritySlotInCategory( categoryNumericId );
					if ( victimSlot == INVALID_INDEX ) {
						break;
					}
					ForceStopSlot( victimSlot, true );
				}
			}
		}

		private int FindLowestPrioritySlotInCategory( ushort categoryNumericId )
		{
			int slot = _categoryHeadById[categoryNumericId];
			int bestSlot = INVALID_INDEX;
			float lowestPriority = float.MaxValue;

			while ( slot != INVALID_INDEX ) {
				int next = _arena.SlotNextInCategory[slot];
				int dense = _arena.SlotToDense[slot];
				if ( dense != INVALID_INDEX ) {
					if ( (_arena.Flags[dense] & FLAG_ESSENTIAL) == 0 && IsInstancePlaying( _arena.InstancePtr[dense] ) ) {
						float prio = _arena.CurrentPriority[dense];
						if ( prio < lowestPriority ) {
							lowestPriority = prio;
							bestSlot = slot;
						}
					}
				}
				slot = next;
			}

			return bestSlot;
		}

		private void ForceStopSlot( int slot, bool wasStolen )
		{
			int dense = _arena.SlotToDense[slot];
			if ( dense != INVALID_INDEX ) {
				ForceStopDense( dense, wasStolen );
			}
		}

		private void ForceStopDense( int dense, bool wasStolen, bool returnSlotToFreeList = true )
		{
			int slot = _arena.DenseToSlot[dense];
			ushort cat = _arena.CategoryId[dense];

			if ( wasStolen ) {
				_arena.LastStolenTime[dense] = _elapsedSeconds;
			}

			nint instanceHandle = _arena.InstancePtr[dense];
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( instance.isValid() ) {
				instance.setCallback( null );
				instance.stop( FMOD.Studio.STOP_MODE.IMMEDIATE );
				instance.release();
				instance.clearHandle();
			}

			_instanceToSlot.Remove( instanceHandle );
			UnlinkSlotFromCategory( slot, cat );
			_activeCountByCategoryId[cat]--;

			int lastDense = --_denseCount;
			if ( dense != lastDense ) {
				MoveDense( lastDense, dense );
			}

			_arena.InstancePtr[lastDense] = 0;
			_arena.PosX[lastDense] = 0.0f;
			_arena.PosY[lastDense] = 0.0f;
			_arena.PosZ[lastDense] = 0.0f;
			_arena.BasePriority[lastDense] = 0.0f;
			_arena.CurrentPriority[lastDense] = 0.0f;
			_arena.StartTime[lastDense] = 0.0f;
			_arena.LastStolenTime[lastDense] = 0.0f;
			_arena.Volume[lastDense] = 0.0f;
			_arena.EventId[lastDense] = 0;
			_arena.CategoryId[lastDense] = 0;
			_arena.Flags[lastDense] = 0;
			_arena.UserVolume[lastDense] = 0.0f;
			_arena.Pitch[lastDense] = 0.0f;
			_arena.Attenuation[lastDense] = 0.0f;

			_arena.SlotToDense[slot] = INVALID_INDEX;
			if ( returnSlotToFreeList ) {
				_arena.FreeSlots[_freeTop++] = slot;
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void MoveDense( int srcDense, int dstDense )
		{
			int movedSlot = _arena.DenseToSlot[srcDense];
			_arena.DenseToSlot[dstDense] = movedSlot;
			_arena.SlotToDense[movedSlot] = dstDense;

			_arena.InstancePtr[dstDense] = _arena.InstancePtr[srcDense];
			_arena.PosX[dstDense] = _arena.PosX[srcDense];
			_arena.PosY[dstDense] = _arena.PosY[srcDense];
			_arena.PosZ[dstDense] = _arena.PosZ[srcDense];
			_arena.BasePriority[dstDense] = _arena.BasePriority[srcDense];
			_arena.CurrentPriority[dstDense] = _arena.CurrentPriority[srcDense];
			_arena.StartTime[dstDense] = _arena.StartTime[srcDense];
			_arena.LastStolenTime[dstDense] = _arena.LastStolenTime[srcDense];
			_arena.Volume[dstDense] = _arena.Volume[srcDense];
			_arena.EventId[dstDense] = _arena.EventId[srcDense];
			_arena.CategoryId[dstDense] = _arena.CategoryId[srcDense];
			_arena.Flags[dstDense] = _arena.Flags[srcDense];
			_arena.UserVolume[dstDense] = _arena.UserVolume[srcDense];
			_arena.Pitch[dstDense] = _arena.Pitch[srcDense];
			_arena.Attenuation[dstDense] = _arena.Attenuation[srcDense];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void LinkSlotIntoCategory( int slot, ushort categoryNumericId )
		{
			int head = _categoryHeadById[categoryNumericId];
			_arena.SlotPrevInCategory[slot] = INVALID_INDEX;
			_arena.SlotNextInCategory[slot] = head;
			if ( head != INVALID_INDEX ) {
				_arena.SlotPrevInCategory[head] = slot;
			}
			_categoryHeadById[categoryNumericId] = slot;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void UnlinkSlotFromCategory( int slot, ushort categoryNumericId )
		{
			int prev = _arena.SlotPrevInCategory[slot];
			int next = _arena.SlotNextInCategory[slot];

			if ( prev != INVALID_INDEX ) {
				_arena.SlotNextInCategory[prev] = next;
			} else {
				_categoryHeadById[categoryNumericId] = next;
			}
			if ( next != INVALID_INDEX ) {
				_arena.SlotPrevInCategory[next] = prev;
			}

			_arena.SlotPrevInCategory[slot] = INVALID_INDEX;
			_arena.SlotNextInCategory[slot] = INVALID_INDEX;
		}

		private float CalculateActualPriority(
			float now,
			ushort eventNumericId,
			ushort categoryNumericId,
			float x,
			float y,
			float z,
			float basePriority )
		{

			Vector3 listener = _listenerService.ActiveListener;
			float dx = x - listener.X;
			float dy = y - listener.Y;
			float dz = z - listener.Z;
			float distance = MathF.Sqrt( dx * dx + dy * dy + dz * dz );
			float distanceFactor = _priorityCalculator.CalculateDistanceFactor( distance );

			float priority = basePriority * _priorityScaleByCategoryId[categoryNumericId] * distanceFactor;

			float timeSinceLastPlay = now - _lastPlayTimeByEventId[eventNumericId];
			if ( timeSinceLastPlay < 0.050f ) {
				priority *= 0.70f;
			} else if ( timeSinceLastPlay < 0.100f ) {
				priority *= 0.85f;
			}

			ushort steals = _consecutiveStealCountByEventId[eventNumericId];
			if ( steals > 0 ) {
				priority *= 1.0f + (steals * 0.05f);
			}

			return priority;
		}

		private float CalculateStealScore( int dense, float now, float incomingPriority, ushort incomingCategoryId )
		{
			float age = now - _arena.StartTime[dense];
			float ageFactor = age >= 5.0f ? 1.0f : age * 0.2f;
			float distanceFactor = 1.0f - _arena.Attenuation[dense];
			float volumeFactor = 1.0f - _arena.Volume[dense];

			float score =
				(incomingPriority - _arena.CurrentPriority[dense]) * 2.0f +
				ageFactor * 0.5f +
				distanceFactor * _distanceWeight +
				volumeFactor * _volumeWeight;

			if ( _arena.CategoryId[dense] == incomingCategoryId ) {
				score *= 0.5f;
			}

			float timeSinceLastStolen = now - _arena.LastStolenTime[dense];
			if ( timeSinceLastStolen < 1.0f ) {
				score *= timeSinceLastStolen;
			}

			return score;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private float CalculateInstanceVolume( int dense )
		{
			float volume = _arena.UserVolume[dense] * _arena.Attenuation[dense];
			_arena.Volume[dense] = Math.Clamp( volume, 0.0f, 1.0f );
			return _arena.Volume[dense];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void RecordSteal( ushort eventNumericId )
		{
			if ( _consecutiveStealCountByEventId[eventNumericId] < ushort.MaxValue ) {
				_consecutiveStealCountByEventId[eventNumericId]++;
			}
			if ( _dirtyStealEventMarks[eventNumericId] == 0 ) {
				_dirtyStealEventMarks[eventNumericId] = 1;
				_dirtyStealEventIds[_dirtyStealEventCount++] = eventNumericId;
			}
			_shouldDecay = true;
		}

		private FMODEventResource CreateSoundInstance( string eventName )
		{
			var cached = _eventRepository.GetCached( eventName )
				?? throw new Exception( $"Couldn't find event description for '{eventName}'." );
			cached.Get( out var description );

			if ( description is not FMODEventResource eventResource ) {
				throw new InvalidCastException();
			}

			return eventResource;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static bool IsInstancePlaying( nint instanceHandle )
		{
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( !instance.isValid() ) {
				return false;
			}

			FMOD.RESULT result = instance.getPlaybackState( out var state );
			return result == FMOD.RESULT.OK && state == FMOD.Studio.PLAYBACK_STATE.PLAYING;
		}

		private ushort GetOrCreateEventId( string eventName )
		{
			if ( _eventIds.TryGetValue( eventName, out ushort existing ) ) {
				return existing;
			}

			ushort id = checked((ushort)_eventCount++);
			EnsureEventCapacity( _eventCount );
			_eventIds.Add( eventName, id );
			_eventNames[id] = eventName;
			return id;
		}

		private ushort GetOrCreateCategoryId( SoundCategory category )
		{
			string categoryName = category.Config.Name;
			if ( _categoryIds.TryGetValue( categoryName, out ushort existing ) ) {
				return existing;
			}

			ushort id = checked((ushort)_categoryCount++);
			EnsureCategoryCapacity( _categoryCount );
			_categoryIds.Add( categoryName, id );
			_categoryNames[id] = categoryName;
			_categoryHeadById[id] = INVALID_INDEX;
			_maxSimultaneousByCategoryId[id] = (ushort)Math.Clamp( category.Config.MaxSimultaneous, 0, ushort.MaxValue );
			_priorityScaleByCategoryId[id] = category.Config.PriorityScale;
			_stealProtectionByCategoryId[id] = category.Config.StealProtectionTime;
			_allowStealFromSameCategoryById[id] = category.Config.AllowStealingFromSameCategory ? (byte)1 : (byte)0;
			return id;
		}

		private void EnsureEventCapacity( int count )
		{
			if ( count <= _eventNames.Length ) {
				return;
			}
			int newSize = NextPow2( count );
			Array.Resize( ref _eventNames, newSize );
			Array.Resize( ref _lastPlayTimeByEventId, newSize );
			Array.Resize( ref _consecutiveStealCountByEventId, newSize );
			Array.Resize( ref _dirtyStealEventMarks, newSize );
			Array.Resize( ref _dirtyStealEventIds, newSize );
		}

		private void EnsureCategoryCapacity( int count )
		{
			if ( count <= _categoryNames.Length ) {
				return;
			}
			int old = _categoryNames.Length;
			int newSize = NextPow2( count );
			Array.Resize( ref _categoryNames, newSize );
			Array.Resize( ref _categoryHeadById, newSize );
			Array.Resize( ref _activeCountByCategoryId, newSize );
			Array.Resize( ref _maxSimultaneousByCategoryId, newSize );
			Array.Resize( ref _priorityScaleByCategoryId, newSize );
			Array.Resize( ref _stealProtectionByCategoryId, newSize );
			Array.Resize( ref _allowStealFromSameCategoryById, newSize );
			for ( int i = old; i < newSize; i++ ) {
				_categoryHeadById[i] = INVALID_INDEX;
			}
		}

		private void InitConfig( ICVarSystemService cvarSystem )
		{
			var minTimeBetweenChannelSteals = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.MIN_TIME_BETWEEN_CHANNEL_STEALS );
			_minTimeBetweenChannelSteals = minTimeBetweenChannelSteals.Value;
			_minTimeBetweenChannelStealsChanged = minTimeBetweenChannelSteals.ValueChanged.Subscribe( OnMinTimeBetweenChannelStealsValueChanged );

			var distanceWeight = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.DISTANCE_WEIGHT );
			_distanceWeight = distanceWeight.Value;
			_distanceWeightChanged = distanceWeight.ValueChanged.Subscribe( OnDistanceWeightValueChanged );

			var volumeWeight = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.VOLUME_WEIGHT );
			_volumeWeight = volumeWeight.Value;
			_volumeWeightChanged = volumeWeight.ValueChanged.Subscribe( OnVolumeWeightValueChanged );
		}

		private void OnMaxActiveChannelsValueChanged( in CVarValueChangedEventArgs<int> args )
		{
			if ( args.NewValue <= 0 ) {
				_maxActiveChannels.Value = Math.Max( 1, args.OldValue );
				return;
			}
			if ( args.NewValue > _capacity ) {
				_maxActiveChannels.Value = _capacity;
			}
		}

		private void OnMinTimeBetweenChannelStealsValueChanged( in CVarValueChangedEventArgs<float> args )
		{
			_minTimeBetweenChannelSteals = args.NewValue;
		}

		private void OnDistanceWeightValueChanged( in CVarValueChangedEventArgs<float> args )
		{
			_distanceWeight = args.NewValue;
		}

		private void OnVolumeWeightValueChanged( in CVarValueChangedEventArgs<float> args )
		{
			_volumeWeight = args.NewValue;
		}

		private FMOD.RESULT SoundFinishedCallback( FMOD.Studio.EVENT_CALLBACK_TYPE type, nint instance, IntPtr parameters )
		{
			_finishedInstances.Enqueue( instance );
			return FMOD.RESULT.OK;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static int NextPow2( int value )
		{
			value--;
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			value++;
			return value < 8 ? 8 : value;
		}

		private sealed class InstanceToSlotMap
		{
			private nint[] _keys;
			private int[] _values;
			private byte[] _state; // 0 empty, 1 used, 2 tombstone
			private int _count;
			private int _mask;

			public InstanceToSlotMap( int capacity )
			{
				int size = NextPow2( Math.Max( 8, capacity ) );
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
				int idx = FindIndex( key );
				if ( idx < 0 ) {
					value = -1;
					return false;
				}
				value = _values[idx];
				return true;
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public void Remove( nint key )
			{
				int idx = FindIndex( key );
				if ( idx < 0 ) {
					return;
				}
				_state[idx] = 2;
				_keys[idx] = 0;
				_values[idx] = 0;
				_count--;
			}

			private void InsertOrUpdate( nint key, int value )
			{
				int idx = Hash( key ) & _mask;
				int firstTombstone = -1;
				while ( true ) {
					byte state = _state[idx];
					if ( state == 0 ) {
						int target = firstTombstone >= 0 ? firstTombstone : idx;
						_state[target] = 1;
						_keys[target] = key;
						_values[target] = value;
						_count++;
						return;
					}
					if ( state == 2 ) {
						if ( firstTombstone < 0 ) {
							firstTombstone = idx;
						}
					} else if ( _keys[idx] == key ) {
						_values[idx] = value;
						return;
					}
					idx = (idx + 1) & _mask;
				}
			}

			private int FindIndex( nint key )
			{
				int idx = Hash( key ) & _mask;
				while ( true ) {
					byte state = _state[idx];
					if ( state == 0 ) {
						return -1;
					}

					if ( state == 1 && _keys[idx] == key ) {
						return idx;
					}

					idx = (idx + 1) & _mask;
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
			private static int Hash( nint value ) => value.GetHashCode();
		}
	}
}
