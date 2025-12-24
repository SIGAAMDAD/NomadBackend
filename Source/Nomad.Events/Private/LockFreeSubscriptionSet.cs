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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.Logger;

namespace Nomad.Events.Private {
	/*
	===================================================================================

	LockFreeSubscriptionSet

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class LockFreeSubscriptionSet<TArgs>( IGameEvent<TArgs> eventData, ILoggerService logger, int cleanupInterval = 100 ) : ISubscriptionSet<TArgs>
		where TArgs : struct
	{
		/// <summary>
		/// The number of pumps before initiating a purge.
		/// </summary>
		private readonly int _cleanupInterval = cleanupInterval;

		private readonly ILoggerService _logger = logger;

		private readonly SubscriptionCache<TArgs, EventCallback<TArgs>> _genericSubscriptions = new( logger );
		private int _cleanupCounter = 0;

		private readonly HashSet<WeakReference<IGameEvent>> _friends = new HashSet<WeakReference<IGameEvent>>();

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Clears all the subscriptions within this set.
		/// </summary>
		public void Dispose() {
			_friends.Clear();
		}

		/*
		===============
		BindEventFriend
		===============
		*/
		/// <summary>
		/// Binds the provided event to the owned event in a "friendship".
		/// </summary>
		/// <param name="friend"></param>
		public void BindEventFriend( IGameEvent friend ) {
			var refEvent = new WeakReference<IGameEvent>( friend );
			if ( _friends.Contains( refEvent ) ) {
				_logger?.PrintWarning( $"LockFreeSubscriptionSet.BindEventFriend: event '{eventData.DebugName}' already has friendship with event '{friend.DebugName}'" );
			}

			_logger?.PrintLine( $"LockFreeSubscriptionSet.BindEventFriend: friendship created between events '{eventData.DebugName}' and '{friend.DebugName}'" );
			_friends.Add( refEvent );
		}

		/*
		===============
		AddSubscription
		===============
		*/
		/// <summary>
		/// Adds a callback method to the <see cref="Subscriptions"/> list.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback">The method that is called whenever the event triggers.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		public void AddSubscription( object subscriber, EventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			_genericSubscriptions.AddSubscription( subscriber, callback );
		}

		/*
		===============
		AddSubscriptionAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		/// <exception cref="NotSupportedException"></exception>
		public void AddSubscriptionAsync( object subscriber, AsyncEventCallback<TArgs> callback ) {
			throw new NotSupportedException();
		}

		/*
		===============
		RemoveSubscription
		===============
		*/
		/// <summary>
		/// Removes the provided <paramref name="callback"/> from the event's subscription list.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback">The callback to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the returned index from <see cref="ContainsCallback"/> is invalid.</exception>
		public void RemoveSubscription( object subscriber, EventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			_genericSubscriptions.RemoveSubscription( subscriber, callback );
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
		/// <exception cref="NotSupportedException"></exception>
		public void RemoveSubscriptionAsync( object subscriber, AsyncEventCallback<TArgs> callback ) {
			throw new NotSupportedException();
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

			_genericSubscriptions.RemoveAllForSubscriber( subscriber );
		}

		/*
		===============
		PumpWithLock
		===============
		*/
		/// <summary>
		/// "Publishes" an event to all subscribers with thread-safety.
		/// </summary>
		/// <param name="args"></param>
		public void Pump( in TArgs args ) {
#if EVENT_DEBUG
			_logger?.PrintLine( $"SubscriptionSet.Pump: publishing event {eventData.DebugName}" );
#endif
			bool shouldCleanup = false;
			if ( ++_cleanupCounter >= _cleanupInterval ) {
				shouldCleanup = true;
				_cleanupCounter = 0;
			}

			var subscriptions = _genericSubscriptions.Subscriptions;
			int count = subscriptions.Count;
			for ( int i = 0; i < count; i++ ) {
				var subscription = subscriptions[ i ];
				if ( subscription.Subscriber.TryGetTarget( out _ ) && subscription.Callback.TryGetTarget( out var callback ) ) {
					callback.Invoke( in args );
				}
			}

			if ( shouldCleanup ) {
				_genericSubscriptions.CleanupDeadSubscriptions();
			}
		}

		/*
		===============
		PumpAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public async Task PumpAsync( TArgs args, CancellationToken ct ) {
			throw new NotSupportedException();
		}

		/*
		===============
		ContainsCallback
		===============
		*/
		/// <summary>
		/// Public method that acquires the read lock and checks for callback existence.
		/// </summary>s
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool ContainsCallback( object subscriber, EventCallback<TArgs> callback, out int index ) {
			index = -1;
			return _genericSubscriptions.ContainsCallback( subscriber, callback, out index );
		}

		/*
		===============
		ContainsCallbackAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public bool ContainsCallbackAsync( object subscriber, AsyncEventCallback<TArgs> callback, out int index ) {
			throw new NotSupportedException();
		}
	};
};
