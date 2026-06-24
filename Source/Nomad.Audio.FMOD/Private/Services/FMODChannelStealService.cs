#if false
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Interfaces;
using Nomad.Audio.Fmod.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Repositories {
	// Add a matching overload on FMODPriorityCalculator:
	//
	// float CalculateActualPriority(
	//     float startTime,
	//     string id,
	//     Vector3 position,
	//     float basePriority,
	//     SoundCategory category,
	//     Func<string, (bool found, float value)> lastPlayLookup,
	//     Func<string, int> consecutiveStealLookup )
	//
	// That keeps the calculator authoritative while letting the repository stop
	// feeding it Dictionary<string, ...> instances.

	internal sealed class FMODChannelStealService {
		private readonly IListenerService _listenerService;
		private readonly FMODPriorityCalculator _priorityCalculator;
		private readonly FMODChannelStealCalculator _channelStealCalculator;

		private readonly StringFloatMap _lastPlayTimes;
		private readonly StringIntMap _consecutiveStealCounts;

		private bool _shouldDecay;
		private float _minTimeBetweenChannelSteals;

		public FMODChannelStealService(
			IListenerService listenerService,
			FMODPriorityCalculator priorityCalculator,
			FMODChannelStealCalculator channelStealCalculator,
			int expectedUniqueEventCount = 1024
		) {
			_listenerService = listenerService;
			_priorityCalculator = priorityCalculator;
			_channelStealCalculator = channelStealCalculator;
			_lastPlayTimes = new StringFloatMap( expectedUniqueEventCount );
			_consecutiveStealCounts = new StringIntMap( expectedUniqueEventCount );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetMinTimeBetweenChannelSteals( float value ) {
			_minTimeBetweenChannelSteals = value;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UpdateLastPlayTime( float startTime, string eventId ) {
			_lastPlayTimes.Set( eventId, startTime );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UpdateStealStatistics( string stolenEventId ) {
			_consecutiveStealCounts.Increment( stolenEventId );
			_shouldDecay = true;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryGetLastPlayTime( string eventId, out float lastPlayTime ) {
			return _lastPlayTimes.TryGetValue( eventId, out lastPlayTime );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public int GetConsecutiveStealCount( string eventId ) {
			return _consecutiveStealCounts.GetValueOrDefault( eventId );
		}

		public float CalculateActualPriority(
			float startTime,
			string eventId,
			Vector3 position,
			float basePriority,
			SoundCategory category ) {
			return _priorityCalculator.CalculateActualPriority(
				startTime,
				position,
				basePriority,
				category,
				_lastPlayTimes.TryGetValue( eventId, out float value ) ? value : null,
				_consecutiveStealCounts.GetValueOrDefault( eventId ) );
		}

		public void DecayStealCountsIfNeeded() {
			if ( !_shouldDecay ) {
				return;
			}

			_shouldDecay = false;
			_consecutiveStealCounts.DecayAllPositiveValuesByOne();
		}

		public void UpdatePriorities( FMODChannelTracker tracker ) {
			Vector3 listenerPosition = _listenerService.ActiveListener;

			for ( int i = 0; i < tracker.ActiveCount; i++ ) {
				FMODChannel channel = tracker.GetActiveChannel( i );

				if ( !channel.IsPlaying ) {
					continue;
				}

				float distanceFactor = _priorityCalculator.CalculateDistanceFactor(
					Vector3.Distance( channel.Instance.Position, listenerPosition ) );

				channel.Volume = distanceFactor;
				channel.CurrentPriority = channel.BasePriority * channel.Category.Config.PriorityScale * distanceFactor;
			}
		}

		public bool TryFindStealCandidate(
			float currentTime,
			float incomingPriority,
			SoundCategory incomingCategory,
			FMODChannelTracker tracker,
			out FMODChannel? bestCandidate
		) {
			bestCandidate = null;
			float bestStealScore = float.MinValue;

			for ( int i = 0; i < tracker.ActiveCount; i++ ) {
				FMODChannel sound = tracker.GetActiveChannel( i );

				if ( !sound.IsPlaying || sound.IsEssential ) {
					continue;
				}

				if ( sound.AgeSeconds( currentTime ) < sound.Category.Config.StealProtectionTime ) {
					continue;
				}

				if ( currentTime - sound.LastStolenTime < _minTimeBetweenChannelSteals ) {
					continue;
				}

				float stealScore = _channelStealCalculator.CalculateStealScore(
					currentTime,
					_listenerService.ActiveListener,
					sound,
					incomingPriority,
					incomingCategory );

				if ( stealScore > bestStealScore ) {
					bestStealScore = stealScore;
					bestCandidate = sound;
				}
			}

			return bestCandidate != null && bestStealScore > 0.0f;
		}

		private sealed class StringFloatMap {
			private string?[] _keys;
			private float[] _values;
			private int _mask;
			private int _count;

			public StringFloatMap( int capacity ) {
				int size = NextPowerOfTwo( Math.Max( 8, capacity ) );
				_keys = new string?[size];
				_values = new float[size];
				_mask = size - 1;
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public bool TryGetValue( string key, out float value ) {
				int index = FindIndex( key );
				if ( index < 0 ) {
					value = 0.0f;
					return false;
				}

				value = _values[index];
				return true;
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public void Set( string key, float value ) {
				int index = GetOrCreateIndex( key );
				_values[index] = value;
			}

			private int GetOrCreateIndex( string key ) {
				if ( ( _count + 1 ) * 2 >= _keys.Length ) {
					Resize( _keys.Length << 1 );
				}

				int index = Hash( key ) & _mask;
				while ( true ) {
					string? existing = _keys[index];

					if ( existing == null ) {
						_keys[index] = key;
						_values[index] = 0.0f;
						_count++;
						return index;
					}

					if ( existing == key || StringComparer.Ordinal.Equals( existing, key ) ) {
						return index;
					}

					index = ( index + 1 ) & _mask;
				}
			}

			private int FindIndex( string key ) {
				int index = Hash( key ) & _mask;
				while ( true ) {
					string? existing = _keys[index];
					if ( existing == null ) {
						return -1;
					}

					if ( existing == key || StringComparer.Ordinal.Equals( existing, key ) ) {
						return index;
					}

					index = ( index + 1 ) & _mask;
				}
			}

			private void Resize( int newSize ) {
				string?[] oldKeys = _keys;
				float[] oldValues = _values;

				_keys = new string?[newSize];
				_values = new float[newSize];
				_mask = newSize - 1;
				_count = 0;

				for ( int i = 0; i < oldKeys.Length; i++ ) {
					string? key = oldKeys[i];
					if ( key == null ) {
						continue;
					}

					int index = GetOrCreateIndex( key );
					_values[index] = oldValues[i];
				}
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			private static int Hash( string key ) {
				return StringComparer.Ordinal.GetHashCode( key );
			}
		};

		private sealed class StringIntMap {
			private string?[] _keys;
			private int[] _values;
			private int _mask;
			private int _count;

			public StringIntMap( int capacity ) {
				int size = NextPowerOfTwo( Math.Max( 8, capacity ) );
				_keys = new string?[size];
				_values = new int[size];
				_mask = size - 1;
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public int GetValueOrDefault( string key ) {
				int index = FindIndex( key );
				return index >= 0 ? _values[index] : 0;
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			public void Increment( string key ) {
				int index = GetOrCreateIndex( key );
				_values[index]++;
			}

			public void DecayAllPositiveValuesByOne() {
				for ( int i = 0; i < _keys.Length; i++ ) {
					if ( _keys[i] != null && _values[i] > 0 ) {
						_values[i]--;
					}
				}
			}

			private int GetOrCreateIndex( string key ) {
				if ( ( _count + 1 ) * 2 >= _keys.Length ) {
					Resize( _keys.Length << 1 );
				}

				int index = Hash( key ) & _mask;
				while ( true ) {
					string? existing = _keys[index];

					if ( existing == null ) {
						_keys[index] = key;
						_values[index] = 0;
						_count++;
						return index;
					}

					if ( existing == key || StringComparer.Ordinal.Equals( existing, key ) ) {
						return index;
					}

					index = ( index + 1 ) & _mask;
				}
			}

			private int FindIndex( string key ) {
				int index = Hash( key ) & _mask;
				while ( true ) {
					string? existing = _keys[index];
					if ( existing == null ) {
						return -1;
					}

					if ( existing == key || StringComparer.Ordinal.Equals( existing, key ) ) {
						return index;
					}

					index = ( index + 1 ) & _mask;
				}
			}

			private void Resize( int newSize ) {
				string?[] oldKeys = _keys;
				int[] oldValues = _values;

				_keys = new string?[newSize];
				_values = new int[newSize];
				_mask = newSize - 1;
				_count = 0;

				for ( int i = 0; i < oldKeys.Length; i++ ) {
					string? key = oldKeys[i];
					if ( key == null ) {
						continue;
					}

					int index = GetOrCreateIndex( key );
					_values[index] = oldValues[i];
				}
			}

			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			private static int Hash( string key ) {
				return StringComparer.Ordinal.GetHashCode( key );
			}
		};

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static int NextPowerOfTwo( int value ) {
			value--;
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			value++;
			return value < 8 ? 8 : value;
		}
	}
}
#endif
