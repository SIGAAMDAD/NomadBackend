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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;

namespace Nomad.Events.Private {
	internal interface ISubscriptionSet<TArgs> : IDisposable
		where TArgs : struct
	{
		public void BindEventFriend( IGameEvent friend );
		public void RemoveAllForSubscriber( object subscriber );

		public void AddSubscription( object subscriber, EventCallback<TArgs> callback );
		public void AddSubscriptionAsync( object subscriber, AsyncEventCallback<TArgs> callback );

		public void RemoveSubscription( object subscriber, EventCallback<TArgs> callback );
		public void RemoveSubscriptionAsync( object subscriber, AsyncEventCallback<TArgs> callback );

		public void Pump( in TArgs args );
		public Task PumpAsync( TArgs args, CancellationToken ct );

		public bool ContainsCallback( object subscriber, EventCallback<TArgs> callback, out int index );
		public bool ContainsCallbackAsync( object subscriber, AsyncEventCallback<TArgs> callback, out int index );
	};
};
