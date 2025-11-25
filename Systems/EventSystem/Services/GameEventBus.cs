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
using NomadCore.Abstractions.Services;
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Systems.EventSystem.Common;
using NomadCore.Systems.EventSystem.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.EventSystem.Services {
	/*
	===================================================================================

	GameEventBus

	===================================================================================
	*/
	/// <summary>
	/// Handles <see cref="GameEvent"/> publishing, event subscriptions, and godot native events.
	/// </summary>

	public sealed class GameEventBus : IGameEventBusService {
		private readonly struct ConnectionInfo {
			/// <summary>
			/// 
			/// </summary>
			public readonly GodotObject Source;

			/// <summary>
			/// 
			/// </summary>
			public readonly StringName SignalName;

			/// <summary>
			/// 
			/// </summary>
			public readonly Callable Callable;

			/*
			===============
			ConnectionInfo
			===============
			*/
			public ConnectionInfo( GodotObject? source, StringName? signalName, Callable? callable ) {
				if ( signalName == null || signalName.IsEmpty ) {
					throw new ArgumentException( "signalName is null or empty" );
				}
				ArgumentNullException.ThrowIfNull( source );
				if ( !callable.HasValue ) {
					throw new ArgumentNullException( nameof( callable ) );
				}

				Source = source;
				SignalName = signalName;
				Callable = callable.Value;
			}
		};

		/// <summary>
		/// The collection/container for all events
		/// </summary>
		private readonly ConcurrentDictionary<IGameEvent, EventSubscriptionSet> EventCache = new ConcurrentDictionary<IGameEvent, EventSubscriptionSet>();

		/// <summary>
		/// The container for all scene based events
		/// </summary>
		private readonly ConcurrentDictionary<object, HashSet<IGameEvent>> SubscriberToEvents = new ConcurrentDictionary<object, HashSet<IGameEvent>>();

		/// <summary>
		/// 
		/// </summary>
		private readonly ConcurrentDictionary<GodotObject, List<ConnectionInfo>> GodotConnections = new ConcurrentDictionary<GodotObject, List<ConnectionInfo>>();

		/// <summary>
		/// 
		/// </summary>
		private readonly Func<IGameEvent, EventSubscriptionSet> SubscriptionSetFactory = new Func<IGameEvent, EventSubscriptionSet>( ( e ) => new EventSubscriptionSet( e ) );
		private readonly Func<object, HashSet<IGameEvent>> HashSetFactory = new Func<object, HashSet<IGameEvent>>( ( s ) => new HashSet<IGameEvent>() );

		/*
		===============
		Initialize
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Initialize() {
		}

		/*
		===============
		Shutdown
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Shutdown() {
		}

		/*
		===============
		BindEventFriend
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="friend"></param>
		public static void BindEventFriend( IGameEvent? owner, IGameEvent? friend ) {
			ArgumentNullException.ThrowIfNull( owner );
		}

		/*
		===============
		ConnectSignal
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="signalName"></param>
		/// <param name="target"></param>
		/// <param name="method"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void ConnectSignal( GodotObject? source, StringName? signalName, GodotObject? target, Action? method ) {
			ArgumentNullException.ThrowIfNull( source );
			ArgumentNullException.ThrowIfNull( target );
			ArgumentNullException.ThrowIfNull( method );
			if ( signalName == null || signalName.IsEmpty ) {
				throw new ArgumentException( "signalName is null or empty" );
			}

			// make sure the source actually has the signal we're connecting to
			if ( !source.HasSignal( signalName ) ) {
				throw new InvalidOperationException( $"GodotObject {source.GetType().FullName} doesn't have signal {signalName}" );
			}

			string callableKey = $"{target.GetInstanceId()}:{method.Method.Name}";

			if ( !GodotConnections.TryGetValue( source, out List<ConnectionInfo>? connectionList ) ) {
				connectionList = new List<ConnectionInfo>();
				if ( !GodotConnections.TryAdd( source, connectionList ) ) {

				}
			}
			ArgumentNullException.ThrowIfNull( connectionList );

			Callable callable = Callable.From( method );
			source.Connect( signalName, callable );

			connectionList.Add( new ConnectionInfo( source, signalName, callable ) );

			HookGodotObjectCleanup( source );
		}

		/*
		===============
		ConnectSignal
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="signalName"></param>
		/// <param name="target"></param>
		/// <param name="method"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void ConnectSignal( GodotObject? source, StringName? signalName, GodotObject? target, Callable? method ) {
			ArgumentNullException.ThrowIfNull( source );
			ArgumentNullException.ThrowIfNull( target );

			if ( signalName == null || signalName.IsEmpty ) {
				throw new ArgumentException( "signalName is null or empty" );
			}
			if ( !method.HasValue ) {
				throw new ArgumentNullException( nameof( method ) );
			}
			if ( !source.HasSignal( signalName ) ) {
				throw new InvalidOperationException( $"GodotObject {source.GetType().FullName} doesn't have signal {signalName}" );
			}

			if ( !GodotConnections.TryGetValue( source, out List<ConnectionInfo>? connectionList ) ) {
				connectionList = new List<ConnectionInfo>();
				if ( !GodotConnections.TryAdd( source, connectionList ) ) {

				}
			}
			ArgumentNullException.ThrowIfNull( connectionList );
			source.Connect( signalName, method.Value );

			connectionList.Add( new ConnectionInfo( source, signalName, method ) );

			HookGodotObjectCleanup( source );
		}

		/*
		===============
		ReleaseDanglingDelegates
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="event"></param>
		public static void ReleaseDanglingDelegates( Delegate @event ) {
			if ( @event == null ) {
				return;
			}
			Delegate[] invocations = @event.GetInvocationList();
			for ( int i = 0; i < invocations.Length; i++ ) {
				Delegate.Remove( @event, invocations[ i ] );
			}
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TEvent"></typeparam>
		/// <typeparam name="TCallback"></typeparam>
		/// <param name="subscriber"></param>
		/// <param name="eventHandler"></param>
		/// <param name="callback"></param>
		public void Subscribe( object? subscriber, IGameEvent? eventHandler, IGameEvent.EventCallback? callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( eventHandler );
			ArgumentNullException.ThrowIfNull( callback );

			EventSubscriptionSet? subscriptionSet = EventCache.GetOrAdd( eventHandler, SubscriptionSetFactory );
			ArgumentNullException.ThrowIfNull( subscriptionSet );

			ServiceRegistry.Get<ILoggerService>()?.PrintLine( $"GameEventBus.Subscribe: subscribed to event '{eventHandler.Name}' with callback '{callback.Method.Name}'..." );
			subscriptionSet.AddSubscription( subscriber, callback );

			HashSet<IGameEvent>? events = SubscriberToEvents.GetOrAdd( subscriber, HashSetFactory );
			ArgumentNullException.ThrowIfNull( events );
			lock ( events ) {
				events.Add( eventHandler );
			}

			if ( subscriber is GodotObject godotObject ) {
				HookGodotObjectCleanup( godotObject );
			}
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="subscriber"></param>
		/// <param name="eventHandler"></param>
		/// <param name="callback"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Subscribe<TArgs>( object? subscriber, IGameEvent<TArgs>? eventHandler, IGameEvent<TArgs>.GenericEventCallback? callback ) where TArgs : IEventArgs {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( eventHandler );
			ArgumentNullException.ThrowIfNull( callback );

			eventHandler.Subscribe( subscriber, callback );
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TEvent"></typeparam>
		/// <typeparam name="TCallback"></typeparam>
		/// <param name="subscriber"></param>
		/// <param name="eventHandler"></param>
		/// <param name="callback"></param>
		public void Unsubscribe( object? subscriber, IGameEvent? eventHandler, IGameEvent.EventCallback? callback ) {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( eventHandler );
			ArgumentNullException.ThrowIfNull( callback );

			if ( EventCache.TryGetValue( eventHandler, out EventSubscriptionSet? subscriptionSet ) ) {
				subscriptionSet.RemoveSubscription( subscriber, callback );

				if ( SubscriberToEvents.TryGetValue( subscriber, out HashSet<IGameEvent>? events ) ) {
					lock ( events ) {
						events.Remove( eventHandler );
						if ( events.Count == 0 ) {
							SubscriberToEvents.TryRemove( subscriber, out _ );
						}
					}
				}
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
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="subscriber"></param>
		/// <param name="eventHandler"></param>
		/// <param name="callback"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Unsubscribe<TArgs>( object? subscriber, IGameEvent<TArgs>? eventHandler, IGameEvent<TArgs>.GenericEventCallback? callback ) where TArgs : IEventArgs {
			ArgumentNullException.ThrowIfNull( subscriber );
			ArgumentNullException.ThrowIfNull( eventHandler );
			ArgumentNullException.ThrowIfNull( callback );

			eventHandler.Unsubscribe( subscriber, callback );
		}

		/*
		===============
		Publish
		===============
		*/
		/// <summary>
		/// Publishes an event of name <paramref name="eventHandler"/> to all subscribed components.
		/// </summary>
		/// <param name="eventHandler">The event to publish.</param>
		/// <param name="args"></param>
		/// <param name="singleThreaded"></param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="eventHandler"/> is null.</exception>
		public void Publish( IGameEvent? eventHandler, in IEventArgs args ) {
			ArgumentNullException.ThrowIfNull( eventHandler );

			if ( !GetSubscriptionSet( eventHandler, out EventSubscriptionSet? subscriptionSet ) ) {
				// no subscriptions
				return;
			}
			ArgumentNullException.ThrowIfNull( subscriptionSet );

			// pump it! LOUDER!
			subscriptionSet.Pump( args );
		}

		/*
		===============
		Publish
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="eventHandler"></param>
		/// <param name="args"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Publish<TArgs>( IGameEvent<TArgs>? eventHandler, in TArgs args ) where TArgs : IEventArgs {
			ArgumentNullException.ThrowIfNull( eventHandler );
			eventHandler.Publish( args );
		}

		/*
		===============
		CreateEvent
		===============
		*/
		public IGameEvent CreateEvent( string? name ) {
			return new GameEvent( name );
		}

		/*
		===============
		CreateEvent
		===============
		*/
		public IGameEvent<TArgs> CreateEvent<TArgs>( string? name ) where TArgs : IEventArgs {
			return new GameEvent<TArgs>( name );
		}

		/*
		===============
		CleanupSubscriber
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public void CleanupSubscriber( object? obj ) {
			ArgumentNullException.ThrowIfNull( obj );

			if ( SubscriberToEvents.TryRemove( obj, out HashSet<IGameEvent>? events ) ) {
				foreach ( var eventHandler in events ) {
					if ( EventCache.TryGetValue( eventHandler, out var subscriptionSet ) ) {
						subscriptionSet.RemoveAllForSubscriber( obj );
					}
				}
			}
			if ( obj is GodotObject godotObject ) {
				DisconnectAllForGodotObject( godotObject );
				GodotConnections.TryRemove( godotObject, out _ );
			}
		}

		/*
		===============
		DisconnectAllForGodotObjects
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		private void DisconnectAllForGodotObject( GodotObject obj ) {
			if ( GodotConnections.TryGetValue( obj, out List<ConnectionInfo>? connections ) ) {
				var logger = ServiceRegistry.Get<ILoggerService>();
				for ( int i = 0; i < connections.Count; i++ ) {
					if ( connections[ i ].Source != null ) {
						logger?.PrintDebug(
							string.Format( "Disconnected signal {0} from GodotObject {1} to GodotObject {2}"
								, connections[ i ].SignalName, connections[ i ].Source.GetType().FullName,
								obj.GetType().FullName )
						);
						connections[ i ].Source.Disconnect( connections[ i ].SignalName, connections[ i ].Callable );
					}
				}
				if ( !GodotConnections.TryRemove( new KeyValuePair<GodotObject, List<ConnectionInfo>>( obj, connections ) ) ) {
					logger?.PrintWarning( "GameEventBus.DisconnectAllForGodotObject: Connections.TryRemove failed!" );
				}
			}
		}

		/*
		===============
		GetSubscriptionSet
		===============
		*/
		/// <summary>
		/// Fetches a <see cref="EventSubscriptionSet"/> from the <see cref="EventCache"/>, if it doesn't exist, just return.
		/// </summary>
		/// <param name="eventHandler"></param>
		/// <param name="subscriptionSet"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private bool GetSubscriptionSet( IGameEvent eventHandler, out EventSubscriptionSet? subscriptionSet ) {
			return EventCache.TryGetValue( eventHandler, out subscriptionSet );
		}

		/*
		===============
		HookGodotObjectCleanup
		===============
		*/
		/// <summary>
		/// Hooks a godot object for automatic signal/event cleanup.
		/// </summary>
		/// <param name="godotObject">The godot object to hook.</param>
		/// <returns>True if the connection wasn't cached yet.</returns>
		private bool HookGodotObjectCleanup( GodotObject godotObject ) {
			if ( godotObject is Node node ) {
				if ( !node.IsConnected( Node.SignalName.TreeExiting, Callable.From( () => CleanupSubscriber( godotObject ) ) ) ) {
					node.Connect( Node.SignalName.TreeExiting, Callable.From( () => CleanupSubscriber( godotObject ) ) );
				} else {
					return true;
				}
			}
			return false;
		}
	};
};