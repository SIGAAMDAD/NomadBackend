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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Nomad.Core.Compatibility.Guards;

namespace Nomad.Events.Private.EventTypes
{
	/*
	===================================================================================
	
	EventScheduler
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal static class EventScheduler
	{
		private sealed class ScheduledItem : IDisposable
		{
			public ScheduledItem( Action callback, SynchronizationContext? context, int publisherThreadId, long dueTimestamp, long intervalTicks, long order )
			{
				Callback = callback;
				Context = context;
				PublisherThreadId = publisherThreadId;
				DueTimestamp = dueTimestamp;
				IntervalTicks = intervalTicks;
				Order = order;
			}

			public Action Callback { get; }
			public SynchronizationContext? Context { get; }
			public int PublisherThreadId { get; }
			public long IntervalTicks { get; }
			public long Order { get; }
			public long DueTimestamp { get; set; }

			public bool IsCanceled => Volatile.Read( ref _isCanceled ) != 0;
			private int _isCanceled;

			public void Dispose()
			{
				Interlocked.Exchange( ref _isCanceled, 1 );
			}
		};

		private static readonly object _gate = new();
		private static readonly List<ScheduledItem> _heap = new( 64 );
		private static readonly Timer _timer = new( OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite );
		private static readonly WaitCallback _executeScheduledItem = ExecuteScheduledItem;
		private static readonly SendOrPostCallback _postScheduledItem = PostScheduledItem;

		private static long _nextOrder;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IDisposable Schedule( Action callback, int delayMS )
		{
			return ScheduleCore( callback, delayMS, 0 );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IDisposable ScheduleRecurring( Action callback, int intervalMS )
		{
			return ScheduleCore( callback, intervalMS, intervalMS );
		}

		private static IDisposable ScheduleCore( Action callback, int dueMS, int intervalMS )
		{
			RangeGuard.ThrowIfNegative( dueMS, nameof( dueMS ) );
			RangeGuard.ThrowIfNegative( intervalMS, nameof( intervalMS ) );

			ScheduledItem item = new(
				callback ?? throw new ArgumentNullException( nameof( callback ) ),
				SynchronizationContext.Current,
				Environment.CurrentManagedThreadId,
				Stopwatch.GetTimestamp() + ToTimestampTicks( dueMS ),
				ToTimestampTicks( intervalMS ),
				Interlocked.Increment( ref _nextOrder ) );

			if ( dueMS == 0 ) {
				Dispatch( item );
				return item;
			}

			lock ( _gate ) {
				bool isNextDue = _heap.Count == 0 || Compare( item, _heap[0] ) < 0;
				HeapPush( item );

				if ( isNextDue ) {
					ArmTimer( item.DueTimestamp );
				}
			}

			return item;
		}

		private static void OnTimerElapsed( object state )
		{
			List<ScheduledItem> dueItems = null;

			lock ( _gate ) {
				long now = Stopwatch.GetTimestamp();
				while ( _heap.Count > 0 ) {
					ScheduledItem next = _heap[0];
					if ( next.IsCanceled ) {
						HeapPop();
						continue;
					}
					if ( next.DueTimestamp > now ) {
						break;
					}

					ScheduledItem dueItem = HeapPop();
					if ( dueItem.IsCanceled ) {
						continue;
					}

					dueItems ??= new List<ScheduledItem>( 4 );
					dueItems.Add( dueItem );
				}

				if ( _heap.Count > 0 ) {
					ArmTimer( _heap[0].DueTimestamp );
				} else {
					_timer.Change( Timeout.Infinite, Timeout.Infinite );
				}
			}

			if ( dueItems == null ) {
				return;
			}

			for ( int i = 0; i < dueItems.Count; ++i ) {
				Dispatch( dueItems[i] );
			}
		}

		private static void Dispatch( ScheduledItem item )
		{
			if ( item.IsCanceled ) {
				return;
			}

			if ( Environment.CurrentManagedThreadId == item.PublisherThreadId ) {
				ExecuteScheduledItem( item );
				return;
			}

			if ( item.Context != null ) {
				item.Context.Post( _postScheduledItem, item );
				return;
			}

			ThreadPool.QueueUserWorkItem( _executeScheduledItem, item );
		}

		private static void PostScheduledItem( object? state )
		{
			ExecuteScheduledItem( state! );
		}

		private static void ExecuteScheduledItem( object state )
		{
			ScheduledItem item = (ScheduledItem)state;
			if ( item.IsCanceled ) {
				return;
			}

			item.Callback();

			if ( item.IsCanceled || item.IntervalTicks <= 0 ) {
				return;
			}

			long nextDue = item.DueTimestamp + item.IntervalTicks;
			long now = Stopwatch.GetTimestamp();
			if ( nextDue <= now ) {
				nextDue = now + item.IntervalTicks;
			}
			item.DueTimestamp = nextDue;

			lock ( _gate ) {
				if ( item.IsCanceled ) {
					return;
				}

				bool isNextDue = _heap.Count == 0 || Compare( item, _heap[0] ) < 0;
				HeapPush( item );

				if ( isNextDue ) {
					ArmTimer( item.DueTimestamp );
				}
			}
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static long ToTimestampTicks( int milliseconds )
		{
			return milliseconds <= 0
				? 0
				: ((long)milliseconds * Stopwatch.Frequency + 999L) / 1000L;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static void ArmTimer( long dueTimestamp )
		{
			long remainingTicks = dueTimestamp - Stopwatch.GetTimestamp();
			int dueMS = remainingTicks <= 0
				? 0
				: (int)((remainingTicks * 1000L + Stopwatch.Frequency - 1L) / Stopwatch.Frequency);
			_timer.Change( dueMS, Timeout.Infinite );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static int Compare( ScheduledItem left, ScheduledItem right )
		{
			int dueCompare = left.DueTimestamp.CompareTo( right.DueTimestamp );
			return dueCompare != 0 ? dueCompare : left.Order.CompareTo( right.Order );
		}

		private static void HeapPush( ScheduledItem item )
		{
			_heap.Add( item );
			int index = _heap.Count - 1;

			while ( index > 0 ) {
				int parentIndex = (index - 1) >> 1;
				if ( Compare( item, _heap[parentIndex] ) >= 0 ) {
					break;
				}

				_heap[index] = _heap[parentIndex];
				index = parentIndex;
			}

			_heap[index] = item;
		}

		private static ScheduledItem HeapPop()
		{
			int lastIndex = _heap.Count - 1;
			ScheduledItem root = _heap[0];
			ScheduledItem last = _heap[lastIndex];
			_heap.RemoveAt( lastIndex );

			if ( lastIndex == 0 ) {
				return root;
			}

			int index = 0;
			int half = _heap.Count >> 1;
			while ( index < half ) {
				int leftIndex = (index << 1) + 1;
				int rightIndex = leftIndex + 1;
				int bestChildIndex = rightIndex < _heap.Count && Compare( _heap[rightIndex], _heap[leftIndex] ) < 0
					? rightIndex
					: leftIndex;

				if ( Compare( last, _heap[bestChildIndex] ) <= 0 ) {
					break;
				}

				_heap[index] = _heap[bestChildIndex];
				index = bestChildIndex;
			}

			_heap[index] = last;
			return root;
		}
	};
};
