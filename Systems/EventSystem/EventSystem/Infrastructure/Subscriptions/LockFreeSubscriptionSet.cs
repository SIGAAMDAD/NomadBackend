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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.EventSystem.Infrastructure.Subscriptions {
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

		private readonly SubscriptionCache<TArgs, IGameEvent<TArgs>.EventCallback> _genericSubscriptions = new( logger );
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
		public void AddSubscription( object subscriber, IGameEvent<TArgs>.EventCallback callback ) {
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
		public void AddSubscriptionAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback ) {
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
		public void RemoveSubscription( object subscriber, IGameEvent<TArgs>.EventCallback callback ) {
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
		public void RemoveSubscriptionAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback ) {
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
		/// "Publishes" an event to all subscribers with thread-safety
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
				NotifySubscriber( subscriptions[ i ], in args );
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
		public bool ContainsCallback( object subscriber, IGameEvent<TArgs>.EventCallback callback, out int index ) {
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
		public bool ContainsCallbackAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback, out int index ) {
			throw new NotSupportedException();
		}

		/*
		===============
		NotifySubscriber
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscription"></param>
		/// <param name="args"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void NotifySubscriber( in WeakSubscription<TArgs, IGameEvent<TArgs>.EventCallback> subscription, in TArgs args ) {
			if ( subscription.Subscriber.TryGetTarget( out _ ) && subscription.Callback.TryGetTarget( out var callback ) ) {
				callback.Invoke( in args );
			}
		}
	};
};