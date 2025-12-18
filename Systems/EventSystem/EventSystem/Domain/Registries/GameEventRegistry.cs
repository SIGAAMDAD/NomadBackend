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
using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.EventSystem.Domain.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.EventSystem.Domain.Registries {
	/*
	===================================================================================
	
	GameEventRegistry
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public sealed class GameEventRegistry( ILoggerService logger ) : IGameEventRegistryService {
		private readonly ILoggerService _logger = logger;
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
		public IGameEvent<TArgs> GetEvent<TArgs>( InternString name, EventFlags flags = EventFlags.Default )
			where TArgs : IEventArgs
		{
			var key = new EventKey(
				name: name,
				argsType: typeof( TArgs )
			);

			if ( _eventCache.TryGetValue( key, out var value ) ) {
				if ( value is IGameEvent<TArgs> typedEvent ) {
					return typedEvent;
				}
				throw new InvalidOperationException(
					$"Event '{key.Name}' already registered with type {value.GetType().GenericTypeArguments[ 0 ].Name} cannot register with type {typeof( TArgs ).Name}"
				);
			}

			value = new GameEvent<TArgs>( key.Name, _logger, flags );
			_eventCache.TryAdd( key, value );
			return (IGameEvent<TArgs>)value;
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
			where TArgs : IEventArgs
		{
			var key = new EventKey(
				name: name,
				argsType: typeof( TArgs )
			);
			if ( _eventCache.TryRemove( key, out var eventObj ) ) {
				( eventObj as IDisposable )?.Dispose();
				return true;
			}
			return false;
		}
	};
};