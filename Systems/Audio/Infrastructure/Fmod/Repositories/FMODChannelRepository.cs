/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Godot;
using NomadCore.Domain.Events;
using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.Audio.Application.Interfaces;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using NomadCore.Utilities;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories {
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
		private readonly Dictionary<InternString, SoundCategory> _categories = new Dictionary<InternString, SoundCategory>();
		private readonly Dictionary<EventId, float> _lastPlayTimes = new Dictionary<EventId, float>();
		private readonly Dictionary<EventId, int> _consecutiveStealCounts = new Dictionary<EventId, int>();

		private bool _shouldDecay = false;

		private const float TIME_PENALITY_MULTIPLIER = 0.5f;
		private const float DISTANCE_WEIGHT = 0.3f;
		private const float VOLUME_WEIGHT = 0.2f;
		private const float FREQUENCY_PENALTY = 0.4f;
		private const float MIN_TIME_BETWEEN_STEALS = 0.1f;

		private int _maxChannels;
		private float _distanceFalloffStart = 50.0f;
		private float _distanceFalloffEnd = 100.0f;

		private readonly IResourceCacheService<IEventResource, EventId> _eventRepository;
		private readonly FMODGuidRepository _guidRepository;
		private readonly ILoggerService _logger;
		private readonly IListenerService _listenerService;

		private readonly ObjectPool<FMODChannel> _channelPool = new ObjectPool<FMODChannel>();

		private float _effectsVolume = 0.0f;

		/*
		===============
		FMODChannelRepository
		===============
		*/
		public FMODChannelRepository( ILoggerService logger, ICVarSystemService cvarSystem, IListenerService listenerService,
			IResourceCacheService<IEventResource, EventId> eventRepository, FMODGuidRepository guidRepository )
		{
			_logger = logger;
			_listenerService = listenerService;

			var maxActiveChannels = cvarSystem.GetCVar<int>( "audio.MaxActiveChannels" ) ?? throw new Exception( "Missing CVar 'audio.MaxActiveChannels'" );
			var maxChannels = cvarSystem.GetCVar<int>( "audio.MaxChannels" ) ?? throw new Exception( "Missing CVar 'audio.MaxChannels'" );

			if ( maxActiveChannels.Value > maxChannels.Value ) {
				_logger.PrintError( $"FMODChannelRepository: maxActiveChannels cannot be larger than maxChannels, resetting both." );
				maxActiveChannels.Reset();
				maxChannels.Reset();
			}

			var effectsVolume = cvarSystem.GetCVar<float>( "audio.EffectsVolume" ) ?? throw new Exception( "Missing CVar 'audio.EffectsVolume'" );
			effectsVolume.ValueChanged.Subscribe( this, OnEffectsVolumeChanged );
			_effectsVolume = effectsVolume.Value;

			var distanceFalloffStart = cvarSystem.GetCVar<float>( "audio.DistanceFalloffStart" ) ?? throw new Exception( "Missing CVar 'audio.DistanceFalloffStart'" );
			var distanceFalloffEnd = cvarSystem.GetCVar<float>( "audio.DistanceFalloffEnd" ) ?? throw new Exception( "Missing CVar 'audio.DistanceFalloffEnd'" );

			_distanceFalloffStart = distanceFalloffStart.Value;
			_distanceFalloffEnd = distanceFalloffEnd.Value;

			_maxChannels = maxActiveChannels.Value;
			_allocatedChannels = new List<FMODChannel>( _maxChannels );
			_freeChannelIds = new Queue<int>( _maxChannels );

			for ( int i = 0; i < _maxChannels; i++ ) {
				_freeChannelIds.Enqueue( i );
			}
			_eventRepository = eventRepository;
			_guidRepository = guidRepository;

			maxActiveChannels.ValueChanged.Subscribe( this, OnMaxChannelsValueChanged );

			_categories[ StringPool.Intern( "SoundCategory:UI" ) ] = new SoundCategory {
				Name = StringPool.Intern( "SoundCategory:UI" ),
				MaxSimultaneous = 4,
				PriorityScale = 1.5f,
				StealProtectionTime = 0.2f,
				AllowStealingFromSameCategory = false
			};
		}

		public void Dispose() {
			_allocatedChannels.Clear();
			_freeChannelIds.Clear();
			_categories.Clear();
			_lastPlayTimes.Clear();
			_consecutiveStealCounts.Clear();
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
			ReadOnlySpan<char> category = "SoundCategory:Default", float basePriority = 0.5f,
			bool isEssential = false )
		{
			var categoryName = StringPool.Intern( category );
			if ( !_categories.TryGetValue( categoryName, out var config ) ) {
				config = new SoundCategory { Name = categoryName };
				_categories[ categoryName ] = config;
				GD.Print( "Adding category..." );
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
			if ( _listenerService.ActiveListener == null ) {
				return;
			}
			Vector2 listenerPos = _listenerService.ActiveListener.Position;

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
		OnMaxChannelsValueChanged
		===============
		*/
		private void OnMaxChannelsValueChanged( in CVarValueChangedEventData<int> args ) {
			_maxChannels = args.Value;
		}

		/*
		===============
		OnMaxChannelsValueChanged
		===============
		*/
		private void OnEffectsVolumeChanged( in CVarValueChangedEventData<float> args ) {
			_effectsVolume = args.Value;
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
			float distance = 0.0f;
			if ( _listenerService.ActiveListener != null ) {
				Vector2 listenerPos = _listenerService.ActiveListener.Position;
				distance = position.DistanceTo( listenerPos );
			}
			float distanceFactor = CalculateDistanceFactor( distance );

			// prevent spamming
			float timePenalty = CalculateTimePenalty( id );

			float frequencyPenalty = CalculateFrequencyPenalty( id );

			float categoryMultiplier = category.PriorityScale;

			float priority = basePriority * categoryMultiplier * distanceFactor;
			priority *= 1.0f - timePenalty * TIME_PENALITY_MULTIPLIER;
			priority *= 1.0f - frequencyPenalty * FREQUENCY_PENALTY;

			return Mathf.Clamp( priority, 0.01f, 1.0f );
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
			return Mathf.Clamp( stealCount * 0.1f, 0.0f, 0.5f );
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
				if ( currentTime - sound.LastStolenTime < MIN_TIME_BETWEEN_STEALS ) {
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
			float distance = 0.0f;
			if ( _listenerService.ActiveListener != null ) {
				Vector2 listenerPos = _listenerService.ActiveListener.Position;
				distance = candidate.Position.DistanceTo( listenerPos );
			}

			float priorityDiff = newPriority - candidate.CurrentPriority;
			float ageFactor = Mathf.Min( candidate.Age / 5.0f, 1.0f );
			float distanceFactor = 1.0f - CalculateDistanceFactor( distance );
			float volumeFactor = 1.0f - candidate.Volume;

			float score =
				priorityDiff * 2.0f +
				ageFactor * 0.5f +
				distanceFactor * DISTANCE_WEIGHT +
				volumeFactor * VOLUME_WEIGHT;

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
				stealCount = Mathf.Min( 0, stealCount - 1 );
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
				channel.Instance.stop( wasStolen ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT );
				channel.Instance.release();
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

			if ( description is not FMODEventResource eventResource ) {
				throw new InvalidCastException();
			}

			FMODValidator.ValidateCall( eventResource.Handle.createInstance( out var instance ) );

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
		private FMOD.RESULT SoundFinishedCallback( FMOD.Studio.EVENT_CALLBACK_TYPE type, nint instance, IntPtr parameters ) {
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
		private int CountSoundsInCategory( InternString category ) {
			int count = 0;
			for ( int i = 0; i < _allocatedChannels.Count; i++ ) {
				var channel = _allocatedChannels[ i ];
				if ( channel.Category.Name == category && channel.IsPlaying ) {
					count++;
				}
			}
			return count;
		}
	};
};