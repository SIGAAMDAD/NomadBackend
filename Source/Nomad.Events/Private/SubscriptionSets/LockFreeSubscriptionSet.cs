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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.Logger;

namespace Nomad.Events.Private.SubscriptionSets
{
	/*
	===================================================================================

	LockFreeSubscriptionSet

	===================================================================================
	*/
	/// <summary>
	/// A completely lock free subscription set, this is meant for extremely low-latency high performance event pumping. This is explicitly meant to run single-threaded.
	/// </summary>
	/// <remarks>
	/// DO NOT MULTITHREAD THIS, YOU WILL GET RACE CONDITIONS!!!
	/// </remarks>
	internal sealed class LockFreeSubscriptionSet<TArgs> : SubscriptionSetBase<TArgs>
		where TArgs : struct
	{
		private readonly SubscriptionCache<TArgs, EventCallback<TArgs>> _genericSubscriptions;

		/*
		===============
		LockFreeSubscriptionSet
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="logger"></param>
		/// <param name="exceptionPolicy"></param>
		public LockFreeSubscriptionSet( IGameEvent<TArgs> eventData, ILoggerService logger, EventExceptionPolicy exceptionPolicy )
			: base( eventData, logger, exceptionPolicy )
		{
			_genericSubscriptions = new SubscriptionCache<TArgs, EventCallback<TArgs>>();
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
			_genericSubscriptions?.Dispose();
		}

		/*
		===============
		AddSubscription
		===============
		*/
		/// <summary>
		/// Adds a callback method to the subscriptions list.
		/// </summary>
		/// <param name="callback">The method that is called whenever the event triggers.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		public override bool AddSubscription( EventCallback<TArgs> callback )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( callback, nameof( callback ) );

			if ( ContainsCallback( callback, out _ ) ) {
				Logger?.PrintWarning( $"LockFreeSubscriptionSet.AddSubscription: subscription for callback '{callback.Method.Name}' already exists!" );
				return false;
			}

			_genericSubscriptions.Add( callback );
			IncrementSubscriberCount();
			return true;
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
		/// <exception cref="NotSupportedException"></exception>
		public override bool AddSubscriptionAsync( AsyncEventCallback<TArgs> callback )
		{
			throw new NotSupportedException( $"{nameof( LockFreeSubscriptionSet<TArgs> )} does not support async subscriptions." );
		}

		/*
		===============
		RemoveSubscription
		===============
		*/
		/// <summary>
		/// Removes the provided <paramref name="callback"/> from the event's subscription list.
		/// </summary>
		/// <param name="callback">The callback to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		public override bool RemoveSubscription( EventCallback<TArgs> callback )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( callback, nameof( callback ) );

			if ( !ContainsCallback( callback, out int index ) ) {
				Logger?.PrintWarning( $"LockFreeSubscriptionSet.RemoveSubscription: no such existing subscription for callback '{callback.Method.Name}'." );
				return false;
			}

			_genericSubscriptions.RemoveAt( index );
			DecrementSubscriberCount();
			return true;
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
		/// <exception cref="NotSupportedException"></exception>
		public override bool RemoveSubscriptionAsync( AsyncEventCallback<TArgs> callback )
		{
			throw new NotSupportedException( $"{nameof( LockFreeSubscriptionSet<TArgs> )} does not support async subscriptions." );
		}

		/*
		===============
		Pump
		===============
		*/
		/// <summary>
		/// "Publishes" an event to all subscribers.
		/// </summary>
		/// <param name="args"></param>
		public override void Pump( in TArgs args )
		{
			ThrowIfDisposed();
			if ( _genericSubscriptions.Count == 0 ) {
				return;
			}

			List<EventHandlerException>? failures = null;

			int count = _genericSubscriptions.Count;
			switch ( count ) {
				case 1:
					PumpEvent( _genericSubscriptions[0], 0, in args, failures );
					break;

				case 2:
					PumpEvent( _genericSubscriptions[0], 0, in args, failures );
					PumpEvent( _genericSubscriptions[1], 1, in args, failures );
					break;

				case 3:
					PumpEvent( _genericSubscriptions[0], 0, in args, failures );
					PumpEvent( _genericSubscriptions[1], 1, in args, failures );
					PumpEvent( _genericSubscriptions[2], 2, in args, failures );
					break;

				case 4:
					PumpEvent( _genericSubscriptions[0], 0, in args, failures );
					PumpEvent( _genericSubscriptions[1], 1, in args, failures );
					PumpEvent( _genericSubscriptions[2], 2, in args, failures );
					PumpEvent( _genericSubscriptions[3], 3, in args, failures );
					break;

				default:
					for ( int i = 0; i < count; i++ ) {
						PumpEvent( _genericSubscriptions[i], i, in args, failures );
					}
					break;
			}

			IncrementPublishCount();
			CheckAggregateException( failures );
		}

		private void PumpEvent( EventCallback<TArgs> callback, int index, in TArgs args, List<EventHandlerException> failures )
		{
			var ex = InvokeCallback( callback, index, in args );
			if ( ex != null ) {
				failures ??= new();
				failures.Add( ex );
			}
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
		/// <exception cref="NotSupportedException"></exception>
		public override Task PumpAsync( TArgs args, CancellationToken ct )
		{
			throw new NotSupportedException( $"{nameof( LockFreeSubscriptionSet<TArgs> )} does not support async pumping." );
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
			return TryFindCallback( _genericSubscriptions, callback, out index );
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
		/// <exception cref="NotSupportedException"></exception>
		public override bool ContainsCallbackAsync( AsyncEventCallback<TArgs> callback, out int index )
		{
			throw new NotSupportedException( $"{nameof( LockFreeSubscriptionSet<TArgs> )} does not support async subscriptions." );
		}
	};
};
