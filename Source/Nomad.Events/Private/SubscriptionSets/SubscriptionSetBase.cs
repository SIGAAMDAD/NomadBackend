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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.Logger;

namespace Nomad.Events.Private.SubscriptionSets
{
	/*
	===================================================================================

	SubscriptionSetBase

	===================================================================================
	*/
	/// <summary>
	/// Shared base class for subscription set implementations.
	/// Handles common lifecycle, counters, event metadata, and callback lookup helpers.
	/// </summary>
	internal abstract class SubscriptionSetBase<TArgs> : ISubscriptionSet<TArgs>
		where TArgs : struct
	{
		public int SubscriberCount => _subscriberCount;
		private int _subscriberCount = 0;

		public long PublishCount => _publishCount;
		private long _publishCount = 0;

		protected EventExceptionPolicy ExceptionPolicy { get; }

		protected ILoggerService Logger { get; }
		protected IGameEvent<TArgs> EventData { get; }

		protected bool isDisposed { get; private set; } = false;

		/*
		===============
		SubscriptionSetBase
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="logger"></param>
		/// <param name="exceptionPolicy"></param>
		/// <exception cref="ArgumentNullException"></exception>
		protected SubscriptionSetBase( IGameEvent<TArgs> eventData, ILoggerService logger, EventExceptionPolicy exceptionPolicy )
		{
			EventData = eventData ?? throw new ArgumentNullException( nameof( eventData ) );
			Logger = logger ?? throw new ArgumentNullException( nameof( logger ) );
			ExceptionPolicy = exceptionPolicy;
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
			if ( isDisposed ) {
				return;
			}

			Logger?.PrintLine( $"Releasing subscription set for event {EventData.DebugName}..." );
			OnDispose();

			isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		OnDispose
		===============
		*/
		/// <summary>
		/// Allows derived classes to dispose owned resources.
		/// </summary>
		protected abstract void OnDispose();

		/*
		===============
		ThrowIfDisposed
		===============
		*/
		/// <summary>
		///
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		protected void ThrowIfDisposed()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
		}

		/*
		===============
		IncrementSubscriberCount
		===============
		*/
		/// <summary>
		///
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		protected void IncrementSubscriberCount()
		{
			Interlocked.Increment( ref _subscriberCount );
		}

		/*
		===============
		DecrementSubscriberCount
		===============
		*/
		/// <summary>
		///
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		protected void DecrementSubscriberCount()
		{
			Interlocked.Decrement( ref _subscriberCount );
		}

		/*
		===============
		IncrementPublishCount
		===============
		*/
		/// <summary>
		///
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		protected void IncrementPublishCount()
		{
			Interlocked.Increment( ref _publishCount );
		}

		/*
		===============
		TryFindCallback
		===============
		*/
		/// <summary>
		/// Shared callback lookup helper used by concrete implementations.
		/// </summary>
		/// <typeparam name="TCallback"></typeparam>
		/// <param name="subscriptions"></param>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		protected static bool TryFindCallback<TCallback>(
			SubscriptionCache<TArgs, TCallback> subscriptions,
			TCallback callback,
			out int index
		)
		{
			for ( int i = 0; i < subscriptions.Count; i++ ) {
				if ( EqualityComparer<TCallback>.Default.Equals( subscriptions[i], callback ) ) {
					index = i;
					return true;
				}
			}
			index = -1;
			return false;
		}

		public abstract bool AddSubscription( EventCallback<TArgs> callback );
		public abstract bool AddSubscriptionAsync( AsyncEventCallback<TArgs> callback );

		public abstract bool RemoveSubscription( EventCallback<TArgs> callback );
		public abstract bool RemoveSubscriptionAsync( AsyncEventCallback<TArgs> callback );

		/// <remarks>
		/// Publishing an event should never silently swallow subscriber exceptions.
		/// The publisher must either propagate, aggregate, or report them through the framework
		/// logger/diagnostics channel according to the event’s configured exception policy.
		/// </remarks>
		/// <param name="args"></param>
		public abstract void Pump( in TArgs args );

		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public abstract Task PumpAsync( TArgs args, CancellationToken ct );

		public abstract bool ContainsCallback( EventCallback<TArgs> callback, out int index );
		public abstract bool ContainsCallbackAsync( AsyncEventCallback<TArgs> callback, out int index );

		/*
		===============
		PumpAsyncSnapshot
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="subscriptions"></param>
		/// <param name="subscriptionCount"></param>
		/// <param name="args"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="EventPublishException"></exception>
		protected async Task PumpAsyncSnapshot( AsyncEventCallback<TArgs>[] subscriptions, int subscriptionCount, TArgs args, CancellationToken ct )
		{
			ct.ThrowIfCancellationRequested();

			if ( subscriptionCount == 0 ) {
				IncrementPublishCount();
				return;
			}

			if ( subscriptionCount == 1 ) {
				await PumpSingleAsync( subscriptions[0], 0, args, ct ).ConfigureAwait( false );
				IncrementPublishCount();
				return;
			}

			Task[] tasks = ArrayPool<Task>.Shared.Rent( subscriptionCount );
			int taskCount = 0;
			List<EventHandlerException>? failures = null;

			try {
				for ( int i = 0; i < subscriptionCount; i++ ) {
					ct.ThrowIfCancellationRequested();

					try {
						Task task = subscriptions[i].Invoke( args, ct ) ?? Task.CompletedTask;
						tasks[taskCount++] = task;
					} catch ( Exception ex ) {
						failures ??= new List<EventHandlerException>();
						failures.Add( CreateAsyncFailure( subscriptions[i], i, ex ) );

						if ( ExceptionPolicy == EventExceptionPolicy.Propagate ) {
							break;
						}
					}
				}

				if ( taskCount > 0 ) {
#if NET10_0_OR_GREATER
					Task whenAll = Task.WhenAll( tasks.AsSpan( 0, taskCount ) );
#else
					Task whenAll = Task.WhenAll( tasks.Take( taskCount ) );
#endif

					try {
						await whenAll.ConfigureAwait( false );
					} catch {
						failures ??= new List<EventHandlerException>();

						for ( int i = 0; i < taskCount; i++ ) {
							Task task = tasks[i];

							if ( !task.IsFaulted || task.Exception == null ) {
								continue;
							}

							foreach ( Exception ex in task.Exception.InnerExceptions ) {
								failures.Add( CreateAsyncFailure( subscriptions[i], i, ex ) );
							}
						}
					}
				}

				if ( failures is { Count: > 0 } ) {
					foreach ( EventHandlerException failure in failures ) {
						Logger?.PrintError( failure.ToString() );
					}

					if ( ExceptionPolicy != EventExceptionPolicy.ReportAndContinue ) {
						throw new EventPublishException(
							EventData.DebugName,
							typeof( TArgs ),
							failures
						);
					}
				}

				IncrementPublishCount();
			} finally {
				Array.Clear( tasks, 0, taskCount );
				ArrayPool<Task>.Shared.Return( tasks );
			}
		}

		/*
		===============
		PumpSingleAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <param name="args"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="EventPublishException"></exception>
		private async Task PumpSingleAsync( AsyncEventCallback<TArgs> callback, int index, TArgs args, CancellationToken ct )
		{
			try {
				Task task = callback.Invoke( args, ct ) ?? Task.CompletedTask;
				await task.ConfigureAwait( false );
			} catch ( Exception ex ) {
				var failure = CreateAsyncFailure( callback, index, ex );

				Logger?.PrintError( failure.ToString() );

				if ( ExceptionPolicy != EventExceptionPolicy.ReportAndContinue ) {
					throw new EventPublishException(
						EventData.DebugName,
						typeof( TArgs ),
						new List<EventHandlerException> { failure }
					);
				}
			}
		}

		/*
		===============
		CreateAsyncFailure
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <param name="ex"></param>
		/// <returns></returns>
		private EventHandlerException CreateAsyncFailure( AsyncEventCallback<TArgs> callback, int index, Exception ex )
		{
			return new EventHandlerException(
				EventData.DebugName,
				typeof( TArgs ),
				callback.Method.Name,
				index,
				ex
			);
		}

		/*
		===============
		InvokeCallback
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="index"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		protected EventHandlerException? InvokeCallback( EventCallback<TArgs> callback, int index, in TArgs args )
		{
			EventHandlerException? failure = null;

			try {
				callback.Invoke( in args );
			} catch ( Exception ex ) {
				failure = new EventHandlerException(
					eventName: EventData.DebugName,
					argsType: typeof( TArgs ),
					handlerName: callback.Method.Name,
					handlerIndex: index,
					innerException: ex
				);

				Logger?.PrintLine(
					$"Event subscriber exception: event='{EventData.DebugName}', " +
					$"handler='{callback.Method.DeclaringType?.FullName}.{callback.Method.Name}', " +
					$"exception='{ex.GetType().Name}: {ex.Message}'"
				);

				if ( ExceptionPolicy == EventExceptionPolicy.Propagate ) {
					throw failure;
				}
			}

			return failure;
		}

		/*
		===============
		CheckAggregateException
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="failures"></param>
		/// <exception cref="EventPublishException"></exception>
		protected void CheckAggregateException( List<EventHandlerException> failures )
		{
			if ( failures is { Count: > 0 } && ExceptionPolicy == EventExceptionPolicy.AggregateAfterDispatch ) {
				throw new EventPublishException(
					EventData.DebugName,
					typeof( TArgs ),
					failures
				);
			}
		}
	};
};
