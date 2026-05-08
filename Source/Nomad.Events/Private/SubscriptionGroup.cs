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
using System.Collections.Generic;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;

namespace Nomad.Events.Private
{
	/*
	===================================================================================

	SubscriptionGroup

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SubscriptionGroup : ISubscriptionGroup
	{
		public string Name => _name;
		private readonly string _name;

		public IReadOnlyList<ISubscriptionHandle> Subscriptions => _subscriptions;
		private readonly List<ISubscriptionHandle> _subscriptions = new();

		private readonly IGameEventRegistryService _eventFactory;

		private bool _isDisposed = false;

		/*
		===============
		SubscriptionGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="eventRegistry"></param>
		public SubscriptionGroup( string name, IGameEventRegistryService eventRegistry )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name, nameof( name ) );
			_name = name;
			_eventFactory = eventRegistry;
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
				UnsubscribeAll();
				_eventFactory.RemoveGroup( this );
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Add
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="gameEvent"></param>
		/// <param name="callback"></param>
		public void Add<TArgs>( IGameEvent<TArgs> gameEvent, EventCallback<TArgs> callback )
			where TArgs : struct
		{
			_subscriptions.Add( gameEvent.Subscribe( callback ) );
		}

		/*
		===============
		UnsubscribeAll
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void UnsubscribeAll()
		{
			for ( int i = 0; i < _subscriptions.Count; ++i ) {
				_subscriptions[i]?.Dispose();
			}
			_subscriptions.Clear();
		}
	};
};
