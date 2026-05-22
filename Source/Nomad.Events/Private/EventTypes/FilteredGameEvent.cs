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
using Nomad.Events.Extensions;

namespace Nomad.Events.Private.EventTypes
{
	/*
	===================================================================================

	FilteredGameEvent

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FilteredGameEvent<TArgs> : IGameEvent<TArgs>
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
		private readonly FilteredEventPredicate<TArgs> _predicate;
		private readonly Dictionary<Delegate, Delegate> _handlerMap = new();

		private bool _isDisposed = false;

		/*
		===============
		FilteredGameEvent
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <param name="predicate"></param>
		public FilteredGameEvent( IGameEvent<TArgs> source, FilteredEventPredicate<TArgs> predicate )
		{
			ArgumentGuard.ThrowIfNull( source, nameof( source ) );
			ArgumentGuard.ThrowIfNull( predicate, nameof( predicate ) );

			_source = source as GameEvent<TArgs> ?? throw new InvalidCastException();
			_predicate = predicate;
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
				_source.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
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
		public Task PublishAsync( TArgs eventArgs, CancellationToken ct = default )
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
			EventCallback<TArgs> wrapped = ( in TArgs args ) => {
				if ( _predicate.Invoke( in args ) ) {
					callback.Invoke( in args );
				}
			};
			_handlerMap[callback] = wrapped;
			return _source.Subscribe( wrapped );
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
			if ( _handlerMap.TryGetValue( callback, out var wrapped ) ) {
				_source.Unsubscribe( (EventCallback<TArgs>)wrapped );
				_handlerMap.Remove( callback );
			}
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
