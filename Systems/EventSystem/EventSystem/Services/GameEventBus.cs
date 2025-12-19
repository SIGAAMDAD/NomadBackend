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
using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;
using System;
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
			public ConnectionInfo( GodotObject source, StringName signalName, Callable callable ) {
				if ( signalName == null || signalName.IsEmpty ) {
					throw new ArgumentException( "signalName is null or empty" );
				}
				ArgumentNullException.ThrowIfNull( source );

				Source = source;
				SignalName = signalName;
				Callable = callable;
			}
		};

		/// <summary>
		/// The container for all scene based events
		/// </summary>
		private readonly ConditionalWeakTable<object, HashSet<IGameEvent>> _subscriberToEvents;

		/// <summary>
		/// The container for all godot node events.
		/// </summary>
		private readonly ConditionalWeakTable<GodotObject, List<ConnectionInfo>> _godotConnections = new();

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_godotConnections.Clear();
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
			var connectionList = _godotConnections.GetOrAdd( source, s => new List<ConnectionInfo>() );
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
		public void ConnectSignal( GodotObject source, StringName signalName, GodotObject target, Callable method ) {
			ArgumentNullException.ThrowIfNull( source );
			ArgumentNullException.ThrowIfNull( target );

			if ( signalName == null || signalName.IsEmpty ) {
				throw new ArgumentException( "signalName is null or empty" );
			}
			if ( !source.HasSignal( signalName ) ) {
				throw new InvalidOperationException( $"GodotObject {source.GetType().FullName} doesn't have signal {signalName}" );
			}

			var connectionList = _godotConnections.GetOrAdd( source, s => new List<ConnectionInfo>() );
			ArgumentNullException.ThrowIfNull( connectionList );
			source.Connect( signalName, method );

			connectionList.Add( new ConnectionInfo( source, signalName, method ) );

			HookGodotObjectCleanup( source );
		}

		public void Subscribe<TArgs>( object subscriber, IGameEvent<TArgs> @event, IGameEvent<TArgs>.EventCallback callback ) where TArgs : IEventArgs {
		}

		public void SubscribeAsync<TArgs>( object subscriber, IGameEvent<TArgs> @event, IGameEvent<TArgs>.AsyncCallback asyncCallback ) where TArgs : IEventArgs {
		}

		public void Unsubscribe<TArgs>( object subscriber, IGameEvent<TArgs> @event, IGameEvent<TArgs>.EventCallback callback ) where TArgs : IEventArgs {
		}

		public void UnsubscribeAsync<TArgs>( object subscriber, IGameEvent<TArgs> @event, IGameEvent<TArgs>.AsyncCallback asyncCallback ) where TArgs : IEventArgs {
		}

		public void DisconnectSignal( GodotObject source, StringName signalName ) {
		}

		/*
		===============
		CleanupSubscriber
		===============
		*/
		public void CleanupSubscriber( object subscriber ) {
			ArgumentNullException.ThrowIfNull( subscriber );

			if ( subscriber is GodotObject godotObject ) {
				DisconnectAllForGodotObject( godotObject );
				_godotConnections.Remove( godotObject, out _ );
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
			if ( _godotConnections.TryGetValue( obj, out List<ConnectionInfo>? connections ) ) {
				for ( int i = 0; i < connections.Count; i++ ) {
					connections[ i ].Source?.Disconnect( connections[ i ].SignalName, connections[ i ].Callable );
				}
				_godotConnections.Remove( obj, out _ );
			}
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