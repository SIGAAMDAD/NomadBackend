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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.Core.Memory.Buffers;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Util;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamCloudStorageService

	===================================================================================
	*/
	/// <summary>
	/// Steam Remote Storage backed cloud storage service.
	/// </summary>

	internal sealed class SteamCloudStorageService : ICloudStorageService
	{
		private static bool IsEnabled => SteamRemoteStorage.IsCloudEnabledForApp() && SteamRemoteStorage.IsCloudEnabledForAccount();

		public bool SupportsCloudStorage => true;
		public bool IsCloudStorageEnabled => IsEnabled;

		private readonly ConcurrentDictionary<string, CloudStorageFileInfo> _cloudFiles = new ConcurrentDictionary<string, CloudStorageFileInfo>();

		private readonly ILoggerCategory _category;
		private readonly Callback<RemoteStorageLocalFileChange_t> _fileChangeCallback;

		private bool _isDisposed = false;

		/*
		===============
		SteamCloudStorageService
		===============
		*/
		/// <summary>
		/// Creates a new SteamCloudStorageService instance.
		/// </summary>
		/// <param name="logger">The logger service to use for logging.</param>
		/// <param name="fileSystem">The framework file system. Kept in the constructor for service factory compatibility.</param>
		public SteamCloudStorageService( ILoggerService logger, IFileSystem fileSystem )
		{
			ArgumentGuard.ThrowIfNull( logger );
			ArgumentGuard.ThrowIfNull( fileSystem );

			_category = logger.CreateCategory( nameof( SteamCloudStorageService ), LogLevel.Info, true );
			_fileChangeCallback = Callback<RemoteStorageLocalFileChange_t>.Create( OnFileChange );

			RefreshCloudFileCache();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Disposes Steam callback registrations and logging resources.
		/// </summary>
		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			_fileChangeCallback.Dispose();
			_category.Dispose();

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		GetQuotaAsync
		===============
		*/
		/// <inheritdoc />
		public Task<CloudStorageQuota?> GetQuotaAsync( CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();

			if ( !IsEnabled || !SteamRemoteStorage.GetQuota( out ulong totalBytes, out ulong availableBytes ) ) {
				return Task.FromResult<CloudStorageQuota?>( null );
			}

			return Task.FromResult<CloudStorageQuota?>(
				new CloudStorageQuota( ClampUInt64ToInt64( totalBytes ), ClampUInt64ToInt64( availableBytes ) )
			);
		}

		/*
		===============
		ListFilesAsync
		===============
		*/
		/// <inheritdoc />
		public Task<IReadOnlyList<CloudStorageFileInfo>> ListFilesAsync( CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();

			RefreshCloudFileCache();

			IReadOnlyList<CloudStorageFileInfo> files = _cloudFiles.Values
				.OrderBy( file => file.Path, StringComparer.Ordinal )
				.ToArray();

			return Task.FromResult( files );
		}

		/*
		===============
		GetFileInfoAsync
		===============
		*/
		/// <inheritdoc />
		public Task<CloudStorageFileInfo?> GetFileInfoAsync( string path, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();

			string fileName = NormalizeCloudPath( path );
			if ( !SteamRemoteStorage.FileExists( fileName ) ) {
				_cloudFiles.TryRemove( fileName, out _ );
				return Task.FromResult<CloudStorageFileInfo?>( null );
			}

			if ( _cloudFiles.TryGetValue( fileName, out CloudStorageFileInfo info ) ) {
				return Task.FromResult<CloudStorageFileInfo?>( info );
			}

			info = GetSteamFileInfo( fileName );
			_cloudFiles[fileName] = info;
			return Task.FromResult<CloudStorageFileInfo?>( info );
		}

		/*
		===============
		FileExistsAsync
		===============
		*/
		/// <inheritdoc />
		public Task<bool> FileExistsAsync( string path, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();

			string fileName = NormalizeCloudPath( path );
			bool exists = IsEnabled && SteamRemoteStorage.FileExists( fileName );
			if ( !exists ) {
				_cloudFiles.TryRemove( fileName, out _ );
			}

			return Task.FromResult( exists );
		}

		/*
		===============
		OpenReadAsync
		===============
		*/
		/// <inheritdoc />
		public async Task<IFileReadStream> OpenReadAsync( string path, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();
			EnsureEnabled();

			string fileName = NormalizeCloudPath( path );
			if ( !SteamRemoteStorage.FileExists( fileName ) ) {
				throw new FileNotFoundException( $"Steam cloud file '{fileName}' does not exist.", fileName );
			}

			int fileSize = SteamRemoteStorage.GetFileSize( fileName );
			if ( fileSize < 0 ) {
				throw new IOException( $"Steam returned an invalid size for cloud file '{fileName}'." );
			}

			byte[] data = new byte[fileSize];
			if ( fileSize > 0 ) {
				SteamAPICall_t readCall = SteamRemoteStorage.FileReadAsync( fileName, 0, (uint)fileSize );
				RemoteStorageFileReadAsyncComplete_t result = await AwaitSteamCallAsync<RemoteStorageFileReadAsyncComplete_t>(
					readCall,
					nameof( SteamRemoteStorage.FileReadAsync ),
					ct
				).ConfigureAwait( false );

				ThrowIfOperationFailed( result.m_eResult, $"read cloud file '{fileName}'" );

				if ( !SteamRemoteStorage.FileReadAsyncComplete( result.m_hFileReadAsync, data, result.m_cubRead ) ) {
					throw new IOException( $"Steam failed to complete async read for cloud file '{fileName}'." );
				}

				if ( result.m_cubRead < data.Length ) {
					Array.Resize( ref data, (int)result.m_cubRead );
				}
			}

			_cloudFiles[fileName] = GetSteamFileInfo( fileName );
			return new SteamCloudReadStream( fileName, data );
		}

		/*
		===============
		WriteAsync
		===============
		*/
		/// <inheritdoc />
		public async Task WriteAsync( string path, IBufferHandle data, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ArgumentGuard.ThrowIfNull( data );
			ct.ThrowIfCancellationRequested();
			EnsureEnabled();

			string fileName = NormalizeCloudPath( path );
			byte[] buffer = data.ToArray();

			SteamAPICall_t writeCall = SteamRemoteStorage.FileWriteAsync( fileName, buffer, (uint)buffer.Length );
			RemoteStorageFileWriteAsyncComplete_t result = await AwaitSteamCallAsync<RemoteStorageFileWriteAsyncComplete_t>(
				writeCall,
				nameof( SteamRemoteStorage.FileWriteAsync ),
				ct
			).ConfigureAwait( false );

			ThrowIfOperationFailed( result.m_eResult, $"write cloud file '{fileName}'" );

			_cloudFiles[fileName] = GetSteamFileInfo( fileName );
		}

		/*
		===============
		DeleteAsync
		===============
		*/
		/// <inheritdoc />
		public Task<bool> DeleteAsync( string path, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();
			EnsureEnabled();

			string fileName = NormalizeCloudPath( path );
			bool deleted = SteamRemoteStorage.FileDelete( fileName );
			if ( deleted ) {
				_cloudFiles.TryRemove( fileName, out _ );
			}

			return Task.FromResult( deleted );
		}

		/*
		===============
		SyncAsync
		===============
		*/
		/// <inheritdoc />
		public Task SyncAsync( CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();

			RefreshCloudFileCache();
			return Task.CompletedTask;
		}

		/*
		===============
		OnFileChange
		===============
		*/
		/// <summary>
		/// Refreshes the metadata cache when Steam reports local remote-storage changes.
		/// </summary>
		/// <param name="callback"></param>
		private void OnFileChange( RemoteStorageLocalFileChange_t callback )
		{
			_ = callback;

			int changeCount = SteamRemoteStorage.GetLocalFileChangeCount();
			for ( int i = 0; i < changeCount; i++ ) {
				string fileName = SteamRemoteStorage.GetLocalFileChange(
					i,
					out ERemoteStorageLocalFileChange change,
					out ERemoteStorageFilePathType pathType
				);

				if ( pathType != ERemoteStorageFilePathType.k_ERemoteStorageFilePathType_APIFilename || string.IsNullOrWhiteSpace( fileName ) ) {
					continue;
				}

				if ( change == ERemoteStorageLocalFileChange.k_ERemoteStorageLocalFileChange_FileDeleted ) {
					_cloudFiles.TryRemove( fileName, out _ );
					continue;
				}

				if ( change == ERemoteStorageLocalFileChange.k_ERemoteStorageLocalFileChange_FileUpdated && SteamRemoteStorage.FileExists( fileName ) ) {
					_cloudFiles[fileName] = GetSteamFileInfo( fileName );
				}
			}

			RefreshCloudFileCache();
		}

		/*
		===============
		RefreshCloudFileCache
		===============
		*/
		/// <summary>
		/// Rebuilds cached Steam Remote Storage metadata.
		/// </summary>
		private void RefreshCloudFileCache()
		{
			_cloudFiles.Clear();

			if ( !IsEnabled ) {
				_category.PrintWarning( "Steam cloud storage is not enabled for this application or account." );
				return;
			}

			int fileCount = SteamRemoteStorage.GetFileCount();
			for ( int i = 0; i < fileCount; i++ ) {
				string fileName = SteamRemoteStorage.GetFileNameAndSize( i, out _ );
				if ( string.IsNullOrWhiteSpace( fileName ) ) {
					continue;
				}

				_cloudFiles[fileName] = GetSteamFileInfo( fileName );
				_category.PrintLine( $"SteamCloudStorage: found cloud file '{fileName}'." );
			}
		}

		/*
		===============
		GetSteamFileInfo
		===============
		*/
		/// <summary>
		/// Gets a provider-neutral metadata snapshot for a Steam cloud file.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private static CloudStorageFileInfo GetSteamFileInfo( string fileName )
		{
			int fileSize = SteamRemoteStorage.GetFileSize( fileName );
			long timestamp = SteamRemoteStorage.GetFileTimestamp( fileName );
			DateTimeOffset? lastModified = timestamp > 0 ? DateTimeOffset.FromUnixTimeSeconds( timestamp ) : null;

			return new CloudStorageFileInfo(
				fileName,
				fileSize,
				lastModified,
				SteamRemoteStorage.FilePersisted( fileName )
			);
		}

		/*
		===============
		AwaitSteamCallAsync
		===============
		*/
		/// <summary>
		/// Converts a Steam CallResult into a task.
		/// </summary>
		/// <typeparam name="TCallback"></typeparam>
		/// <param name="call"></param>
		/// <param name="operationName"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private static Task<TCallback> AwaitSteamCallAsync<TCallback>( SteamAPICall_t call, string operationName, CancellationToken ct )
			where TCallback : struct
		{
			ct.ThrowIfCancellationRequested();
			SteamApiGuard.ThrowIfInvalidCall( call, operationName );

			TaskCompletionSource<TCallback> tcs = new TaskCompletionSource<TCallback>(
				TaskCreationOptions.RunContinuationsAsynchronously
			);

			CallResult<TCallback>? callResult = null;
			CancellationTokenRegistration registration = default;

			CallResult<TCallback>.APIDispatchDelegate callback = ( result, ioFailure ) => {
				registration.Dispose();
				callResult?.Dispose();

				if ( ioFailure ) {
					tcs.TrySetException( new IOException( $"{operationName} failed with Steam IO failure." ) );
					return;
				}

				tcs.TrySetResult( result );
			};

			callResult = CallResult<TCallback>.Create( callback );
			if ( ct.CanBeCanceled ) {
				registration = ct.Register( () => {
					callResult.Cancel();
					callResult.Dispose();
					tcs.TrySetCanceled( ct );
				} );
			}

			callResult.Set( call, callback );
			return tcs.Task;
		}

		/*
		===============
		ThrowIfOperationFailed
		===============
		*/
		/// <summary>
		/// Converts a Steam operation result into a framework exception.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="operationDescription"></param>
		/// <exception cref="IOException"></exception>
		private static void ThrowIfOperationFailed( EResult result, string operationDescription )
		{
			if ( result.IsSuccess() ) {
				return;
			}

			throw new IOException( $"Steam failed to {operationDescription}: {result.ToDiagnosticString()}" );
		}

		/*
		===============
		EnsureEnabled
		===============
		*/
		/// <summary>
		/// Ensures the active Steam user and app can use remote storage.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		private static void EnsureEnabled()
		{
			if ( !IsEnabled ) {
				throw new InvalidOperationException( "Steam cloud storage is not enabled for this application or account." );
			}
		}

		/*
		===============
		ThrowIfDisposed
		===============
		*/
		/// <summary>
		/// Guards public methods after disposal.
		/// </summary>
		private void ThrowIfDisposed()
		{
			if ( _isDisposed ) {
				throw new ObjectDisposedException( nameof( SteamCloudStorageService ) );
			}
		}

		/*
		===============
		NormalizeCloudPath
		===============
		*/
		/// <summary>
		/// Normalizes a portable provider-relative cloud path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string NormalizeCloudPath( string path )
		{
			if ( path == null ) {
				throw new ArgumentNullException( nameof( path ) );
			}

			string normalized = path.Trim().Replace( '\\', '/' );
			if ( normalized.Length == 0 ) {
				throw new ArgumentException( "Cloud storage path cannot be empty.", nameof( path ) );
			}

			if ( Path.IsPathRooted( normalized ) || normalized.StartsWith( "/", StringComparison.Ordinal ) ) {
				throw new ArgumentException( "Cloud storage paths must be provider-relative.", nameof( path ) );
			}

			string[] segments = normalized.Split( '/' );
			for ( int i = 0; i < segments.Length; i++ ) {
				string segment = segments[i];
				if ( segment.Length == 0 || segment == "." || segment == ".." ) {
					throw new ArgumentException( "Cloud storage paths cannot contain empty, current, or parent segments.", nameof( path ) );
				}
			}

			return normalized;
		}

		/*
		===============
		ClampUInt64ToInt64
		===============
		*/
		/// <summary>
		/// Clamps Steam unsigned quota values to the framework's signed size type.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static long ClampUInt64ToInt64( ulong value )
			=> value > long.MaxValue ? long.MaxValue : (long)value;
	};
};
