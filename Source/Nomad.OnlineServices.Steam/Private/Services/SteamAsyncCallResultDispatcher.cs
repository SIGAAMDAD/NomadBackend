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
using Nomad.Core.Logger;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================
	
	SteamAsyncCallResultDispatcher
	
	===================================================================================
	*/
	/// <summary>
	/// Handles asynchronous calls into the Steamworks API
	/// </summary>

	internal sealed class SteamAsyncCallResultDispatcher<TCallbackArgs, TResult> : IDisposable
		where TCallbackArgs : struct
	{
		private readonly CallResult<TCallbackArgs> _callback;

		private readonly object _requestLock = new object();
		private TaskCompletionSource<TCallbackArgs>? _currentTcs;
		private Task<TResult>? _currentRequest;

		private readonly SynchronizationContext _mainContext;

		private bool _isDisposed = false;

		/*
		===============
		SteamAsyncCallResultDispatcher
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public SteamAsyncCallResultDispatcher()
		{
			_mainContext = SynchronizationContext.Current ?? new SynchronizationContext();
			_callback = CallResult<TCallbackArgs>.Create( OnCallback );
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
				// kill the retrieval thread if there is any
				lock ( _requestLock ) {
					_currentTcs?.TrySetCanceled();
					_currentTcs = null;
				}
				_callback?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Invoke
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="steamCallback"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<TResult> Invoke( Func<TCallbackArgs, TResult> callback, Action? steamCallback = null, CancellationToken ct = default )
		{
			if ( _currentRequest != null && !_currentRequest.IsCompleted ) {
				return await _currentRequest;
			}
			lock ( _requestLock ) {
				_currentRequest = InvokeInternal( callback, steamCallback, ct );
			}
			return await _currentRequest;
		}

		/*
		===============
		InvokeInternal
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="steamCallback"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<TResult> InvokeInternal( Func<TCallbackArgs, TResult> callback, Action? steamCallback, CancellationToken ct )
		{
			_currentTcs = new TaskCompletionSource<TCallbackArgs>();

			if ( steamCallback != null ) {
				_mainContext.Post( _ => {
					try {
						steamCallback.Invoke();
					} catch ( Exception ex ) {
						_currentTcs.TrySetException( ex );
					}
				}, null );
			}
			try {
				using ( ct.Register( () => {
					lock ( _requestLock ) {
						_currentTcs?.TrySetCanceled( ct );
						_currentTcs = null;
					}
				} ) ) {
					var result = await _currentTcs.Task.ConfigureAwait( false );
					return callback.Invoke( result );
				}
			} finally {
				lock ( _requestLock ) {
					_currentTcs = null;
				}
			}
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
			TaskCompletionSource<TCallbackArgs>? tcs = null;
			lock ( _requestLock ) {
				tcs = _currentTcs;
				_currentTcs = null;
			}
			tcs?.TrySetResult( pCallback );
		}
	}
}
