/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Godot;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Exceptions;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;
using Nomad.Core;
using Nomad.Core.Exceptions;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.CVars;
using Nomad.ResourceCache;
using NomadCore.Utilities;
using System;
using System.Collections.Generic;

namespace Nomad.Audio.Fmod.Private.Repositories {
	/*
	===================================================================================

	FMODChannelRepository

	FIXME: this violates SOLID

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODChannelRepository : IChannelRepository {
		private const int INVALID_CHANNEL = -1;

		// TODO: add an index hash for faster operations

		private readonly List<FMODChannel> _allocatedChannels;
		private readonly Queue<int> _freeChannelIds;
		private readonly Dictionary<EventId, float> _lastPlayTimes = new Dictionary<EventId, float>();
		private readonly Dictionary<EventId, int> _consecutiveStealCounts = new Dictionary<EventId, int>();

		private readonly FMODBusRepository _busRepository;

		private bool _shouldDecay = false;
		private float _timePenaltyMultiplier = 0.5f;
		private float _distanceWeight = 0.3f;
		private float _volumeWeight = 0.2f;
		private float _frequencyPenalty = 0.4f;
		private float _minTimeBetweenChannelSteals = 0.1f;

		private int _maxChannels = 0;
		private float _distanceFalloffStart = 50.0f;
		private float _distanceFalloffEnd = 100.0f;
		private float _effectsVolume = 0.0f;

		private readonly IResourceCacheService<FMODEventResource, EventId> _eventRepository;
		private readonly FMODGuidRepository _guidRepository;
		private readonly ILoggerService _logger;
		private readonly IListenerService _listenerService;

		private readonly ObjectPool<FMODChannel> _channelPool = new ObjectPool<FMODChannel>();

		/*
		===============
		FMODChannelRepository
		===============
		*/
		public FMODChannelRepository( ILoggerService logger, ICVarSystemService cvarSystem, IListenerService listenerService,
			IResourceCacheService<FMODEventResource, EventId> eventRepository, FMODGuidRepository guidRepository, FMODBusRepository busRepository )
		{
			_logger = logger;
			_listenerService = listenerService;
			_busRepository = busRepository;

			InitConfig( cvarSystem );

			_allocatedChannels = new List<FMODChannel>( _maxChannels );
			_freeChannelIds = new Queue<int>( _maxChannels );

			for ( int i = 0; i < _maxChannels; i++ ) {
				_freeChannelIds.Enqueue( i );
			}
			_eventRepository = eventRepository;
			_guidRepository = guidRepository;
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_freeChannelIds.Clear();
			_lastPlayTimes.Clear();
			_consecutiveStealCounts.Clear();
			_busRepository.Dispose();
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
		/// <param name="category"></param>
		/// <param name="basePriority"></param>
		/// <param name="isEssential"></param>
		/// <returns></returns>
		public FMODChannel? AllocateChannel( EventId id, Vector2 position,
			string category = "SoundCategory:Default", float basePriority = 0.5f,
			bool isEssential = false )
		{

			if ( !_categories.TryGetValue( categoryName, out var config ) ) {
				throw new MissingChannelCategory( category );
			}

			int soundsInCategory = CountSoundsInCategory( categoryName );
			if ( soundsInCategory >= config.MaxSimultaneous ) {
				if ( !config.AllowStealingFromSameCategory ) {
					// not allowed to steal from ourselves, most unfortunate :(
					return null;
				}
			}

			float actualPriority = CalculateActualPriority( id, position, basePriority, config );
			int channelId = AllocateChannel( id, position, actualPriority, config, isEssential );

			if ( channelId == INVALID_CHANNEL ) {
				_logger.PrintError( $"Couldn't allocate a channel for sound event '{id.Name}'!" );
				return null;
			}

			var instance = CreateSoundInstance( id, position, channelId );

			var channel = _channelPool.Rent();
			channel.Instance = instance;
			channel.Path = id;
			channel.Category = config;
			channel.BasePriority = basePriority;
			channel.CurrentPriority = actualPriority;
			channel.StartTime = Time.GetTicksMsec() / 1000.0f;
			channel.Position = position;
			channel.IsEssential = isEssential;
			channel.ChannelId = channelId;

			_allocatedChannels.Add( channel );
			UpdateLastPlayTime( id );

			return channel;
		}

		/*
		===============
		UpdateLastPlayTime
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		public void UpdateLastPlayTime( EventId id ) {
			_lastPlayTimes[ id ] = Time.GetTicksMsec() / 1000.0f;
		}

		/*
		===============
		Update
		===============
		*/
		public void Update( float deltaTime ) {
			if ( _shouldDecay ) {
				DecayStealCounts();
				_shouldDecay = false;
			}
			UpdatePriorities();
			CleanupFinishedSounds();
			EnforceCategoryLimits();
		}

		/*
		===============
		UpdatePriorities
		===============
		*/
		private void UpdatePriorities() {
			Vector2 listenerPos = _listenerService.ActiveListener;

			for ( int i = 0; i < _allocatedChannels.Count; i++ ) {
				var channel = _allocatedChannels[ i ];

				if ( !channel.IsPlaying ) {
					continue;
				}

				float distanceFactor = CalculateDistanceFactor( channel.Position.DistanceTo( listenerPos ) );
				channel.Volume = distanceFactor;
				channel.CurrentPriority = channel.BasePriority * channel.Category.PriorityScale * distanceFactor;
			}
		}

		/*
		===============
		CleanupFinishedSounds
		===============
		*/
		private void CleanupFinishedSounds() {
			for ( int i = _allocatedChannels.Count - 1; i >= 0; i-- ) {
				if ( !_allocatedChannels[ i ].IsPlaying ) {
					StopSound( _allocatedChannels[ i ], false );
				}
			}
		}

		/*
		===============
		EnforceCategoryLimits
		===============
		*/
		private void EnforceCategoryLimits() {
			foreach ( var category in _categories ) {
				int count = CountSoundsInCategory( category.Key );
				if ( count > category.Value.MaxSimultaneous ) {
					// find the lowest priority sound in this category to steal
					StealFromCategory( category.Key );
				}
			}
		}

		/*
		===============
		StealFromCategory
		===============
		*/
		private void StealFromCategory( InternString category ) {
			FMODChannel? lowestPriority = null;
			float lowestPriorityValue = float.MaxValue;

			for ( int i = 0; i < _allocatedChannels.Count; i++ ) {
				var channel = _allocatedChannels[ i ];
				if ( channel.Category.Name == category && channel.IsPlaying && !channel.IsEssential ) {
					if ( channel.CurrentPriority < lowestPriorityValue ) {
						lowestPriorityValue = channel.CurrentPriority;
						lowestPriority = channel;
					}
				}
			}
			if ( lowestPriority != null ) {
				StopSound( lowestPriority, true );
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
		private float CalculateActualPriority( EventId id, Vector2 position, float basePriority, SoundCategory category ) {
			float distance = position.DistanceTo( _listenerService.ActiveListener );
			float distanceFactor = CalculateDistanceFactor( distance );

			// prevent spamming
			float timePenalty = CalculateTimePenalty( id );

			float frequencyPenalty = CalculateFrequencyPenalty( id );

			float categoryMultiplier = category.PriorityScale;

			float priority = basePriority * categoryMultiplier * distanceFactor;
			priority *= 1.0f - timePenalty * _timePenaltyMultiplier;
			priority *= 1.0f - frequencyPenalty * _frequencyPenalty;

			return Math.Clamp( priority, 0.01f, 1.0f );
		}


		/*
		===============
		CalculateDistanceFactor
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="distance"></param>
		/// <returns></returns>
		private float CalculateDistanceFactor( float distance ) {
			if ( distance <= _distanceFalloffStart ) {
				return 1.0f;
			}
			if ( distance >= _distanceFalloffEnd ) {
				return 0.1f;
			}

			// TODO: precompute

			// LERP it
			float t = ( distance - _distanceFalloffStart ) / ( _distanceFalloffEnd / _distanceFalloffStart );
			return 1.0f - t * 0.5f;
		}

		/*
		===============
		CalculateTimePenalty
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private float CalculateTimePenalty( EventId id ) {
			if ( !_lastPlayTimes.TryGetValue( id, out float lastTime ) ) {
				return 0.0f;
			}
			float timeSinceLast = Time.GetTicksMsec() / 1000.0f - lastTime;
			float protectionTime = 0.5f;

			// less penality if enough time has passed
			if ( timeSinceLast > protectionTime ) {
				return 0.0f;
			}
			return 1.0f - ( timeSinceLast / protectionTime );
		}

		/*
		===============
		CalculateFrequencyPenalty
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private float CalculateFrequencyPenalty( EventId id ) {
			if ( !_consecutiveStealCounts.TryGetValue( id, out int stealCount ) ) {
				return 0.0f;
			}
			// reduce priority if this sound keeps getting stolen
			return Math.Clamp( stealCount * 0.1f, 0.0f, 0.5f );
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
		/// <param name="priority"></param>
		/// <param name="category"></param>
		/// <param name="isEssential"></param>
		/// <returns></returns>
		private int AllocateChannel( EventId id, Vector2 position, float priority, SoundCategory category, bool isEssential ) {
			if ( _freeChannelIds.Count > 0 ) {
				return _freeChannelIds.Dequeue();
			}
			return StealChannel( id, position, priority, category, isEssential );
		}

		/*
		===============
		StealChannel
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="position"></param>
		/// <param name="priority"></param>
		/// <param name="category"></param>
		/// <param name="isEssential"></param>
		/// <returns></returns>
		private int StealChannel( EventId id, Vector2 position, float priority, SoundCategory category, bool isEssential ) {
			float bestStealScore = float.MinValue;
			FMODChannel? bestCandidate = null;
			float currentTime = Time.GetTicksMsec() / 1000.0f;

			for ( int i = 0; i < _allocatedChannels.Count; i++ ) {
				var sound = _allocatedChannels[ i ];
				if ( !sound.IsPlaying || sound.IsEssential ) {
					continue;
				}

				// check steal protection (can't steal brand new sounds)
				if ( sound.Age < sound.Category.StealProtectionTime ) {
					continue;
				}

				// don't steal if recently stolen (prevents thrashing)
				if ( currentTime - sound.LastStolenTime < _minTimeBetweenChannelSteals ) {
					continue;
				}

				// calc steal score
				float stealScore = CalculateStealScore( sound, priority, category );
				if ( stealScore > bestStealScore ) {
					bestStealScore = stealScore;
					bestCandidate = sound;
				}
			}
			if ( bestCandidate != null && bestStealScore > 0.0f ) {
				int stolenChannelId = bestCandidate.ChannelId;
				StopSound( bestCandidate, true );

				UpdateStealStatistics( bestCandidate.Path );

				return stolenChannelId;
			}
			return -1;
		}

		/*
		===============
		CalculateStealScore
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="candidate"></param>
		/// <param name="newPriority"></param>
		/// <param name="category"></param>
		/// <returns></returns>
		private float CalculateStealScore( FMODChannel candidate, float newPriority, SoundCategory category ) {
			float distance = candidate.Position.DistanceTo( _listenerService.ActiveListener );
			float priorityDiff = newPriority - candidate.CurrentPriority;
			float ageFactor = Math.Min( candidate.Age / 5.0f, 1.0f );
			float distanceFactor = 1.0f - CalculateDistanceFactor( distance );
			float volumeFactor = 1.0f - candidate.Volume;

			float score =
				priorityDiff * 2.0f +
				ageFactor * 0.5f +
				distanceFactor * _distanceWeight +
				volumeFactor * _volumeWeight;

			score *= category.Name == candidate.Category.Name ? 0.5f : 1.0f;

			float timeSinceLastStolen = Time.GetTicksMsec() / 1000.0f - candidate.LastStolenTime;
			if ( timeSinceLastStolen < 1.0f ) {
				score *= timeSinceLastStolen;
			}

			return score;

		}

		/*
		===============
		UpdateStealStatistics
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="stolenEventId"></param>
		private void UpdateStealStatistics( EventId stolenEventId ) {
			if ( !_consecutiveStealCounts.ContainsKey( stolenEventId ) ) {
				_consecutiveStealCounts[ stolenEventId ] = 0;
			}
			_consecutiveStealCounts[ stolenEventId ]++;
			_shouldDecay = true;
		}

		/*
		===============
		DecayStealCounts
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void DecayStealCounts() {
			foreach ( var kvp in _consecutiveStealCounts ) {
				int stealCount = _consecutiveStealCounts[ kvp.Key ];
				stealCount = Math.Min( 0, stealCount - 1 );
				if ( stealCount == 0 ) {
					_consecutiveStealCounts.Remove( kvp.Key );
				}
			}
		}

		/*
		===============
		StopSound
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="wasStolen"></param>
		private void StopSound( FMODChannel channel, bool wasStolen = false ) {
			if ( channel.Instance.isValid() ) {
				FMODValidator.ValidateCall( channel.Instance.stop( wasStolen ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT ) );
				FMODValidator.ValidateCall( channel.Instance.release() );
				channel.Instance.clearHandle();
			}
			if ( wasStolen ) {
				channel.LastStolenTime = Time.GetTicksMsec() / 1000.0f;
			}

			_channelPool.Return( channel );

			// FIXME: this is slow
			_allocatedChannels.Remove( channel );
			_freeChannelIds.Enqueue( channel.ChannelId );
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
		/// <param name="position"></param>
		/// <param name="channelId"></param>
		/// <returns></returns>
		private FMOD.Studio.EventInstance CreateSoundInstance( EventId id, Vector2 position, int channelId ) {
			var cached = _eventRepository.GetCached( id ) ?? throw new Exception( $"Couldn't find event description for '{id.Name}'" );
			cached.Get( out var description );
			FMODValidator.ValidateCall( description.Handle.createInstance( out var instance ) );

			FMOD.ATTRIBUTES_3D attributes = new FMOD.ATTRIBUTES_3D { };
			attributes.position = new FMOD.VECTOR { x = position.X, y = position.Y, z = 0.0f };
			instance.set3DAttributes( attributes );
			FMODValidator.ValidateCall( instance.setVolume( _effectsVolume / 10.0f ) );

			FMODValidator.ValidateCall( instance.start() );
			FMODValidator.ValidateCall( instance.setCallback( SoundFinishedCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED | FMOD.Studio.EVENT_CALLBACK_TYPE.START_FAILED ) );

			return instance;
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
		private FMOD.RESULT SoundFinishedCallback( FMOD.Studio.EVENT_CALLBACK_TYPE type, nint instance, nint parameters ) {
			FMOD.Studio.EventInstance eventInstance = new FMOD.Studio.EventInstance( instance );
			FMODChannel? channel = null;

			for ( int i = 0; i < _allocatedChannels.Count; i++ ) {
				var sound = _allocatedChannels[ i ];
				if ( sound.Instance.handle == eventInstance.handle ) {
					channel = sound;
					break;
				}
			}
			if ( channel != null ) {
				// ensure we unhook the callback (causes a seggy)
				channel.Instance.setCallback( null, FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED | FMOD.Studio.EVENT_CALLBACK_TYPE.START_FAILED );
				StopSound( channel, false );
			}
			return FMOD.RESULT.OK;
		}

		/*
		===============
		CountSoundsInCategory
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="category"></param>
		/// <returns></returns>
		private int CountSoundsInCategory( ReadOnlyMemory<char> category ) {
			int count = 0;
			for ( int i = 0; i < _allocatedChannels.Count; i++ ) {
				var channel = _allocatedChannels[ i ];
				if ( channel.Category.Name == category && channel.IsPlaying ) {
					count++;
				}
			}
			return count;
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
			var effectsVolume = cvarSystem.GetCVar<float>( Constants.CVars.Audio.EFFECTS_VOLUME ) ?? throw new CVarMissing( Constants.CVars.Audio.EFFECTS_VOLUME );
			_effectsVolume = effectsVolume.Value;
			effectsVolume.ValueChanged.Subscribe( this, OnEffectsVolumeChanged );

			var distanceFalloffStart = cvarSystem.GetCVar<float>( Constants.CVars.Audio.DISTANCE_FALLOFF_START ) ?? throw new CVarMissing( Constants.CVars.Audio.DISTANCE_FALLOFF_START );
			_distanceFalloffStart = distanceFalloffStart.Value;
			distanceFalloffStart.ValueChanged.Subscribe( this, OnDistanceFalloffStartValueChanged );

			var distanceFalloffEnd = cvarSystem.GetCVar<float>( Constants.CVars.Audio.DISTANCE_FALLOFF_END ) ?? throw new CVarMissing( Constants.CVars.Audio.DISTANCE_FALLOFF_END );
			_distanceFalloffEnd = distanceFalloffEnd.Value;
			distanceFalloffEnd.ValueChanged.Subscribe( this, OnDistanceFalloffEndValueChanged );

			var minTimeBetweenChannelSteals = cvarSystem.GetCVar<float>( Constants.CVars.Audio.MIN_TIME_BETWEEN_CHANNEL_STEALS ) ?? throw new CVarMissing( Constants.CVars.Audio.MIN_TIME_BETWEEN_CHANNEL_STEALS );
			_minTimeBetweenChannelSteals = minTimeBetweenChannelSteals.Value;
			minTimeBetweenChannelSteals.ValueChanged.Subscribe( this, OnMinTimeBetweenChannelStealsValueChanged );

			var frequencyPenalty = cvarSystem.GetCVar<float>( Constants.CVars.Audio.FREQUENCY_PENALTY ) ?? throw new CVarMissing( Constants.CVars.Audio.FREQUENCY_PENALTY );
			_frequencyPenalty = frequencyPenalty.Value;
			frequencyPenalty.ValueChanged.Subscribe( this, OnFrequencyPenaltyValueChanged );

			var volumeWeight = cvarSystem.GetCVar<float>( Constants.CVars.Audio.VOLUME_PENALTY ) ?? throw new CVarMissing( Constants.CVars.Audio.VOLUME_PENALTY );
			_volumeWeight = volumeWeight.Value;
			volumeWeight.ValueChanged.Subscribe( this, OnVolumeWeightValueChanged );

			var distanceWeight = cvarSystem.GetCVar<float>( Constants.CVars.Audio.DISTANCE_WEIGHT ) ?? throw new CVarMissing( Constants.CVars.Audio.DISTANCE_WEIGHT );
			_distanceWeight = distanceWeight.Value;
			distanceWeight.ValueChanged.Subscribe( this, OnDistanceWeightValueChanged );

			var maxChannels = cvarSystem.GetCVar<int>( Constants.CVars.Audio.MAX_CHANNELS ) ?? throw new CVarMissing( Constants.CVars.Audio.MAX_CHANNELS );

			var maxActiveChannels = cvarSystem.GetCVar<int>( Constants.CVars.Audio.MAX_ACTIVE_CHANNELS ) ?? throw new CVarMissing( Constants.CVars.Audio.MAX_ACTIVE_CHANNELS );
			_maxChannels = maxActiveChannels.Value;
			maxActiveChannels.ValueChanged.Subscribe( this, OnMaxChannelsValueChanged );
			if ( maxActiveChannels.Value > maxChannels.Value ) {
				_logger.PrintError( $"FMODChannelRepository: maxActiveChannels cannot be larger than maxChannels, resetting both." );
				maxActiveChannels.Reset();
				maxChannels.Reset();
			}
		}

		/*
		===============
		OnMaxChannelsValueChanged
		===============
		*/
		private void OnMaxChannelsValueChanged( in CVarValueChangedEventArgs<int> args ) {
			_maxChannels = args.NewValue;
		}

		/*
		===============
		OnMaxChannelsValueChanged
		===============
		*/
		private void OnEffectsVolumeChanged( in CVarValueChangedEventArgs<float> args ) {
			_effectsVolume = args.NewValue;
		}

		/*
		===============
		OnDistanceWeightValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnDistanceWeightValueChanged( in CVarValueChangedEventArgs<float> args ) {
			_distanceWeight = args.NewValue;
		}

		/*
		===============
		OnDistanceFalloffEndValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnDistanceFalloffEndValueChanged( in CVarValueChangedEventArgs<float> args ) {
			_distanceFalloffEnd = args.NewValue;
		}

		/*
		===============
		OnDistanceFalloffStartValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnDistanceFalloffStartValueChanged( in CVarValueChangedEventArgs<float> args ) {
			_distanceFalloffStart = args.NewValue;
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
		}

		/*
		===============
		OnFrequencyPenaltyValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnFrequencyPenaltyValueChanged( in CVarValueChangedEventArgs<float> args ) {
			_frequencyPenalty = args.NewValue;
		}

		/*
		===============
		OnVolumeWeightValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnVolumeWeightValueChanged( in CVarValueChangedEventArgs<float> args ) {
			_volumeWeight = args.NewValue;
		}
	};
};
