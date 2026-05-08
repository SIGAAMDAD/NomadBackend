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
using Nomad.Core.Events;
using System.Threading.Tasks;

namespace Nomad.Events.Private.EventTypes
{
	/*
	===================================================================================
	
	ScheduledEvent
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class ScheduledEvent<TArgs> : IGameEvent<TArgs>
		where TArgs : struct
	{
#if EVENT_DEBUG
		public TArgs LastPayload => _source.LastPayload;
		public int SubscriberCount => _source.SubscriberCount;
		public long PublishCount => _source.PublishCount;
		public DateTime LastPublishTime => _source.LastPublishTime;
#endif

		public string DebugName => _source.DebugName;
		public string NameSpace => _source.NameSpace;
		public int Id => _source.Id;

		public event EventCallback<TArgs> OnPublished {
			add => Subscribe( value );
			remove => Unsubscribe( value );
		}
		public event AsyncEventCallback<TArgs> OnPublishedAsync {
			add => SubscribeAsync( value );
			remove => UnsubscribeAsync( value );
		}

		private readonly GameEvent<TArgs> _source;
		private readonly IDisposable _scheduleHandle;

		private bool _isDisposed = false;

		/*
		===============
		ScheduledEvent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="payload"></param>
		/// <param name="publishIntervalMS"></param>
		public ScheduledEvent( IGameEvent<TArgs> source, TArgs payload, int publishIntervalMS )
		{
			_source = source as GameEvent<TArgs> ?? throw new InvalidCastException();
			_scheduleHandle = EventScheduler.ScheduleRecurring( () => {
				if ( _isDisposed ) {
					return;
				}
				source.Publish( in payload );
			}, publishIntervalMS );
		}

		/*
		===============
		ScheduledEvent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="payloadCallback"></param>
		/// <param name="publishIntervalMS"></param>
		public ScheduledEvent( IGameEvent<TArgs> source, Func<TArgs> payloadCallback, int publishIntervalMS )
		{
			_source = source as GameEvent<TArgs> ?? throw new InvalidCastException();
			_scheduleHandle = EventScheduler.ScheduleRecurring( () => {
				if ( _isDisposed ) {
					return;
				}
				source.Publish( payloadCallback.Invoke() );
			}, publishIntervalMS );
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
				_isDisposed = true;
				_scheduleHandle?.Dispose();
				_source.Dispose();
			}
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Publish
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventArgs"></param>
		public void Publish( in TArgs eventArgs )
		{
			_source.Publish( in eventArgs );
		}

		/*
		===============
		PublishAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventArgs"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public async Task PublishAsync( TArgs eventArgs, CancellationToken ct = default )
		{
			throw new NotSupportedException();
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public ISubscriptionHandle Subscribe( EventCallback<TArgs> callback )
		{
			return _source.Subscribe( callback );
		}

		/*
		===============
		SubscribeAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="asyncCallback"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public ISubscriptionHandle SubscribeAsync( AsyncEventCallback<TArgs> asyncCallback )
		{
			throw new NotSupportedException();
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		public void Unsubscribe( EventCallback<TArgs> callback )
		{
			_source.Unsubscribe( callback );
		}

		/*
		===============
		UnsubscribeAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="asyncCallback"></param>
		/// <exception cref="NotSupportedException"></exception>
		public void UnsubscribeAsync( AsyncEventCallback<TArgs> asyncCallback )
		{
			throw new NotSupportedException();
		}
	};
};
