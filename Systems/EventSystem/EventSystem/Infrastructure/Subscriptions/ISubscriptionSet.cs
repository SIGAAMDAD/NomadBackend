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

using NomadCore.Domain.Models.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.EventSystem.Infrastructure.Subscriptions {
	internal interface ISubscriptionSet<TArgs> : IDisposable
		where TArgs : struct
	{
		public void BindEventFriend( IGameEvent friend );
		public void RemoveAllForSubscriber( object subscriber );

		public void AddSubscription( object subscriber, IGameEvent<TArgs>.EventCallback callback );
		public void AddSubscriptionAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback );
		
		public void RemoveSubscription( object subscriber, IGameEvent<TArgs>.EventCallback callback );
		public void RemoveSubscriptionAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback );

		public void Pump( in TArgs args );
		public Task PumpAsync( TArgs args, CancellationToken ct );

		public bool ContainsCallback( object subscriber, IGameEvent<TArgs>.EventCallback callback, out int index );
		public bool ContainsCallbackAsync( object subscriber, IGameEvent<TArgs>.AsyncCallback callback, out int index );
	};
};