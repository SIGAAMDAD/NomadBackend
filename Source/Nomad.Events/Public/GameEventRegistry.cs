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
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Events.Private;

namespace Nomad.Events {
	/*
	===================================================================================

	GameEventRegistry

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	public sealed class GameEventRegistry( ILoggerService logger ) : IGameEventRegistryService {
		private readonly ConcurrentDictionary<EventKey, IGameEvent> _eventCache = new();

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			foreach ( var @event in _eventCache ) {
				@event.Value.Dispose();
			}
			_eventCache.Clear();
		}

		/*
		===============
		RegisterEvent
		===============
		*/
		/// <summary>
		/// Registers a <see cref="IGameEvent"/> with argument parameters <typeparamref name="TArgs"/> and id <paramref name="name"/>.
		/// </summary>
		/// <typeparam name="TArgs">The type of <see cref="IEventArgs"/> used with the event.</typeparam>
		/// <param name="name">Name of the event to register.</param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public IGameEvent<TArgs> GetEvent<TArgs>( string name )
			where TArgs : struct {
			var key = new EventKey(
				name: new( name ),
				argsType: typeof( TArgs )
			);

			if ( _eventCache.TryGetValue( key, out var value ) ) {
				if ( value is IGameEvent<TArgs> typedEvent ) {
					return typedEvent;
				}
				throw new InvalidOperationException(
					$"Event '{key.Name}' already registered with type {value.GetType().GenericTypeArguments[0].Name} cannot register with type {typeof( TArgs ).Name}"
				);
			}

			value = new GameEvent<TArgs>( key.Name, logger, EventFlags.Default );
			_eventCache.TryAdd( key, value );
			return ( IGameEvent<TArgs> )value;
		}

		/*
		===============
		TryRemoveEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="name"></param>
		/// <param name="nameSpace"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryRemoveEvent<TArgs>( InternString name )
			where TArgs : struct {
			var key = new EventKey(
				name: name,
				argsType: typeof( TArgs )
			);
			if ( _eventCache.TryRemove( key, out var eventObj ) ) {
				(eventObj as IDisposable)?.Dispose();
				return true;
			}
			return false;
		}
	};
};
