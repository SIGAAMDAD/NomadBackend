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
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Util
{
	/*
	===================================================================================

	SteamAsyncImageHandleDispatcher

	===================================================================================
	*/
	/// <summary>
	/// Adapts Steam image-handle APIs that return an int handle immediately but may
	/// complete later through a regular Steam Callback&lt;TCallbackArgs&gt;.
	///
	/// Examples:
	/// - SteamFriends.GetSmallFriendAvatar(...) + AvatarImageLoaded_t
	/// - SteamUserStats.GetAchievementIcon(...) + UserAchievementIconFetched_t
	/// </summary>

	internal sealed class SteamAsyncImageHandleDispatcher<TCallbackArgs, TKey> : IDisposable
		where TCallbackArgs : struct
		where TKey : notnull
	{
		private sealed class PendingRequest
		{
			public readonly TKey Key;
			public readonly TaskCompletionSource<int> Completion;
			public readonly CancellationTokenRegistration CancellationRegistration;

			private int _completed;

			public PendingRequest(
				TKey key,
				TaskCompletionSource<int> completion,
				CancellationTokenRegistration cancellationRegistration )
			{
				Key = key;
				Completion = completion;
				CancellationRegistration = cancellationRegistration;
			}

			public bool TryComplete()
			{
				return Interlocked.Exchange( ref _completed, 1 ) == 0;
			}
		};

		private readonly object _requestLock = new object();

		private readonly Callback<TCallbackArgs> _callback;
		private readonly Callback<TCallbackArgs>.DispatchDelegate _callbackDelegate;

		private readonly Func<TCallbackArgs, TKey?> _callbackKeySelector;
		private readonly Func<TCallbackArgs, int> _callbackImageHandleSelector;

		private readonly Dictionary<TKey, PendingRequest> _pendingRequests = new Dictionary<TKey, PendingRequest>();

		private readonly SynchronizationContext _mainContext;

		private bool _isDisposed;

		public SteamAsyncImageHandleDispatcher(
			Func<TCallbackArgs, TKey?> callbackKeySelector,
			Func<TCallbackArgs, int> callbackImageHandleSelector )
			: this(
				SynchronizationContext.Current,
				callbackKeySelector,
				callbackImageHandleSelector
			)
		{
		}

		public SteamAsyncImageHandleDispatcher(
			SynchronizationContext? mainContext,
			Func<TCallbackArgs, TKey?> callbackKeySelector,
			Func<TCallbackArgs, int> callbackImageHandleSelector )
		{
			_mainContext = mainContext
				?? throw new InvalidOperationException(
					"SteamAsyncImageHandleDispatcher must be created with a valid Steam/main SynchronizationContext."
				);

			_callbackKeySelector = callbackKeySelector
				?? throw new ArgumentNullException( nameof( callbackKeySelector ) );

			_callbackImageHandleSelector = callbackImageHandleSelector
				?? throw new ArgumentNullException( nameof( callbackImageHandleSelector ) );

			_callbackDelegate = OnCallback;
			_callback = Callback<TCallbackArgs>.Create( _callbackDelegate );
		}

		/// <summary>
		/// Invokes a Steam image-handle function. If it returns a ready handle,
		/// the task completes immediately. If the handle is pending, the task waits
		/// for the matching Steam callback.
		/// </summary>
		/// <param name="key">The request key used to match the later callback.</param>
		/// <param name="steamCall">The Steam function that returns an int image handle.</param>
		/// <param name="isReadyHandle">
		/// Returns true if the immediate handle should complete the task.
		/// Usually: handle &gt; 0.
		/// </param>
		/// <param name="isTerminalHandle">
		/// Returns true if the immediate handle means no callback should be expected.
		/// Example: avatars may use 0 as "no avatar set."
		/// </param>
		/// <param name="ct">Cancellation token.</param>
		public Task<int> Invoke(
			TKey key,
			Func<int> steamCall,
			Func<int, bool>? isReadyHandle = null,
			Func<int, bool>? isTerminalHandle = null,
			CancellationToken ct = default )
		{
			if ( steamCall == null ) {
				throw new ArgumentNullException( nameof( steamCall ) );
			}

			ThrowIfDisposed();

			isReadyHandle ??= static handle => handle > 0;
			isTerminalHandle ??= static _ => false;

			var completion = new TaskCompletionSource<int>(
				TaskCreationOptions.RunContinuationsAsynchronously
			);

			CancellationTokenRegistration cancellationRegistration = ct.Register( () => {
				Cancel( key, ct );
			} );

			var pending = new PendingRequest(
				key,
				completion,
				cancellationRegistration
			);

			lock ( _requestLock ) {
				ThrowIfDisposed();

				if ( _pendingRequests.TryGetValue( key, out PendingRequest? existing ) ) {
					cancellationRegistration.Dispose();
					return existing.Completion.Task;
				}

				_pendingRequests.Add( key, pending );
			}

			_mainContext.Post( _ => {
				try {
					int handle = steamCall();

					if ( isReadyHandle( handle ) ) {
						Complete( key, handle );
						return;
					}

					if ( isTerminalHandle( handle ) ) {
						Complete( key, handle );
						return;
					}

					// Otherwise, the request remains pending until the Steam callback arrives.
				} catch ( Exception ex ) {
					Fault( key, ex );
				}
			}, null );

			return completion.Task;
		}

		private void OnCallback( TCallbackArgs callbackArgs )
		{
			TKey? key = _callbackKeySelector( callbackArgs );

			if ( key == null ) {
				return;
			}

			int imageHandle = _callbackImageHandleSelector( callbackArgs );

			Complete( key, imageHandle );
		}

		private void Complete( TKey key, int imageHandle )
		{
			PendingRequest? request;

			lock ( _requestLock ) {
				if ( !_pendingRequests.TryGetValue( key, out request ) ) {
					return;
				}

				_pendingRequests.Remove( key );
			}

			if ( !request.TryComplete() ) {
				return;
			}

			request.CancellationRegistration.Dispose();
			request.Completion.TrySetResult( imageHandle );
		}

		private void Fault( TKey key, Exception exception )
		{
			PendingRequest? request;

			lock ( _requestLock ) {
				if ( !_pendingRequests.TryGetValue( key, out request ) ) {
					return;
				}

				_pendingRequests.Remove( key );
			}

			if ( !request.TryComplete() ) {
				return;
			}

			request.CancellationRegistration.Dispose();
			request.Completion.TrySetException( exception );
		}

		private void Cancel( TKey key, CancellationToken ct )
		{
			PendingRequest? request;

			lock ( _requestLock ) {
				if ( !_pendingRequests.TryGetValue( key, out request ) ) {
					return;
				}

				_pendingRequests.Remove( key );
			}

			if ( !request.TryComplete() ) {
				return;
			}

			request.CancellationRegistration.Dispose();
			request.Completion.TrySetCanceled( ct );
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			List<PendingRequest> pending;

			lock ( _requestLock ) {
				if ( _isDisposed ) {
					return;
				}

				_isDisposed = true;

				pending = new List<PendingRequest>( _pendingRequests.Values );
				_pendingRequests.Clear();
			}

			foreach ( PendingRequest request in pending ) {
				if ( request.TryComplete() ) {
					request.CancellationRegistration.Dispose();
					request.Completion.TrySetCanceled();
				}
			}

			_callback.Dispose();

			GC.SuppressFinalize( this );
		}

		private void ThrowIfDisposed()
		{
			if ( _isDisposed ) {
				throw new ObjectDisposedException( nameof( SteamAsyncImageHandleDispatcher<TCallbackArgs, TKey> ) );
			}
		}
	};
};
