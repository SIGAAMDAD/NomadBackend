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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Util;

namespace Nomad.Events.Private {
	/*
	===================================================================================

	GameEvent

	===================================================================================
	*/
	/// <summary>
	/// <para>Generic class for managing an event.</para>
	/// <para>Inherit from this class to pass data between modules and events, if not needed, simply name the event, then publish it.</para>
	/// </summary>
	/// <example>
	/// Here's an example of how to use the <see cref="GameEvent"/> class (without any inheritance).
	/// <code lang="csharp">
	/// public class Example {
	/// 	public readonly GameEvent Event = new GameEvent( nameof( Event ) );
	///
	/// 	public static void Main() {
	/// 		gameEvent.Subscribe( this, DoSomething ); // "subscribe" to the event, this function will be called into every time the event is published
	/// 		gameEvent.Publish( EmptyEventArgs.Args ); // this will publish the event to the GameEventBus, ensuring that all subscribers are notified that the event has been triggered.
	/// 	}
	///
	/// 	// this will be called whenever the ExampleEvent is triggered
	/// 	public void DoSomething( in IGameEvent eventData, in IEventArgs args ) {
	/// 		Debug.Log( "Foo" );
	/// 	}
	/// };
	/// </code>
	/// </example>

	public class GameEvent<TArgs> : IGameEvent<TArgs>
		where TArgs : struct {
		/// <summary>
		/// The name of the event.
		/// </summary>
		public string DebugName => _name;
		private readonly InternString _name;

		public int Id => _hashCode;
		private readonly int _hashCode;

		public event EventCallback<TArgs> OnPublished {
			add => Subscribe( this, value );
			remove => Unsubscribe( this, value );
		}
		public event AsyncEventCallback<TArgs> OnPublishedAsync {
			add => SubscribeAsync( this, value );
			remove => UnsubscribeAsync( this, value );
		}

		private readonly ISubscriptionSet<TArgs> _subscriptions;

		/*
		===============
		GameEvent
		===============
		*/
		/// <summary>
		/// Creates a new GameEvent object with the debugging alias of <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the event, should be unique.</param>
		/// <param name="logger"></param>
		/// <exception cref="ArgumentException">Thrown if name is null or empty.</exception>
		internal GameEvent( InternString name, ILoggerService logger, EventFlags flags ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			_name = name;
			_hashCode = HashCode.Combine( GetHashCode(), _name.GetHashCode() );

			bool isSynchronous = (flags & EventFlags.Synchronous) != 0;
			bool isAsync = (flags & EventFlags.Asynchronous) != 0;
			bool lockFree = (flags & EventFlags.NoLock) != 0;

			if ( lockFree && isAsync ) {
				throw new NotSupportedException( "Cannot have an event that is both lock free and asynchronous!" );
			}

			if ( lockFree ) {
				_subscriptions = new LockFreeSubscriptionSet<TArgs>( this, logger );
			} else if ( isAsync ) {
				_subscriptions = new SubscriptionSet<TArgs>( this, logger );
			}
		}

		/*
		===============
		Equals
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( IGameEvent? other ) {
			return other is not null && Id.Equals( other.Id );
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
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Publish
		===============
		*/
		/// <summary>
		/// Publishes an event to the main <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="eventArgs"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Publish( in TArgs eventArgs ) {
			_subscriptions.Pump( in eventArgs );
		}

		/*
		===============
		PublishAsync
		===============
		*/
		/// <summary>
		/// Publishes an event to the main <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="eventArgs"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public async Task PublishAsync( TArgs eventArgs, CancellationToken ct = default ) {
			await _subscriptions.PumpAsync( eventArgs, ct );
		}


		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// Adds a new subscription to the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public IDisposable Subscribe( EventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.AddSubscription( this, callback );
			return this;
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// Adds a new subscription to the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public IDisposable SubscribeAsync( AsyncEventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.AddSubscriptionAsync( this, callback );
			return this;
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// Adds a new subscription to the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Subscribe( object owner, EventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( owner );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.AddSubscription( owner, callback );
		}

		/*
		===============
		SubscribeAsync
		===============
		*/
		/// <summary>
		/// Adds a new subscription to the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SubscribeAsync( object owner, AsyncEventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( owner );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.AddSubscriptionAsync( owner, callback );
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		/// <summary>
		/// Removes the <paramref name="callback"/> from the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="callback">The lambda or method to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Unsubscribe( object owner, EventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( owner );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.RemoveSubscription( owner, callback );
		}

		/*
		===============
		UnsubscribeAsync
		===============
		*/
		/// <summary>
		/// Removes the <paramref name="callback"/> from the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="callback">The lambda or method to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UnsubscribeAsync( object owner, AsyncEventCallback<TArgs> callback ) {
			ArgumentNullException.ThrowIfNull( owner );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.RemoveSubscriptionAsync( owner, callback );
		}

		/*
		===============
		UnsubscriberAll
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UnsubscribeAll( object owner ) {
			ArgumentNullException.ThrowIfNull( owner );

			_subscriptions.RemoveAllForSubscriber( owner );
		}

		/*
		===============
		operator +=
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		public void operator +=( EventCallback<TArgs> callback ) {
			_subscriptions.AddSubscription( this, callback );
		}

		/*
		===============
		operator +=
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		public void operator +=( AsyncEventCallback<TArgs> callback ) {
			_subscriptions.AddSubscriptionAsync( this, callback );
		}

		/*
		===============
		operator -=
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		public void operator -=( EventCallback<TArgs> callback ) {
			_subscriptions.RemoveSubscription( this, callback );
		}

		/*
		===============
		operator -=
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		public void operator -=( AsyncEventCallback<TArgs> callback ) {
			_subscriptions.RemoveSubscriptionAsync( this, callback );
		}
	};
};
