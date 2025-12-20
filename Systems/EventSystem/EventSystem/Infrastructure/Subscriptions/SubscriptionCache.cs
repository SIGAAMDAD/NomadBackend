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

using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.EventSystem.Infrastructure.Subscriptions {
	/*
	===================================================================================
	
	SubscriptionCache
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class SubscriptionCache<TArgs, TCallback>( ILoggerService? logger ) : IDisposable
		where TArgs : struct
		where TCallback : class
	{
		public List<WeakSubscription<TArgs, TCallback>> Subscriptions => _subscriptions;
		private readonly List<WeakSubscription<TArgs, TCallback>> _subscriptions = new List<WeakSubscription<TArgs, TCallback>>( 64 );

		private readonly Dictionary<int, List<int>> _indexMap = new Dictionary<int, List<int>>( 64 );

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			_subscriptions.Clear();

			foreach ( var map in _indexMap ) {
				map.Value.Clear();
			}
			_indexMap.Clear();
		}

		/*
		===============
		AddSubscription
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		public void AddSubscription( object subscriber, TCallback callback ) {
			if ( ContainsCallback( subscriber, callback, out _ ) ) {
				logger?.PrintError( $"SubscriptionCache.AddSubscription: duplicate subscription from '{subscriber.GetType().Name}'" );
				return;
			}

			int index = _subscriptions.Count;
			_subscriptions.Add( new WeakSubscription<TArgs, TCallback>( subscriber, callback ) );
#if DEBUG
			logger?.PrintLine( $"SubscriptionCache.AddSubscription: added subscription from '{subscriber.GetType().Name}'" );
#endif

			int key = subscriber.GetHashCode();
			if ( _indexMap.TryGetValue( key, out List<int>? indices ) ) {
				indices.Add( index );
			} else {
				_indexMap[ key ] = new List<int>() { index };
			}
		}

		/*
		===============
		RemoveSubscription
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		public void RemoveSubscription( object subscriber, TCallback callback ) {
			if ( !ContainsCallback( subscriber, callback, out int index ) ) {
				logger?.PrintWarning( $"SubscriptionCache.RemoveSubscription: subscription not found from '{subscriber.GetType().Name}'" );
				return;
			}
#if DEBUG
			logger?.PrintLine( $"SubscriptionCache.RemoveSubscription: removed subscription from '{subscriber.GetType().Name}'" );
#endif
			RemoveAtIndexInternal( index, subscriber );
		}

		/*
		===============
		RemoveAllForSubscriber
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		public void RemoveAllForSubscriber( object subscriber ) {
			ArgumentNullException.ThrowIfNull( subscriber );

			int removed = 0;
			for ( int i = _subscriptions.Count - 1; i >= 0; i-- ) {
				if ( _subscriptions[ i ].Subscriber.TryGetTarget( out object? existingSubscriber ) && existingSubscriber == subscriber ) {
					RemoveAtIndexInternal( i, subscriber );
					removed++;
				}
			}
#if DEBUG
			logger?.PrintLine( $"SubscriptionCache.RemoveAllForSubscriber: removed {removed} subscriptions for '{subscriber.GetType().Name}'" );
#endif
		}

		/*
		===============
		RebuildIndexMap
		===============
		*/
		/// <summary>
		/// Rebuilds the subscription index map from scratch, must be called with the write lock held.
		/// </summary>
		public void RebuildIndexMap() {
			_indexMap.Clear();
			for ( int i = 0; i < _subscriptions.Count; i++ ) {
				if ( _subscriptions[ i ].Subscriber.TryGetTarget( out object? subscriber ) ) {
					int key = subscriber.GetHashCode();
					if ( _indexMap.TryGetValue( key, out List<int>? indices ) ) {
						indices.Add( i );
					} else {
						_indexMap[ key ] = new List<int>() { i };
					}
				}
			}
		}

		/*
		===============
		CleanupDeadSubscriptions
		===============
		*/
		public void CleanupDeadSubscriptions() {
			int initialCount = _subscriptions.Count;
#if DEBUG
			logger?.PrintLine( $"SubscriptionCache({typeof( TCallback )}).CleanupDeadSubscriptionsInternal: cleaning up dead subscriptions..." );
#endif

			for ( int i = _subscriptions.Count - 1; i >= 0; i-- ) {
				if ( !_subscriptions[ i ].IsAlive ) {
					_subscriptions.RemoveAt( i );
				}
			}

			int removed = initialCount - _subscriptions.Count;
			if ( removed > 0 ) {
#if DEBUG
				logger?.PrintLine( $"SubscriptionCache.CleanupDeadSubscriptions: removed {removed} dangling subscriptions" );
#endif
				RebuildIndexMap();
			}
		}

		/*
		===============
		ContainsCallbackInternal
		===============
		*/
		/// <summary>
		/// Internal method that assumes the write lock is already held.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool ContainsCallback( object subscriber, TCallback callback, out int index ) {
			index = -1;
			int key = subscriber.GetHashCode();
			if ( _indexMap.TryGetValue( key, out List<int>? candidates ) ) {
				for ( int i = 0; i < candidates.Count; i++ ) {
					int idx = candidates[ i ];
					if ( idx >= 0 && idx < _subscriptions.Count ) {
						WeakSubscription<TArgs, TCallback> subscription = _subscriptions[ idx ];
						if ( subscription.Subscriber.TryGetTarget( out object? existing ) && existing.Equals( subscriber )
							&& subscription.Callback.TryGetTarget( out TCallback? caller ) && caller.Equals( callback ) )
						{
							index = idx;
							return true;
						}
					}
				}
			}
			return false;
		}

		/*
		===============
		RemoveAtIndexInternal
		===============
		*/
		/// <summary>
		/// Removes a subscription at the given index and updates the index map using swap-and-pop.
		/// Must be called with the write lock held.
		/// </summary>
		private void RemoveAtIndexInternal( int index, object subscriber ) {
			if ( index < 0 || index >= _subscriptions.Count ) {
				logger?.PrintWarning( $"SubscriptionCache({typeof( TCallback )}).RemoveAtIndexInternal: index isn't valid - {index}" );
				return;
			}

			int lastIndex = _subscriptions.Count - 1;
			if ( index != lastIndex ) {
				// good 'ol fashioned swap & pop
				WeakSubscription<TArgs, TCallback> swapped = _subscriptions[ lastIndex ];
				_subscriptions[ index ] = swapped;

				if ( swapped.Subscriber.TryGetTarget( out object? swappedSubscriber ) ) {
					int swappedKey = swappedSubscriber.GetHashCode();
					if ( _indexMap.TryGetValue( swappedKey, out List<int>? swappedIndices ) ) {
						int swappedIdx = swappedIndices.IndexOf( lastIndex );
						if ( swappedIdx >= 0 ) {
							swappedIndices[ swappedIdx ] = index;
						}
					}
				}
			}

			_subscriptions.RemoveAt( lastIndex );

			// update the map
			int key = subscriber.GetHashCode();
			if ( _indexMap.TryGetValue( key, out List<int>? indices ) ) {
				indices.Remove( lastIndex );
				if ( indices.Count == 0 ) {
					_indexMap.Remove( key );
				}
			}
		}
	};
};