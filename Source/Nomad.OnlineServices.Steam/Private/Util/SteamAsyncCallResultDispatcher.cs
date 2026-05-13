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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Logger;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Util
{
	/*
	===================================================================================

	SteamAsyncCallResultDispatcher

	===================================================================================
	*/
	/// <summary>
	/// Handles asynchronous Steamworks CallResult operations.
	///
	/// This dispatcher is single-flight: one active Steam async call at a time.
	/// Cache one dispatcher per Steam operation, such as:
	/// SteamAsyncCallResultDispatcher&lt;LobbyCreated_t, LobbyData&gt;.
	/// </summary>

	internal sealed class SteamAsyncCallResultDispatcher<TCallbackArgs, TResult> : IDisposable
		where TCallbackArgs : struct
	{
		private readonly CallResult<TCallbackArgs> _callback;
		private readonly CallResult<TCallbackArgs>.APIDispatchDelegate _callbackDelegate;

		private readonly object _requestLock = new object();

		private readonly SynchronizationContext _mainContext;

		private readonly ILoggerCategory _category;

		private TaskCompletionSource<TCallbackArgs>? _currentTcs = null;
		private Task<TResult>? _currentRequest = null;

		private bool _isDisposed = false;

		/*
		===============
		SteamAsyncCallResultDispatcher
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="category"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamAsyncCallResultDispatcher( ILoggerCategory category )
			: this( SynchronizationContext.Current, category )
		{
			_category = category ?? throw new ArgumentNullException( nameof( category ) );
		}

		/*
		===============
		SteamAsyncCallResultDispatcher
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="mainContext"></param>
		/// <param name="category"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public SteamAsyncCallResultDispatcher( SynchronizationContext? mainContext, ILoggerCategory category )
		{
			_mainContext = mainContext
				?? throw new InvalidOperationException(
					"SteamAsyncCallResultDispatcher must be created with a valid main/Steam SynchronizationContext."
				);

			_callbackDelegate = OnCallback;
			_callback = CallResult<TCallbackArgs>.Create( _callbackDelegate );
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
			if ( _isDisposed ) {
				return;
			}

			TaskCompletionSource<TCallbackArgs>? tcs = null;

			lock ( _requestLock ) {
				if ( _isDisposed ) {
					return;
				}

				_isDisposed = true;

				tcs = _currentTcs;
				_currentTcs = null;
			}

			tcs?.TrySetCanceled();

			try {
				_callback.Cancel();
			} catch ( Exception ex ) {
				_category.PrintWarning( $"Failed to cancel Steam CallResult during dispose: {ex}" );
			}

			_callback.Dispose();

			GC.SuppressFinalize( this );
		}

		/*
		===============
		Invoke
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="steamCall"></param>
		/// <param name="resultFactory"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public Task<TResult> Invoke(
			Func<SteamAPICall_t> steamCall,
			Func<TCallbackArgs, TResult> resultFactory,
			CancellationToken ct = default )
		{
			ArgumentGuard.ThrowIfNull( steamCall, nameof( steamCall ) );
			ArgumentGuard.ThrowIfNull( resultFactory, nameof( resultFactory ) );

			lock ( _requestLock ) {
				ThrowIfDisposed();

				if ( _currentRequest != null && !_currentRequest.IsCompleted ) {
					return _currentRequest;
				}

				_currentRequest = InvokeInternal( steamCall, resultFactory, ct );
				return _currentRequest;
			}
		}

		/*
		===============
		InvokeInternal
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="steamCall"></param>
		/// <param name="resultFactory"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<TResult> InvokeInternal(
			Func<SteamAPICall_t> steamCall,
			Func<TCallbackArgs, TResult> resultFactory,
			CancellationToken ct )
		{
			TaskCompletionSource<TCallbackArgs> tcs = new TaskCompletionSource<TCallbackArgs>(
				TaskCreationOptions.RunContinuationsAsynchronously
			);

			lock ( _requestLock ) {
				_currentTcs = tcs;
			}

			using CancellationTokenRegistration cancellationRegistration = ct.Register( () => {
				TaskCompletionSource<TCallbackArgs>? localTcs = null;

				lock ( _requestLock ) {
					localTcs = _currentTcs;
					_currentTcs = null;
				}

				try {
					_mainContext.Post( _ => {
						try {
							_callback.Cancel();
						} catch ( Exception ex ) {
							_category.PrintWarning( $"Failed to cancel Steam CallResult: {ex}" );
						}
					}, null );
				} catch {
					// If the context is already gone, cancellation should still complete the task.
				}

				localTcs?.TrySetCanceled( ct );
			} );

			PostSteamCallToMainThread( steamCall, tcs );

			try {
				TCallbackArgs callbackArgs = await tcs.Task.ConfigureAwait( false );
				return resultFactory( callbackArgs );
			} finally {
				lock ( _requestLock ) {
					if ( ReferenceEquals( _currentTcs, tcs ) ) {
						_currentTcs = null;
					}
				}
			}
		}

		/*
		===============
		PostSteamCallToMainThread
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="steamCall"></param>
		/// <param name="tcs"></param>
		private void PostSteamCallToMainThread(
			Func<SteamAPICall_t> steamCall,
			TaskCompletionSource<TCallbackArgs> tcs )
		{
			_mainContext.Post( _ => {
				try {
					SteamAPICall_t call = steamCall();

					if ( call == SteamAPICall_t.Invalid ) {
						tcs.TrySetException(
							new InvalidOperationException( "Steam async call returned SteamAPICall_t.Invalid." )
						);
						return;
					}

					_callback.Set( call );
				} catch ( Exception ex ) {
					tcs.TrySetException( ex );
				}
			}, null );
		}

		/*
		===============
		OnCallback
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		/// <param name="bIOFailure"></param>
		private void OnCallback( TCallbackArgs pCallback, bool bIOFailure )
		{
			TaskCompletionSource<TCallbackArgs>? tcs;

			lock ( _requestLock ) {
				tcs = _currentTcs;
				_currentTcs = null;
			}

			if ( tcs == null ) {
				return;
			}

			if ( bIOFailure ) {
				tcs.TrySetException(
					new InvalidOperationException(
						$"Steam async call failed with bIOFailure=true for {typeof( TCallbackArgs ).Name}."
					)
				);
				return;
			}

			tcs.TrySetResult( pCallback );
		}

		/*
		===============
		ThrowIfDisposed
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <exception cref="ObjectDisposedException"></exception>
		private void ThrowIfDisposed()
		{
			if ( _isDisposed ) {
				throw new ObjectDisposedException( nameof( SteamAsyncCallResultDispatcher<TCallbackArgs, TResult> ) );
			}
		}
	};
};
