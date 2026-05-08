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
using System.Runtime.CompilerServices;
using Nomad.Core.Collections;

namespace Nomad.Events.Private
{
	/*
	===================================================================================

	SubscriptionCache

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SubscriptionCache<TArgs, TCallback> : IDisposable
		where TArgs : struct
	{
		public int Count => _subscriptions != null ? _subscriptions.Count : 0;

		public TCallback this[int index] {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _subscriptions[index];
		}

		private FixedList8<TCallback>? _subscriptions;

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
			_subscriptions?.Clear();
		}

		/*
		===============
		Add
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="entry"></param>
		public void Add( TCallback entry )
		{
			_subscriptions ??= new FixedList8<TCallback>();
			_subscriptions.Add( entry );
		}

		/*
		===============
		RemoveAt
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt( int index )
		{
			_subscriptions ??= new FixedList8<TCallback>();
			_subscriptions.RemoveAtSwapBack( index );
		}
	};
};
