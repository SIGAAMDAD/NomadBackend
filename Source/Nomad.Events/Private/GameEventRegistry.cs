/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

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
using System.Collections.Generic;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Events.Private;

namespace Nomad.Events
{
	/*
	===================================================================================

	GameEventRegistry

	===================================================================================
	*/
	/// <summary>
	/// Handles event registration and lookup.
	/// </summary>

	internal sealed class GameEventRegistry : IGameEventRegistryService
	{
		private readonly ConcurrentDictionary<EventKey, IGameEvent> _eventCache = new ConcurrentDictionary<EventKey, IGameEvent>();
		private readonly ConcurrentDictionary<InternString, ISubscriptionGroup> _groupCache = new ConcurrentDictionary<InternString, ISubscriptionGroup>();

		private readonly ILoggerService _logger;

		private bool _isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="logger"></param>
		public GameEventRegistry( ILoggerService logger )
		{
			_logger = logger;
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				ClearAllGroups();
				foreach ( var @event in _eventCache ) {
					@event.Value.Dispose();
				}
				_eventCache.Clear();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		CreateGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ISubscriptionGroup GetGroup( string name )
		{
			var group = _groupCache.GetOrAdd( new InternString( name ), g => new SubscriptionGroup( name, this ) );
			return group;
		}

		/*
		===============
		ClearAllGroups
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void ClearAllGroups()
		{
			foreach ( var group in _groupCache ) {
				group.Value.Dispose();
			}
			_groupCache.Clear();
		}

		/*
		===============
		RemoveGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="group"></param>
		public void RemoveGroup( ISubscriptionGroup group )
		{
			_groupCache.TryRemove( new InternString( group.Name ), out _ );
		}

		/*
		===============
		ClearEventsInNamespace
		===============
		*/
		/// <summary>
		/// Releases all events in the naming space of <paramref name="nameSpace"/>.
		/// </summary>
		/// <param name="nameSpace"></param>
		public void ClearEventsInNamespace( string nameSpace )
		{
			foreach ( var @event in _eventCache ) {
				if ( @event.Value.NameSpace.Equals( nameSpace, StringComparison.InvariantCulture ) ) {
					@event.Value.Dispose();
					_eventCache.Remove( @event.Key, out _ );
				}
			}
		}

		/*
		===============
		GetEvent
		===============
		*/
		/// <summary>
		/// Registers a <see cref="IGameEvent"/> with argument parameters <typeparamref name="TArgs"/> and id <paramref name="name"/>.
		/// </summary>
		/// <typeparam name="TArgs">The type of struct payload used with the event.</typeparam>
		/// <param name="name">Name of the event to register.</param>
		/// <param name="nameSpace"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public IGameEvent<TArgs> GetEvent<TArgs>( string name, string nameSpace, EventFlags flags = EventFlags.Default )
			where TArgs : struct
		{
			var internedName = new InternString( name );
			var internedNameSpace = new InternString( nameSpace );
			var key = new EventKey(
				name: internedName,
				nameSpace: internedNameSpace,
				argsType: typeof( TArgs )
			);

			if ( _eventCache.TryGetValue( key, out IGameEvent? value ) ) {
				return (IGameEvent<TArgs>)value;
			}

			value = new GameEvent<TArgs>( internedNameSpace, internedName, _logger, flags );
			_eventCache.TryAdd( key, value );
			return (IGameEvent<TArgs>)value;
		}

		/*
		===============
		TryGetEvent
		===============
		*/
		/// <summary>
		/// Attempts to retrieve an existing event by name and namespace.
		/// </summary>
		/// <typeparam name="TArgs">The struct type containing event data.</typeparam>
		/// <param name="name">The name of the event.</param>
		/// <param name="nameSpace">The namespace the event belongs to.</param>
		/// <param name="gameEvent">When this method returns, contains the event if found; otherwise <c>null</c>.</param>
		/// <returns><c>true</c> if the event exists; otherwise <c>false</c>.</returns>
		public bool TryGetEvent<TArgs>( string name, string nameSpace, out IGameEvent<TArgs>? gameEvent )
			where TArgs : struct
		{
			var key = new EventKey(
				name: new InternString( name ),
				nameSpace: new InternString( nameSpace ),
				argsType: typeof( TArgs )
			);
			if ( _eventCache.TryGetValue( key, out IGameEvent? @event ) ) {
				if ( @event is IGameEvent<TArgs> typedEvent ) {
					gameEvent = typedEvent;
					return true;
				}
			}
			gameEvent = null;
			return false;
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
		public bool TryRemoveEvent<TArgs>( string name, string nameSpace )
			where TArgs : struct
		{
			var key = new EventKey(
				name: new InternString( name ),
				nameSpace: new InternString( nameSpace ),
				argsType: typeof( TArgs )
			);
			if ( _eventCache.TryRemove( key, out IGameEvent? eventObj ) ) {
				(eventObj as IDisposable)?.Dispose();
				return true;
			}
			return false;
		}

		/*
		===============
		ClearAllEvents
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void ClearAllEvents()
		{
			foreach ( var @event in _eventCache ) {
				@event.Value.Dispose();
			}
			_eventCache.Clear();
		}

		/*
		===============
		GetAllEvents
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IReadOnlyCollection<IGameEvent> GetAllEvents()
			=> (IReadOnlyCollection<IGameEvent>)_eventCache.Values;
	};
};
