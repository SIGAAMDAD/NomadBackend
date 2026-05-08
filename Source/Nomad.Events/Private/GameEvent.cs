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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Events.Private.SubscriptionSets;

namespace Nomad.Events.Private
{
	/*
	===================================================================================

	GameEvent

	===================================================================================
	*/
	/// <summary>
	/// <para>Generic class for managing an event.</para>
	/// <para>Inherit from this class to pass data between modules and events, if not needed, simply name the event, then publish it.</para>
	/// </summary>

	internal sealed class GameEvent<TArgs> : IGameEvent<TArgs>
		where TArgs : struct
	{
		/// <summary>
		/// The name of the event.
		/// </summary>
		public string DebugName => _name;
		private readonly InternString _name;

		/// <summary>
		/// The event's namespace.
		/// </summary>
		public string NameSpace => _nameSpace;
		private readonly InternString _nameSpace;

		/// <summary>
		/// The <see cref="EventKey"/> but in pre-hashed format without the baggage of a struct.
		/// </summary>
		public int Id => _hashCode;
		private readonly int _hashCode;

#if EVENT_DEBUG
		public TArgs LastPayload => _lastPayload;
		private TArgs _lastPayload;

		public DateTime LastPublishTime => _lastPublishTime;
		private DateTime _lastPublishTime;

		public int SubscriberCount => _subscriptions.SubscriberCount;
		public long PublishCount => _subscriptions.PublishCount;
#endif

		private bool _isDisposed = false;

		public event EventCallback<TArgs> OnPublished {
			add => Subscribe( value );
			remove => Unsubscribe( value );
		}
		public event AsyncEventCallback<TArgs> OnPublishedAsync {
			add => SubscribeAsync( value );
			remove => UnsubscribeAsync( value );
		}

		internal SubscriptionSetBase<TArgs> SubscriptionSet => _subscriptions;
		private readonly SubscriptionSetBase<TArgs> _subscriptions;

		/*
		===============
		GameEvent
		===============
		*/
		/// <summary>
		/// Creates a new GameEvent object with the debugging alias of <paramref name="name"/>.
		/// </summary>
		/// <param name="nameSpace"></param>
		/// <param name="name">The name of the event, should be unique.</param>
		/// <param name="logger"></param>
		/// <param name="flags"></param>
		/// <exception cref="ArgumentException">Thrown if name is null or empty.</exception>
		internal GameEvent( InternString nameSpace, InternString name, ILoggerService logger, EventFlags flags )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			_nameSpace = nameSpace;
			_name = name;
			_hashCode = HashCode.Combine( _name.GetHashCode(), nameSpace.GetHashCode(), typeof( TArgs ).TypeHandle.ToString() );

			bool isSynchronous = flags.HasFlag( EventFlags.Synchronous );
			bool isAsync = flags.HasFlag( EventFlags.Asynchronous );
			bool lockFree = flags.HasFlag( EventFlags.NoLock );
			bool atomicSafe = flags.HasFlag( EventFlags.AtomicSafe );

			if ( lockFree && isAsync ) {
				throw new NotSupportedException( "Cannot have an event that is both lock free and asynchronous!" );
			}

			if ( lockFree ) {
				_subscriptions = new LockFreeSubscriptionSet<TArgs>( this, logger, EventExceptionPolicy.ReportAndContinue );
			} else if ( atomicSafe ) {
				_subscriptions = new AtomicSubscriptionSet<TArgs>( this, logger, EventExceptionPolicy.AggregateAfterDispatch );
			} else if ( isAsync ) {
				_subscriptions = new SubscriptionSet<TArgs>( this, logger, EventExceptionPolicy.AggregateAfterDispatch );
			} else if ( isSynchronous ) {
				_subscriptions = new SubscriptionSet<TArgs>( this, logger, EventExceptionPolicy.AggregateAfterDispatch );
			} else {
				throw new InvalidOperationException();
			}
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
				_subscriptions.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Equals
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( IGameEvent? other )
		{
			return other != null && _hashCode == other.Id;
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
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Publish( in TArgs eventArgs )
		{
#if EVENT_DEBUG
			_lastPayload = eventArgs;
			_lastPublishTime = DateTime.Now;
#endif
			_subscriptions.Pump( in eventArgs );
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
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public async Task PublishAsync( TArgs eventArgs, CancellationToken ct = default )
		{
			await _subscriptions.PumpAsync( eventArgs, ct );
		}


		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ISubscriptionHandle Subscribe( EventCallback<TArgs> callback )
		{
			ArgumentGuard.ThrowIfNull( callback );
			return new SubscriptionHandle<TArgs>( _subscriptions, callback );
		}

		/*
		===============
		Subscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback">The lambda or method to call when the event is triggered.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ISubscriptionHandle SubscribeAsync( AsyncEventCallback<TArgs> callback )
		{
			ArgumentGuard.ThrowIfNull( callback );
			return new AsyncSubscriptionHandle<TArgs>( _subscriptions, callback );
		}

		/*
		===============
		Unsubscribe
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback">The lambda or method to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Unsubscribe( EventCallback<TArgs> callback )
		{
			ArgumentGuard.ThrowIfNull( callback );
			_subscriptions.RemoveSubscription( callback );
		}

		/*
		===============
		UnsubscribeAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback">The lambda or method to remove from the subscription list.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void UnsubscribeAsync( AsyncEventCallback<TArgs> callback )
		{
			ArgumentGuard.ThrowIfNull( callback );
			_subscriptions.RemoveSubscriptionAsync( callback );
		}
	};
};
