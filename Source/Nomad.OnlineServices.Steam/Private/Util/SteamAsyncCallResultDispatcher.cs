using System;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Util
{
	internal sealed class SteamAsyncCallResultDispatcher<TSteamCallback, TResult> : IDisposable
		where TSteamCallback : struct
	{
		private sealed class PendingOperation
		{
			public readonly ulong Generation;
			public readonly TaskCompletionSource<TResult> Completion;
			public readonly CancellationTokenRegistration CancellationRegistration;

			private int _completed;

			public PendingOperation(
				ulong generation,
				TaskCompletionSource<TResult> completion,
				CancellationTokenRegistration cancellationRegistration )
			{
				Generation = generation;
				Completion = completion;
				CancellationRegistration = cancellationRegistration;
			}

			public bool TryMarkCompleted()
			{
				return Interlocked.Exchange( ref _completed, 1 ) == 0;
			}
		};

		private readonly string _operationName;
		private readonly ISteamApiThreadDispatcher _steamThread;
		private readonly Func<TSteamCallback, TResult> _resultFactory;

		private readonly CallResult<TSteamCallback> _callResult;
		private readonly CallResult<TSteamCallback>.APIDispatchDelegate _steamCallbackDelegate;

		private readonly SemaphoreSlim _singleFlight = new SemaphoreSlim( 1, 1 );
		private readonly AutoResetEvent _callbackSignal = new AutoResetEvent( false );
		private readonly Thread _waiterThread;

		private readonly object _syncRoot = new object();

		private PendingOperation? _pending;
		private TSteamCallback _receivedCallback;
		private bool _receivedIoFailure;
		private bool _hasReceivedCallback;

		private bool _disposed;
		private ulong _generation;

		public SteamAsyncCallResultDispatcher(
			string operationName,
			ISteamApiThreadDispatcher steamThread,
			Func<TSteamCallback, TResult> resultFactory )
		{
			_operationName = operationName ?? throw new ArgumentNullException( nameof( operationName ) );
			_steamThread = steamThread ?? throw new ArgumentNullException( nameof( steamThread ) );
			_resultFactory = resultFactory ?? throw new ArgumentNullException( nameof( resultFactory ) );

			_steamCallbackDelegate = OnSteamCallResult;
			_callResult = CallResult<TSteamCallback>.Create( _steamCallbackDelegate );

			_waiterThread = new Thread( WaiterLoop ) {
				IsBackground = true,
				Name = $"SteamAsyncCallResultDispatcher<{typeof( TSteamCallback ).Name}>"
			};

			_waiterThread.Start();
		}

		public async Task<TResult> ExecuteAsync(
			Func<SteamAPICall_t> beginSteamCall,
			CancellationToken ct = default )
		{
			return await ExecuteAsync(
				beginSteamCall,
				timeout: null,
				ct: ct
			).ConfigureAwait( false );
		}

		public async Task<TResult> ExecuteAsync(
			Func<SteamAPICall_t> beginSteamCall,
			TimeSpan? timeout,
			CancellationToken ct = default )
		{
			if ( beginSteamCall == null ) {
				throw new ArgumentNullException( nameof( beginSteamCall ) );
			}

			ThrowIfDisposed();

			await _singleFlight.WaitAsync( ct ).ConfigureAwait( false );

			bool operationStarted = false;

			try {
				ThrowIfDisposed();

				using ( CancellationTokenSource? timeoutSource = timeout.HasValue ? new( timeout.Value ) : null )
				using ( CancellationTokenSource linkedSource = timeoutSource == null
					? CancellationTokenSource.CreateLinkedTokenSource( ct )
					: CancellationTokenSource.CreateLinkedTokenSource( ct, timeoutSource.Token
				) )
				{
					CancellationToken linkedToken = linkedSource.Token;

					TaskCompletionSource<TResult> completion = new TaskCompletionSource<TResult>(
						TaskCreationOptions.RunContinuationsAsynchronously
					);

					ulong generation;

					lock ( _syncRoot ) {
						generation = ++_generation;

						_hasReceivedCallback = false;
						_receivedCallback = default;
						_receivedIoFailure = false;
					}

					CancellationTokenRegistration cancellationRegistration = linkedToken.Register(
						static state => {
							var tuple = (Tuple<SteamAsyncCallResultDispatcher<TSteamCallback, TResult>, ulong>)state!;

							tuple.Item1.CancelPendingOperation( tuple.Item2 );
						},
						Tuple.Create( this, generation )
					);

					var pending = new PendingOperation(
						generation,
						completion,
						cancellationRegistration
					);

					lock ( _syncRoot ) {
						_pending = pending;
					}

					try {
						await _steamThread.InvokeAsync(
							() => {
								SteamAPICall_t callHandle = beginSteamCall();

								if ( callHandle == SteamAPICall_t.Invalid ) {
									throw new SteamAsyncCallFailedException(
										$"Steam async call returned an invalid handle. Operation: {_operationName}."
									);
								}

								_callResult.Set( callHandle );
							},
							linkedToken
						).ConfigureAwait( false );

						operationStarted = true;
					} catch {
						CompletePendingWithException(
							generation,
							new SteamAsyncCallFailedException(
								$"Failed to start Steam async call. Operation: {_operationName}."
							)
						);

						throw;
					}

					return await completion.Task.ConfigureAwait( false );
				}
			} finally {
				if ( !operationStarted ) {
					_singleFlight.Release();
				}
			}
		}

		private void OnSteamCallResult( TSteamCallback callback, bool ioFailure )
		{
			lock ( _syncRoot ) {
				if ( _pending == null ) {
					return;
				}

				_receivedCallback = callback;
				_receivedIoFailure = ioFailure;
				_hasReceivedCallback = true;
			}

			_callbackSignal.Set();
		}

		private void WaiterLoop()
		{
			while ( true ) {
				_callbackSignal.WaitOne();

				if ( _disposed ) {
					return;
				}

				PendingOperation? pending;
				TSteamCallback callback;
				bool ioFailure;

				lock ( _syncRoot ) {
					if ( !_hasReceivedCallback || _pending == null ) {
						continue;
					}

					pending = _pending;
					callback = _receivedCallback;
					ioFailure = _receivedIoFailure;

					_hasReceivedCallback = false;
					_pending = null;
				}

				if ( !pending.TryMarkCompleted() ) {
					continue;
				}

				try {
					pending.CancellationRegistration.Dispose();

					if ( ioFailure ) {
						pending.Completion.TrySetException(
							new SteamAsyncCallIoFailureException( _operationName )
						);

						continue;
					}

					TResult result = _resultFactory( callback );
					pending.Completion.TrySetResult( result );
				} catch ( Exception exception ) {
					pending.Completion.TrySetException(
						new SteamAsyncCallFailedException(
							$"Failed to convert Steam callback result. Operation: {_operationName}.",
							exception
						)
					);
				} finally {
					_singleFlight.Release();
				}
			}
		}

		private void CancelPendingOperation( ulong generation )
		{
			PendingOperation? pending = null;

			lock ( _syncRoot ) {
				if ( _pending == null || _pending.Generation != generation ) {
					return;
				}

				pending = _pending;
				_pending = null;
				_hasReceivedCallback = false;
			}

			if ( !pending.TryMarkCompleted() ) {
				return;
			}

			_ = _steamThread.InvokeAsync(
				() => _callResult.Cancel(),
				CancellationToken.None
			);

			pending.CancellationRegistration.Dispose();
			pending.Completion.TrySetCanceled();

			_singleFlight.Release();
		}

		private void CompletePendingWithException( ulong generation, Exception exception )
		{
			PendingOperation? pending = null;

			lock ( _syncRoot ) {
				if ( _pending == null || _pending.Generation != generation ) {
					return;
				}

				pending = _pending;
				_pending = null;
				_hasReceivedCallback = false;
			}

			if ( !pending.TryMarkCompleted() ) {
				return;
			}

			pending.CancellationRegistration.Dispose();
			pending.Completion.TrySetException( exception );

			_singleFlight.Release();
		}

		private void ThrowIfDisposed()
		{
			if ( _disposed ) {
				throw new ObjectDisposedException( GetType().Name );
			}
		}

		public void Dispose()
		{
			if ( _disposed ) {
				return;
			}

			_disposed = true;

			lock ( _syncRoot ) {
				if ( _pending != null && _pending.TryMarkCompleted() ) {
					_pending.Completion.TrySetCanceled();
					_pending.CancellationRegistration.Dispose();
					_pending = null;

					_singleFlight.Release();
				}
			}

			_callbackSignal.Set();

			_ = _steamThread.InvokeAsync(
				() => _callResult.Cancel(),
				CancellationToken.None
			);

			_callResult.Dispose();
			_callbackSignal.Dispose();
			_singleFlight.Dispose();
		}
	};
};
