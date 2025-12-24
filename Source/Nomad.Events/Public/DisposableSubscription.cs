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
using System.Runtime.CompilerServices;
using Nomad.Core.Events;

namespace Nomad.Events {
	/*
	===================================================================================

	DisposableSubscription

	===================================================================================
	*/
	/// <summary>
	/// An automated disposable subscription.
	/// </summary>

	public sealed class DisposableSubscription<TArgs> : IDisposable
		where TArgs : struct
	{
		private IGameEvent<TArgs> _event;
		private EventCallback<TArgs> _callback;

		/*
		===============
		DisposableSubscription
		===============
		*/
		public DisposableSubscription( IGameEvent<TArgs> handler, EventCallback<TArgs> callback ) {
			handler.Subscribe( this, callback );
			_event = handler;
			_callback = callback;
		}

		/*
		===============
		~DisposableSubscription
		===============
		*/
		~DisposableSubscription() {
			Dispose();
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_event.Unsubscribe( this, _callback );
			_event = null;
			_callback = null;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Publish
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Publish( in TArgs eventArgs ) {
			_event.Publish( in eventArgs );
		}
	};
};
