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

using NomadCore.Domain.Models;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.EventSystem.Infrastructure.Subscriptions;
using NomadCore.Systems.EventSystem.Services;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.EventSystem.Domain {
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
		where TArgs : struct
	{
		/// <summary>
		/// The name of the event.
		/// </summary>
		public string DebugName => _name;
		private readonly InternString _name;

		public int Id => _hashCode;
		private readonly int _hashCode;

		public EventThreadingPolicy ThreadingPolicy => EventThreadingPolicy.MainThread;

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

			bool isSynchronous = ( flags & EventFlags.Synchronous ) != 0;
			bool isAsync = ( flags & EventFlags.Asynchronous ) != 0;
			bool lockFree = ( flags & EventFlags.NoLock ) != 0;

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
		/// <param name="subscriber"></param>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Subscribe( object subscriber, IGameEvent<TArgs>.EventCallback callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.AddSubscription( subscriber, callback );
		}

		/*
		===============
		SubscribeAsync
		===============
		*/
		/// <summary>
		/// Adds a new subscription to the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SubscribeAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.AddSubscriptionAsync( subscriber, callback );
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		/// <summary>
		/// Removes the <paramref name="callback"/> from the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback">The lambda or method to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Unsubscribe( object subscriber, IGameEvent<TArgs>.EventCallback callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.RemoveSubscription( subscriber, callback );
		}

		/*
		===============
		UnsubscribeAsync
		===============
		*/
		/// <summary>
		/// Removes the <paramref name="callback"/> from the GameEvent utilizing the <see cref="GameEventBus"/>.
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback">The lambda or method to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UnsubscribeAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			_subscriptions.RemoveSubscriptionAsync( subscriber, callback );
		}

		/*
		===============
		UnsubscriberAll
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UnsubscribeAll( object subscriber ) {
			ArgumentNullException.ThrowIfNull( subscriber );

			_subscriptions.RemoveAllForSubscriber( subscriber );
		}

		public virtual EventThreadingPolicy GetDefaultThreadingPolicy() => EventThreadingPolicy.MainThread;
	};
};