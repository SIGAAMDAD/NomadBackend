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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.Logger;

namespace Nomad.Events.Private.SubscriptionSets
{
	/*
	===================================================================================

	AtomicSubscriptionSet

	===================================================================================
	*/
	/// <summary>
	/// Thread-safe subscription set with lock-free reads and copy-on-write updates.
	/// </summary>

	internal sealed class AtomicSubscriptionSet<TArgs> : SubscriptionSetBase<TArgs>
		where TArgs : struct
	{
		private static readonly EventCallback<TArgs>[] _emptySubscriptions = Array.Empty<EventCallback<TArgs>>();
		private static readonly AsyncEventCallback<TArgs>[] _emptyAsyncSubscriptions = Array.Empty<AsyncEventCallback<TArgs>>();

		private EventCallback<TArgs>[] _genericSubscriptions;
		private AsyncEventCallback<TArgs>[] _asyncSubscriptions;

		/*
		===============
		AtomicSubscriptionSet
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="logger"></param>
		/// <param name="exceptionPolicy"></param>
		public AtomicSubscriptionSet( IGameEvent<TArgs> eventData, ILoggerService logger, EventExceptionPolicy exceptionPolicy )
			: base( eventData, logger, exceptionPolicy )
		{
			_genericSubscriptions = _emptySubscriptions;
			_asyncSubscriptions = _emptyAsyncSubscriptions;
		}

		/*
		===============
		OnDispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		protected override void OnDispose()
		{
			Interlocked.Exchange( ref _genericSubscriptions, _emptySubscriptions );
			Interlocked.Exchange( ref _asyncSubscriptions, _emptyAsyncSubscriptions );
		}

		/*
		===============
		AddSubscription
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public override bool AddSubscription( EventCallback<TArgs> callback )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( callback );

			while ( true ) {
				EventCallback<TArgs>[] current = Volatile.Read( ref _genericSubscriptions );

				if ( IndexOf( current, callback ) >= 0 ) {
					Logger?.PrintWarning( $"AtomicSubscriptionSet.AddSubscription: subscription for callback '{callback.Method.Name}' already exists!" );
					return false;
				}

				EventCallback<TArgs>[] updated = new EventCallback<TArgs>[current.Length + 1];
				Array.Copy( current, updated, current.Length );
				updated[current.Length] = callback;

				if ( ReferenceEquals( Interlocked.CompareExchange( ref _genericSubscriptions, updated, current ), current ) ) {
					IncrementSubscriberCount();
					return true;
				}
			}
		}

		/*
		===============
		AddSubscriptionAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public override bool AddSubscriptionAsync( AsyncEventCallback<TArgs> callback )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( callback );

			while ( true ) {
				AsyncEventCallback<TArgs>[] current = Volatile.Read( ref _asyncSubscriptions );

				if ( IndexOf( current, callback ) >= 0 ) {
					Logger?.PrintWarning( $"AtomicSubscriptionSet.AddSubscriptionAsync: subscription for callback '{callback.Method.Name}' already exists!" );
					return false;
				}

				AsyncEventCallback<TArgs>[] updated = new AsyncEventCallback<TArgs>[current.Length + 1];
				Array.Copy( current, updated, current.Length );
				updated[current.Length] = callback;

				if ( ReferenceEquals( Interlocked.CompareExchange( ref _asyncSubscriptions, updated, current ), current ) ) {
					IncrementSubscriberCount();
					return true;
				}
			}
		}

		/*
		===============
		RemoveSubscription
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public override bool RemoveSubscription( EventCallback<TArgs> callback )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( callback );

			while ( true ) {
				EventCallback<TArgs>[] current = Volatile.Read( ref _genericSubscriptions );
				int index = IndexOf( current, callback );
				if ( index < 0 ) {
					Logger?.PrintWarning( $"AtomicSubscriptionSet.RemoveSubscription: no such existing subscription for callback '{callback.Method.Name}'." );
					return false;
				}

				EventCallback<TArgs>[] updated = RemoveAt( current, index );
				if ( ReferenceEquals( Interlocked.CompareExchange( ref _genericSubscriptions, updated, current ), current ) ) {
					DecrementSubscriberCount();
					return true;
				}
			}
		}

		/*
		===============
		RemoveSubscriptionAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public override bool RemoveSubscriptionAsync( AsyncEventCallback<TArgs> callback )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( callback );

			while ( true ) {
				AsyncEventCallback<TArgs>[] current = Volatile.Read( ref _asyncSubscriptions );
				int index = IndexOf( current, callback );
				if ( index < 0 ) {
					Logger?.PrintWarning( $"AtomicSubscriptionSet.RemoveSubscriptionAsync: no such existing subscription for callback '{callback.Method.Name}'." );
					return false;
				}

				AsyncEventCallback<TArgs>[] updated = RemoveAt( current, index );
				if ( ReferenceEquals( Interlocked.CompareExchange( ref _asyncSubscriptions, updated, current ), current ) ) {
					DecrementSubscriberCount();
					return true;
				}
			}
		}

		/*
		===============
		Pump
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public override void Pump( in TArgs args )
		{
			ThrowIfDisposed();

			EventCallback<TArgs>[] subscriptions = Volatile.Read( ref _genericSubscriptions );
			List<EventHandlerException>? failures = null;

			for ( int i = 0; i < subscriptions.Length; i++ ) {
				var callback = subscriptions[i];
				var ex = InvokeCallback( callback, i, in args );
				if ( ex != null ) {
					failures ??= new();
					failures.Add( ex );
				}
			}

			IncrementPublishCount();
			CheckAggregateException( failures );
		}

		/*
		===============
		PumpAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public override async Task PumpAsync( TArgs args, CancellationToken ct )
		{
			ThrowIfDisposed();

			AsyncEventCallback<TArgs>[] subscriptions = Volatile.Read( ref _asyncSubscriptions );
			await PumpAsyncSnapshot( subscriptions, subscriptions.Length, args, ct ).ConfigureAwait( false );
		}

		/*
		===============
		ContainsCallback
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public override bool ContainsCallback( EventCallback<TArgs> callback, out int index )
		{
			ThrowIfDisposed();
			index = IndexOf( Volatile.Read( ref _genericSubscriptions ), callback );
			return index >= 0;
		}

		/*
		===============
		ContainsCallbackAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public override bool ContainsCallbackAsync( AsyncEventCallback<TArgs> callback, out int index )
		{
			ThrowIfDisposed();
			index = IndexOf( Volatile.Read( ref _asyncSubscriptions ), callback );
			return index >= 0;
		}

		/*
		===============
		IndexOf
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TCallback"></typeparam>
		/// <param name="subscriptions"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		private static int IndexOf<TCallback>( TCallback[] subscriptions, TCallback callback )
		{
			for ( int i = 0; i < subscriptions.Length; i++ ) {
				if ( Equals( subscriptions[i], callback ) ) {
					return i;
				}
			}
			return -1;
		}

		/*
		===============
		RemoveAt
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TCallback"></typeparam>
		/// <param name="subscriptions"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static TCallback[] RemoveAt<TCallback>( TCallback[] subscriptions, int index )
		{
			if ( subscriptions.Length == 1 ) {
				return Array.Empty<TCallback>();
			}

			TCallback[] updated = new TCallback[subscriptions.Length - 1];
			if ( index > 0 ) {
				Array.Copy( subscriptions, 0, updated, 0, index );
			}

			int tailLength = subscriptions.Length - index - 1;
			if ( tailLength > 0 ) {
				Array.Copy( subscriptions, index + 1, updated, index, tailLength );
			}

			return updated;
		}
	};
};
