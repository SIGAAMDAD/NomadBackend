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

using System;
using System.Collections.Generic;
using Nomad.Core.Logger;

namespace Nomad.Events.Private {
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
		/// <summary>
		///
		/// </summary>
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
