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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;

namespace Nomad.Events.Private.SubscriptionSets
{
	/*
	===================================================================================

	ISubscriptionSet

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal interface ISubscriptionSet<TArgs> : IDisposable
		where TArgs : struct
	{
		/// <summary>
		/// 
		/// </summary>
		long PublishCount { get; }

		/// <summary>
		/// 
		/// </summary>
		int SubscriberCount { get; }

		bool AddSubscription( EventCallback<TArgs> callback );
		bool AddSubscriptionAsync( AsyncEventCallback<TArgs> callback );

		bool RemoveSubscription( EventCallback<TArgs> callback );
		bool RemoveSubscriptionAsync( AsyncEventCallback<TArgs> callback );

		void Pump( in TArgs args );
		Task PumpAsync( TArgs args, CancellationToken ct );

		bool ContainsCallback( EventCallback<TArgs> callback, out int index );
		bool ContainsCallbackAsync( AsyncEventCallback<TArgs> callback, out int index );
	};
};
