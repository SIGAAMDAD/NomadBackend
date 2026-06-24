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

#if false
using System;
using System.Numerics;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Core;
using Nomad.Core.CVars;
using Nomad.Core.Exceptions;
using Nomad.Core.Logger;
using Nomad.CVars;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Repositories {
	/*
	===================================================================================

	FMODChannelRepository

	===================================================================================
	*/
	/// <summary>
	/// High-level FMOD channel orchestration. Allocation policy, priority updates, and
	/// FMOD instance lifecycle live here, while low-level channel storage and lookups
	/// are delegated to <see cref="FMODChannelTracker"/>.
	/// </summary>

	internal sealed class FMODChannelRepository : IChannelRepository {
		public FMODBusRepository BusRepository => _busRepository;
		private readonly FMODBusRepository _busRepository;

		private readonly FMODChannelTracker _channelTracker;
		private readonly ILoggerCategory _category;
		private readonly IResourceCacheService<IAudioResource, string> _eventRepository;
		private readonly IListenerService _listenerService;
		private readonly FMODPriorityCalculator _priorityCalculator;
		private readonly FMODChannelStealCalculator _channelStealCalculator;

		private readonly ICVar<int> _maxChannels;
		private readonly ICVar<int> _maxActiveChannels;

		private float _effectsVolume = 0.0f;
		private float _minTimeBetweenChannelSteals = 0.1f;

		private bool _isDisposed = false;

		/*
		===============
		FMODChannelRepository
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="listenerService"></param>
		/// <param name="fmodSystem"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="CVarMissing"></exception>
		public FMODChannelRepository( ILoggerService logger, ICVarSystemService cvarSystem, IListenerService listenerService, FMODDevice fmodSystem ) {
			_listenerService = listenerService ?? throw new ArgumentNullException( nameof( listenerService ) );
			_category = logger.CreateCategory( "FMODChannelAllocator", LogLevel.Info, true );

			_priorityCalculator = new FMODPriorityCalculator( cvarSystem, listenerService );
			_channelStealCalculator = new FMODChannelStealCalculator( cvarSystem, _priorityCalculator );

			_maxChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_CHANNELS );
			_maxActiveChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_ACTIVE_CHANNELS );
			_maxChannels.Value = _maxActiveChannels.Value;
			_maxActiveChannels.ValueChanged.Subscribe( OnMaxActiveChannelsValueChanged );
			ValidateMaxChannels();
			InitConfig( cvarSystem );

			_eventRepository = fmodSystem.EventRepository;
			_busRepository = new FMODBusRepository( fmodSystem );

			_channelTracker = new FMODChannelTracker( _maxChannels.Value, listenerService, _channelStealCalculator ) {
				MinTimeBetweenChannelSteals = _minTimeBetweenChannelSteals
			};
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose() {
			if ( !_isDisposed ) {
				_channelTracker?.Dispose();
				_channelStealCalculator?.Dispose();
				_priorityCalculator?.Dispose();
				_category?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		AllocateChannel
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="config"></param>
		/// <param name="basePriority"></param>
		/// <param name="isEssential"></param>
		/// <returns></returns>
		public FMODChannelHandle? AllocateChannel( string id, Vector3 position, SoundCategory config, float basePriority = 0.5f, bool isEssential = false ) {
			var instance = CreateSoundInstance( id );
			float actualPriority = CalculateActualPriority( id, position, basePriority, config );

			EnforceCategoryLimits();

			if ( !_channelTracker.TryAllocateChannel( id, config, basePriority, actualPriority, isEssential, out FMODChannel channel ) ) {
				_category.PrintError( $"Couldn't allocate a channel for sound event '{id}'!" );
				return null;
			}

			try {
				instance.CreateInstance( out FMODChannelResource eventInstance );
				eventInstance.Volume = _effectsVolume / 10.0f;
				eventInstance.Position = position;

				FMODValidator.ValidateCall( eventInstance.SetFinishedCallback( SoundFinishedCallback ) );
				_channelTracker.AttachInstance( channel, eventInstance );
				FMODValidator.ValidateCall( eventInstance.Start() );
				_channelTracker.RecordPlayback( id );
				return channel.Handle;
			} catch {
				_channelTracker.StopChannel( channel, false );
				throw;
			}
		}

		/*
		===============
		Update
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Update( float deltaTime ) {
			_channelTracker.Update( deltaTime );
			UpdatePriorities();
		}

		/*
		===============
		TryGetChannel
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		public bool TryGetChannel( FMODChannelHandle? handle, out FMODChannel channel ) {
			return _channelTracker.TryGetChannel( handle, out channel );
		}

		public bool IsAlive( FMODChannelHandle handle ) {
			return _channelTracker.IsAlive( handle );
		}

		/*
		===============
		UpdatePriorities
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void UpdatePriorities() {
			Vector3 listenerPos = _listenerService.ActiveListener;

			for ( int i = 0; i < _channelTracker.ActiveChannelCount; i++ ) {
				FMODChannel channel = _channelTracker.GetActiveChannelAt( i );
				if ( !channel.IsPlaying || channel.Category == null ) {
					continue;
				}

				float distanceFactor = _priorityCalculator.CalculateDistanceFactor( Vector3.Distance( channel.Instance.Position, listenerPos ) );
				channel.Volume = distanceFactor;
				channel.CurrentPriority = channel.BasePriority * channel.Category.Config.PriorityScale * distanceFactor;
			}
		}

		/*
		===============
		EnforceCategoryLimits
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void EnforceCategoryLimits() {
			foreach ( var category in _busRepository.Categories ) {
				while ( _channelTracker.CountSoundsInCategory( category.Key ) > category.Value.Config.MaxSimultaneous ) {
					if ( !_channelTracker.TryGetLowestPriorityChannelInCategory( category.Value.Name, out FMODChannel channel ) ) {
						break;
					}
					_channelTracker.StopChannel( channel, true );
				}
			}
		}

		/*
		===============
		CalculateActualPriority
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="basePriority"></param>
		/// <param name="category"></param>
		/// <returns></returns>
		private float CalculateActualPriority( string id, Vector3 position, float basePriority, SoundCategory category ) {
			float? lastTimePlayed = null;
			int consecutiveStealCount = 0;

			if ( _channelTracker.TryGetPlaybackStatistics( id, out float value, out int stealCount ) ) {
				lastTimePlayed = value;
				consecutiveStealCount = stealCount;
			}

			return _priorityCalculator.CalculateActualPriority(
				_channelTracker.ElapsedSeconds,
				position,
				basePriority,
				category,
				lastTimePlayed,
				consecutiveStealCount
			);
		}

		/*
		===============
		CreateSoundInstance
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private FMODEventResource CreateSoundInstance( string id ) {
			var cached = _eventRepository.GetCached( id ) ?? throw new Exception( $"Couldn't find event description for '{id}'" );
			cached.Get( out IAudioResource description );

			if ( description is not FMODEventResource eventResource ) {
				throw new InvalidCastException();
			}

			return eventResource;
		}

		/*
		===============
		SoundFinishedCallback
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="type"></param>
		/// <param name="instance"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		private FMOD.RESULT SoundFinishedCallback( FMOD.Studio.EVENT_CALLBACK_TYPE type, nint instance, IntPtr parameters ) {
			_channelTracker.QueueFinishedInstance( instance );
			return FMOD.RESULT.OK;
		}

		/*
		===============
		InitConfig
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="cvarSystem"></param>
		/// <exception cref="CVarMissing"></exception>
		private void InitConfig( ICVarSystemService cvarSystem ) {
			var effectsVolume = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.EFFECTS_VOLUME );
			_effectsVolume = effectsVolume.Value;
			effectsVolume.ValueChanged.Subscribe( OnEffectsVolumeChanged );

			var minTimeBetweenChannelSteals = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.MIN_TIME_BETWEEN_CHANNEL_STEALS );
			_minTimeBetweenChannelSteals = minTimeBetweenChannelSteals.Value;
			minTimeBetweenChannelSteals.ValueChanged.Subscribe( OnMinTimeBetweenChannelStealsValueChanged );
		}

		/*
		===============
		UpdateChannelVolumes
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private static void UpdateChannelVolumes() {
		}

		/*
		===============
		ValidateMaxChannels
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void ValidateMaxChannels() {
			if ( _maxActiveChannels.Value > _maxChannels.Value ) {
				_category.PrintError( "ValidateMaxChannels: maxActiveChannels cannot be larger than maxChannels, resetting both." );
				_maxActiveChannels.Reset();
				_maxChannels.Reset();
			}
		}

		/*
		===============
		OnMaxActiveChannelsValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMaxActiveChannelsValueChanged( in CVarValueChangedEventArgs<int> args ) {
			_maxChannels.Value = args.NewValue;
			ValidateMaxChannels();
		}

		/*
		===============
		OnEffectsVolumeChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnEffectsVolumeChanged( in CVarValueChangedEventArgs<float> args ) {
			_effectsVolume = args.NewValue;
			if ( args.OldValue != args.NewValue ) {
				UpdateChannelVolumes();
			}
		}

		/*
		===============
		OnMinTimeBetweenChannelStealsValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnMinTimeBetweenChannelStealsValueChanged( in CVarValueChangedEventArgs<float> args ) {
			_minTimeBetweenChannelSteals = args.NewValue;
			_channelTracker.MinTimeBetweenChannelSteals = args.NewValue;
		}
	};
};
#endif
