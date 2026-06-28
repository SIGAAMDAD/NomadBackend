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
using System.Numerics;
using System.Runtime.CompilerServices;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Core;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================

	FMODChannelService

	===================================================================================
	*/
	/// <summary>
	/// Coordinates FMOD channel allocation and update policy. Raw storage, name registries,
	/// instance mutation, category indexing, and priority math are delegated to smaller types
	/// so this class remains the orchestration boundary for the channel subsystem.
	/// </summary>

	internal unsafe sealed class FMODChannelService : IChannelRepository
	{
		private readonly ILoggerCategory _log;
		private readonly FMODPriorityCalculator _priorityCalculator;
		private readonly FMOD.Studio.EVENT_CALLBACK _finishedCallback;
		private readonly ConcurrentQueue<nint> _finishedInstances = new ConcurrentQueue<nint>();

		private readonly int _capacity;
		private int _denseCount;

		private readonly FMODChannelStorage _storage;
		private readonly FMODChannelSlotAllocator _slots;
		private readonly FMODChannelRegistry _registry;
		private readonly FMODChannelCategoryIndex _categoryIndex;
		private readonly FMODChannelInstanceController _instances;
		private readonly FMODChannelPriorityPolicy _priorityPolicy;
		private readonly FMODInstanceSlotMap _instanceToSlot;

		private readonly ICVar<int> _maxChannels;
		private readonly ICVar<int> _maxActiveChannels;
		private readonly ISubscriptionHandle _maxActiveChannelsChanged;
		private ISubscriptionHandle _minTimeBetweenChannelStealsChanged;
		private ISubscriptionHandle _distanceWeightChanged;
		private ISubscriptionHandle _volumeWeightChanged;

		private float _elapsedSeconds;
		private float _minTimeBetweenChannelSteals;
		private bool _isDisposed;

		public FMODBusRepository BusRepository { get; }
		public int ActiveCount => _denseCount;
		public int Capacity => _capacity;

		public FMODChannelService(
			ILoggerService logger,
			ICVarSystemService cvarSystem,
			IListenerService listenerService,
			FMODDevice fmodSystem
		)
		{
			_priorityCalculator = new FMODPriorityCalculator( cvarSystem, listenerService );
			BusRepository = new FMODBusRepository( fmodSystem );

			_maxChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_CHANNELS );
			_maxActiveChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS );

			ValidateInitialChannelLimits();

			_capacity = _maxChannels.Value;
			_storage = new FMODChannelStorage( _capacity );
			_slots = new FMODChannelSlotAllocator( _storage );
			_registry = new FMODChannelRegistry();
			_categoryIndex = new FMODChannelCategoryIndex( _storage );
			_instances = new FMODChannelInstanceController( fmodSystem.EventRepository );
			_priorityPolicy = new FMODChannelPriorityPolicy( listenerService, _priorityCalculator, _registry, _storage );
			_instanceToSlot = new FMODInstanceSlotMap( _capacity * 2 );

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
			_log?.Dispose();
			_storage?.Dispose();

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

			ushort categoryId = _registry.GetOrCreateCategoryId( category );
			ushort eventId = _registry.GetOrCreateEventId( eventName );

			if ( _registry.RejectsNewSoundAtLimit( categoryId ) ) {
				return null;
			}

			float now = _elapsedSeconds;
			float actualPriority = _priorityPolicy.CalculateIncomingPriority(
				now,
				eventId,
				categoryId,
				position,
				basePriority );

			int slot = AcquireSlotOrSteal( now, actualPriority, categoryId, isEssential );
			if ( slot == FMODChannelConstants.InvalidIndex ) {
				_log.PrintError( $"AllocateChannel: no channel available for '{eventName}'." );
				return null;
			}

			nint instanceHandle;
			try {
				instanceHandle = _instances.CreateStartedInstance( eventName, position, _finishedCallback );
			} catch {
				_slots.Release( slot );
				throw;
			}

			FMODChannelHandle handle = _slots.CreateHandle( slot );
			int dense = _denseCount++;
			_storage.SlotToDense[slot] = dense;
			_storage.DenseToSlot[dense] = slot;
			InitializeDenseChannel(
				dense,
				instanceHandle,
				position,
				basePriority,
				actualPriority,
				now,
				eventId,
				categoryId,
				isEssential );

			FMODChannelInstanceController.SetVolume( instanceHandle, _priorityPolicy.CalculateInstanceVolume( dense ) );
			_instanceToSlot.Set( instanceHandle, slot );
			_categoryIndex.Link( slot, categoryId );
			_registry.IncrementActiveCount( categoryId );
			_registry.RecordPlayTime( eventId, now );

			return handle;
		}

		public void Update( float deltaTime )
		{
			_elapsedSeconds += deltaTime;

			ProcessFinishedInstances();
			_registry.DecayStealCountsIfNeeded();
			_priorityPolicy.UpdatePrioritiesAndVolumes( _denseCount );
			CleanupStoppedInstances();
			EnforceCategoryLimits();
		}

		public bool IsAlive( FMODChannelHandle handle )
		{
			return _slots.IsAlive( handle );
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

			int dense = _storage.SlotToDense[handle.Slot];
			ushort eventId = _storage.EventId[dense];
			ushort categoryId = _storage.CategoryId[dense];

			view = new FMODChannelView(
				handle,
				_registry.GetEventName( eventId ),
				eventId,
				categoryId,
				new Vector3( _storage.PosX[dense], _storage.PosY[dense], _storage.PosZ[dense] ),
				_storage.BasePriority[dense],
				_storage.CurrentPriority[dense],
				_storage.StartTime[dense],
				_storage.LastStolenTime[dense],
				_storage.Volume[dense],
				(_storage.Flags[dense] & FMODChannelConstants.EssentialFlag) != 0,
				FMODChannelInstanceController.IsPlaying( _storage.InstancePtr[dense] ) );
			return true;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryResolveDense( FMODChannelHandle handle, out int dense )
		{
			dense = FMODChannelConstants.InvalidIndex;

			int slot = handle.Slot;
			if ( (uint)slot >= (uint)_capacity ) {
				return false;
			}

			if ( _storage.Generation[slot] != handle.Generation ) {
				return false;
			}

			dense = _storage.SlotToDense[slot];
			return dense != FMODChannelConstants.InvalidIndex;
		}

		public bool TrySetPosition( FMODChannelHandle handle, Vector3 position )
		{
			if ( !TryResolveDense( handle, out int dense ) ) {
				return false;
			}

			_storage.PosX[dense] = position.X;
			_storage.PosY[dense] = position.Y;
			_storage.PosZ[dense] = position.Z;
			FMODChannelInstanceController.SetPosition( _storage.InstancePtr[dense], position );
			return true;
		}

		public bool TrySetVolume( FMODChannelHandle handle, float volume )
		{
			if ( !TryResolveDense( handle, out int dense ) ) {
				return false;
			}

			_storage.UserVolume[dense] = volume;
			FMODChannelInstanceController.SetVolume( _storage.InstancePtr[dense], _priorityPolicy.CalculateInstanceVolume( dense ) );
			return true;
		}

		public bool TrySetPitch( FMODChannelHandle handle, float pitch )
		{
			if ( !TryResolveDense( handle, out int dense ) ) {
				return false;
			}

			_storage.Pitch[dense] = pitch;
			FMODChannelInstanceController.SetPitch( _storage.InstancePtr[dense], pitch );
			return true;
		}

		public bool IsPlaying( FMODChannelHandle handle )
		{
			return TryResolveDense( handle, out int dense ) && FMODChannelInstanceController.IsPlaying( _storage.InstancePtr[dense] );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private int AcquireSlotOrSteal( float now, float incomingPriority, ushort incomingCategoryId, bool isEssential )
		{
			if ( !isEssential && _denseCount >= _maxActiveChannels.Value ) {
				return StealBestCandidate( now, incomingPriority, incomingCategoryId );
			}

			if ( _slots.TryAcquire( out int slot ) ) {
				return slot;
			}

			return StealBestCandidate( now, incomingPriority, incomingCategoryId );
		}

		private int StealBestCandidate( float now, float incomingPriority, ushort incomingCategoryId )
		{
			float bestScore = float.MinValue;
			int bestDense = FMODChannelConstants.InvalidIndex;

			for ( int dense = 0; dense < _denseCount; dense++ ) {
				if ( (_storage.Flags[dense] & FMODChannelConstants.EssentialFlag) != 0 ) {
					continue;
				}

				if ( !FMODChannelInstanceController.IsPlaying( _storage.InstancePtr[dense] ) ) {
					continue;
				}

				ushort categoryId = _storage.CategoryId[dense];
				float age = now - _storage.StartTime[dense];
				if ( age < _registry.GetStealProtection( categoryId ) ) {
					continue;
				}

				if ( now - _storage.LastStolenTime[dense] < _minTimeBetweenChannelSteals ) {
					continue;
				}

				float stealScore = _priorityPolicy.CalculateStealScore( dense, now, incomingPriority, incomingCategoryId );
				if ( stealScore > bestScore ) {
					bestScore = stealScore;
					bestDense = dense;
				}
			}

			if ( bestDense == FMODChannelConstants.InvalidIndex || bestScore <= 0.0f ) {
				return FMODChannelConstants.InvalidIndex;
			}

			ushort stolenEventId = _storage.EventId[bestDense];
			int reusedSlot = _storage.DenseToSlot[bestDense];
			ForceStopDense( bestDense, true, false );
			_registry.RecordSteal( stolenEventId );
			return reusedSlot;
		}

		private void ProcessFinishedInstances()
		{
			while ( _finishedInstances.TryDequeue( out nint instanceHandle ) ) {
				if ( !_instanceToSlot.TryGet( instanceHandle, out int slot ) ) {
					continue;
				}

				if ( _storage.SlotToDense[slot] != FMODChannelConstants.InvalidIndex ) {
					ForceStopSlot( slot, false );
				}
			}
		}

		private void CleanupStoppedInstances()
		{
			for ( int dense = _denseCount - 1; dense >= 0; dense-- ) {
				if ( !FMODChannelInstanceController.IsPlaying( _storage.InstancePtr[dense] ) ) {
					ForceStopDense( dense, false );
				}
			}
		}

		private void EnforceCategoryLimits()
		{
			for ( ushort categoryId = 0; categoryId < _registry.CategoryCount; categoryId++ ) {
				while ( _registry.IsCategoryOverLimit( categoryId ) ) {
					int victimSlot = _categoryIndex.FindLowestPrioritySlot( categoryId );
					if ( victimSlot == FMODChannelConstants.InvalidIndex ) {
						break;
					}

					ForceStopSlot( victimSlot, true );
				}
			}
		}

		private void ForceStopSlot( int slot, bool wasStolen )
		{
			int dense = _storage.SlotToDense[slot];
			if ( dense != FMODChannelConstants.InvalidIndex ) {
				ForceStopDense( dense, wasStolen );
			}
		}

		private void ForceStopDense( int dense, bool wasStolen, bool returnSlotToFreeList = true )
		{
			int slot = _storage.DenseToSlot[dense];
			ushort categoryId = _storage.CategoryId[dense];

			if ( wasStolen ) {
				_storage.LastStolenTime[dense] = _elapsedSeconds;
			}

			nint instanceHandle = _storage.InstancePtr[dense];
			FMODChannelInstanceController.StopAndRelease( instanceHandle );
			_instanceToSlot.Remove( instanceHandle );
			_categoryIndex.Unlink( slot, categoryId );
			_registry.DecrementActiveCount( categoryId );

			int lastDense = --_denseCount;
			if ( dense != lastDense ) {
				MoveDense( lastDense, dense );
			}

			ClearDense( lastDense );
			_storage.SlotToDense[slot] = FMODChannelConstants.InvalidIndex;

			if ( returnSlotToFreeList ) {
				_slots.Release( slot );
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void InitializeDenseChannel(
			int dense,
			nint instanceHandle,
			Vector3 position,
			float basePriority,
			float currentPriority,
			float startTime,
			ushort eventId,
			ushort categoryId,
			bool isEssential )
		{
			_storage.InstancePtr[dense] = instanceHandle;
			_storage.PosX[dense] = position.X;
			_storage.PosY[dense] = position.Y;
			_storage.PosZ[dense] = position.Z;
			_storage.BasePriority[dense] = basePriority;
			_storage.CurrentPriority[dense] = currentPriority;
			_storage.StartTime[dense] = startTime;
			_storage.LastStolenTime[dense] = FMODChannelConstants.InitialLastStolenTime;
			_storage.Volume[dense] = 1.0f;
			_storage.UserVolume[dense] = 1.0f;
			_storage.Attenuation[dense] = 1.0f;
			_storage.Pitch[dense] = 1.0f;
			_storage.EventId[dense] = eventId;
			_storage.CategoryId[dense] = categoryId;
			_storage.Flags[dense] = isEssential ? FMODChannelConstants.EssentialFlag : (byte)0;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void MoveDense( int srcDense, int dstDense )
		{
			int movedSlot = _storage.DenseToSlot[srcDense];
			_storage.DenseToSlot[dstDense] = movedSlot;
			_storage.SlotToDense[movedSlot] = dstDense;

			_storage.InstancePtr[dstDense] = _storage.InstancePtr[srcDense];
			_storage.PosX[dstDense] = _storage.PosX[srcDense];
			_storage.PosY[dstDense] = _storage.PosY[srcDense];
			_storage.PosZ[dstDense] = _storage.PosZ[srcDense];
			_storage.BasePriority[dstDense] = _storage.BasePriority[srcDense];
			_storage.CurrentPriority[dstDense] = _storage.CurrentPriority[srcDense];
			_storage.StartTime[dstDense] = _storage.StartTime[srcDense];
			_storage.LastStolenTime[dstDense] = _storage.LastStolenTime[srcDense];
			_storage.Volume[dstDense] = _storage.Volume[srcDense];
			_storage.UserVolume[dstDense] = _storage.UserVolume[srcDense];
			_storage.Attenuation[dstDense] = _storage.Attenuation[srcDense];
			_storage.Pitch[dstDense] = _storage.Pitch[srcDense];
			_storage.EventId[dstDense] = _storage.EventId[srcDense];
			_storage.CategoryId[dstDense] = _storage.CategoryId[srcDense];
			_storage.Flags[dstDense] = _storage.Flags[srcDense];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void ClearDense( int dense )
		{
			_storage.InstancePtr[dense] = 0;
			_storage.PosX[dense] = 0.0f;
			_storage.PosY[dense] = 0.0f;
			_storage.PosZ[dense] = 0.0f;
			_storage.BasePriority[dense] = 0.0f;
			_storage.CurrentPriority[dense] = 0.0f;
			_storage.StartTime[dense] = 0.0f;
			_storage.LastStolenTime[dense] = 0.0f;
			_storage.Volume[dense] = 0.0f;
			_storage.UserVolume[dense] = 0.0f;
			_storage.Attenuation[dense] = 0.0f;
			_storage.Pitch[dense] = 0.0f;
			_storage.EventId[dense] = 0;
			_storage.CategoryId[dense] = 0;
			_storage.Flags[dense] = 0;
		}

		private void InitConfig( ICVarSystemService cvarSystem )
		{
			var minTimeBetweenChannelSteals = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.MIN_TIME_BETWEEN_CHANNEL_STEALS );
			_minTimeBetweenChannelSteals = minTimeBetweenChannelSteals.Value;
			_minTimeBetweenChannelStealsChanged = minTimeBetweenChannelSteals.ValueChanged.Subscribe( OnMinTimeBetweenChannelStealsValueChanged );

			var distanceWeight = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.DISTANCE_WEIGHT );
			_priorityPolicy.DistanceWeight = distanceWeight.Value;
			_distanceWeightChanged = distanceWeight.ValueChanged.Subscribe( OnDistanceWeightValueChanged );

			var volumeWeight = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.VOLUME_WEIGHT );
			_priorityPolicy.VolumeWeight = volumeWeight.Value;
			_volumeWeightChanged = volumeWeight.ValueChanged.Subscribe( OnVolumeWeightValueChanged );
		}

		private void ValidateInitialChannelLimits()
		{
			if ( _maxChannels.Value <= 0 ) {
				throw new InvalidOperationException( "MAX_CHANNELS must be > 0." );
			}

			if ( _maxActiveChannels.Value <= 0 || _maxActiveChannels.Value > _maxChannels.Value ) {
				throw new InvalidOperationException( "MAX_ACTIVE_CHANNELS must be in range [1, MAX_CHANNELS]." );
			}
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
			_priorityPolicy.DistanceWeight = args.NewValue;
		}

		private void OnVolumeWeightValueChanged( in CVarValueChangedEventArgs<float> args )
		{
			_priorityPolicy.VolumeWeight = args.NewValue;
		}

		private FMOD.RESULT SoundFinishedCallback( FMOD.Studio.EVENT_CALLBACK_TYPE type, nint instance, IntPtr parameters )
		{
			_finishedInstances.Enqueue( instance );
			return FMOD.RESULT.OK;
		}
	}
}
