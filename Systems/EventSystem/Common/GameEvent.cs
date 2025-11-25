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

using NomadCore.Abstractions.Services;
using NomadCore.Enums;
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Systems.EventSystem.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.EventSystem.Common {
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

	public class GameEvent : IGameEvent {
		/// <summary>
		/// The name of the event.
		/// </summary>
		public string? Name => _name;
		private readonly string? _name;

		public EventThreadingPolicy ThreadingPolicy => EventThreadingPolicy.MainThread;

		private readonly IGameEventBusService EventBus = ServiceRegistry.Get<IGameEventBusService>();

		/*
		===============
		GameEvent
		===============
		*/
		/// <summary>
		/// Creates a new GameEvent object with the debugging alias of <paramref name="name"/>.
		/// </summary>
		/// <param name="name">The name of the event, should be unique.</param>
		/// <exception cref="ArgumentException">Thrown if name is null or empty.</exception>
		public GameEvent( string? name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			_name = name;
		}

		/*
		===============
		GameEvent
		===============
		*/
		/// <summary>
		/// Shouldn't be called, use <see cref="GameEvent( string? name )"/> instead. This is because while debugging, we want to know
		/// which events are being manipulated, both when and where.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		private GameEvent() {
			throw new InvalidOperationException( "Call GameEvent( string? name ) instead" );
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
		public void Publish( IEventArgs eventArgs ) {
			EventBus.Publish( this, in eventArgs );
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
		public void Subscribe( object? subscriber, IGameEvent.EventCallback? callback ) {
			ArgumentNullException.ThrowIfNull( callback );
			EventBus.Subscribe( subscriber, this, callback );
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
		public void Unsubscribe( object? subscriber, IGameEvent.EventCallback? callback ) {
			ArgumentNullException.ThrowIfNull( callback );
			EventBus.Unsubscribe( subscriber, this, callback );
		}

		public virtual EventThreadingPolicy GetDefaultThreadingPolicy() => EventThreadingPolicy.MainThread;
	};

	/*
	===================================================================================
	
	GameEvent<TArgs>
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TArgs">The type of <see cref="TypedEventArgs"/> that'll be used with this event type.</typeparam>
	/// <remarks>
	/// Creates a new GameEvent object with the debugging alias of <paramref name="name"/>.
	/// </remarks>
	/// <param name="name">The name of the event, should be unique.</param>
	/// <exception cref="ArgumentException">Thrown if name is null or empty.</exception>



	public class GameEvent<TArgs>( string? name ) : IGameEvent<TArgs> where TArgs : IEventArgs {
		/// <summary>
		/// The name of the event.
		/// </summary>
		public string? Name => NonGenericEvent.Name;
		public EventThreadingPolicy ThreadingPolicy => EventThreadingPolicy.MainThread;

		private readonly GameEvent NonGenericEvent = new GameEvent( name );
		private readonly ConcurrentDictionary<object, List<IGameEvent<TArgs>.GenericEventCallback>> TypedSubscriptions = new ConcurrentDictionary<object, List<IGameEvent<TArgs>.GenericEventCallback>>();
		private readonly ConcurrentDictionary<object, List<Func<TArgs, CancellationToken, Task>>> AsyncSubscriptions = new ConcurrentDictionary<object, List<Func<TArgs, CancellationToken, Task>>>();

		/*
		===============
		Subscribe
		===============
		*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		/// <exception cref="InvalidCastException"></exception>
		public virtual void Subscribe( object? subscriber, IGameEvent<TArgs>.GenericEventCallback? callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			var subscriptions = TypedSubscriptions.GetOrAdd( subscriber, _ => new List<IGameEvent<TArgs>.GenericEventCallback>() );
			lock ( subscriptions ) {
				subscriptions.Add( callback );
			}

			NonGenericEvent.Subscribe( subscriber, ( in IGameEvent eventData, in IEventArgs args ) => {
				if ( args is TArgs typedArgs ) {
					callback.Invoke( typedArgs );
				} else {
					throw new InvalidCastException( nameof( args ) );
				}
			} );
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="asyncCallback"></param>
		public void Subscribe( object? subscriber, Func<TArgs, CancellationToken, Task>? asyncCallback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( asyncCallback );

			var subscriptions = AsyncSubscriptions.GetOrAdd( subscriber, _ => new List<Func<TArgs, CancellationToken, Task>>() );
			lock ( subscriptions ) {
				subscriptions.Add( asyncCallback );
			}
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="subscriber"></param>
		/// <param name="callback"></param>
		public virtual void Unsubscribe( object? subscriber, IGameEvent<TArgs>.GenericEventCallback? callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( callback );

			if ( TypedSubscriptions.TryGetValue( subscriber, out List<IGameEvent<TArgs>.GenericEventCallback> subscriptions ) ) {
				lock ( subscriptions ) {
					subscriptions.Remove( callback );
					if ( subscriptions.Count == 0 ) {
						TypedSubscriptions.TryRemove( subscriber, out _ );
					}
				}
			}
			NonGenericEvent.Unsubscribe( subscriber, null );
		}

		/*
		===============
		Publish
		===============
		*/
		public virtual void Publish( TArgs eventArgs ) {
			NonGenericEvent.Publish( eventArgs );
		}

		/*
		===============
		PublishAsync
		===============
		*/
		public Task PublishAsync( TArgs eventArgs, CancellationToken cancellationToken = default ) {
			List<Task> tasks = new List<Task>();
			return Task.WhenAll( tasks );
		}

		/*
		===============
		Subscribe
		===============
		*/
		void IGameEvent.Subscribe( object? subscriber, IGameEvent.EventCallback? callback ) {
			NonGenericEvent.Subscribe( subscriber, callback );
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		void IGameEvent.Unsubscribe( object? subscriber, IGameEvent.EventCallback? callback ) {
			NonGenericEvent.Unsubscribe( subscriber, callback );
		}

		/*
		===============
		Publish
		===============
		*/
		void IGameEvent.Publish( IEventArgs args ) {
			NonGenericEvent.Publish( args );
		}

		/*
		===============
		Dispose
		===============
		*/
		public virtual void Dispose() {
			foreach ( var subscriptionList in TypedSubscriptions.Values ) {
				subscriptionList.Clear();
			}
			TypedSubscriptions.Clear();
			NonGenericEvent.Dispose();

			GC.SuppressFinalize( this );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public virtual EventThreadingPolicy GetDefaultThreadingPolicy() => EventThreadingPolicy.MainThread;
	};
};