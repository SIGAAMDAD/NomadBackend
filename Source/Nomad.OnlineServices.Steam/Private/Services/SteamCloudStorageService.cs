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
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamCloudStorageService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamCloudStorageService : ICloudStorageService
	{
		private static bool IsEnabled => SteamRemoteStorage.IsCloudEnabledForApp() && SteamRemoteStorage.IsCloudEnabledForAccount();

		public bool SupportsCloudStorage => true;

		public struct CloudFile
		{
			public int Size { get; set; }
			public DateTime CloudAccessTime { get; set; }
			public DateTime LocalAccessTime { get; set; }
		};

		private readonly ConcurrentDictionary<string, CloudFile> _cloudFiles = new ConcurrentDictionary<string, CloudFile>();

		private readonly IFileSystem _fileSystem;

		private readonly ILoggerCategory _category;

		private readonly CallResult<RemoteStorageLocalFileChange_t> _fileChangeResult;
		private readonly CallResult<RemoteStorageFileWriteAsyncComplete_t> _fileWriteAsyncComplete;
		private readonly CallResult<RemoteStorageFileReadAsyncComplete_t> _fileReadAsyncComplete;

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
		/// <param name="fileSystem"></param>
		public SteamCloudStorageService( ILoggerService logger, IFileSystem fileSystem )
		{
			ArgumentGuard.ThrowIfNull( logger );
			ArgumentGuard.ThrowIfNull( fileSystem );

			_category = logger.CreateCategory( nameof( SteamCloudStorageService ), LogLevel.Info, true );
			_fileSystem = fileSystem;

			_fileChangeResult = CallResult<RemoteStorageLocalFileChange_t>.Create( OnFileChange );

			InitializeCloudFileCache();
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
				_fileChangeResult?.Dispose();
				_category?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		OnFileChange
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="pCallback"></param>
		/// <param name="bIOFailure"></param>
		private void OnFileChange( RemoteStorageLocalFileChange_t pCallback, bool bIOFailure )
		{
		}

		/*
		===============
		InitializeCloudFileCache
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void InitializeCloudFileCache()
		{
			int fileCount = SteamRemoteStorage.GetFileCount();

			for ( int i = 0; i < fileCount; i++ ) {
				string fileName = SteamRemoteStorage.GetFileNameAndSize( i, out int fileSize );

				DateTime cloudAccessTimestamp = DateTime.FromFileTimeUtc( SteamRemoteStorage.GetFileTimestamp( fileName ) );
				FileInfo info = new FileInfo( fileName );

				_cloudFiles.TryAdd( fileName, new CloudFile { CloudAccessTime = cloudAccessTimestamp, LocalAccessTime = info.LastAccessTimeUtc, Size = fileSize } );
				_category.PrintLine( $"SteamCloudStorage.InitializeCloudFileCache: found file '{fileName}'" );
			}
		}

		public async Task<CloudFile[]> ListFilesAsync( CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return _cloudFiles.Values.ToArray();
		}

		/*
		===============
		FileExists
		===============
		*/
		/// <summary>
		/// Checks if a file exists in cloud storage.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<bool> FileExists( string fileName, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return _cloudFiles.ContainsKey( fileName );
		}

		/*
		===============
		ResolveConflict
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public async ValueTask ResolveConflict( string fileName )
		{
			if ( !_cloudFiles.TryGetValue( fileName, out var cloudFile ) ) {
				_category.PrintError( $"No such cloud file named '{fileName}'!" );
				return;
			}

			IBufferHandle localFile = await _fileSystem.LoadFileAsync( fileName );


		}

		/*
		===============
		Synchronize
		===============
		*/
		/// <summary>
		/// Synchronizes local files with cloud storage.
		/// </summary>
		/// <returns></returns>
		public async ValueTask Synchronize()
		{
			if ( !IsEnabled ) {
				_category.PrintWarning( "Cloud storage is not enabled for this application." );
				return;
			}

			int fileCount = SteamRemoteStorage.GetFileCount();
			for ( int i = 0; i < fileCount; i++ ) {
				string name = SteamRemoteStorage.GetFileNameAndSize( i, out int fileSize );
				if ( _cloudFiles.TryGetValue( name, out CloudFile cloudFile ) ) {
					// exists, resolve the conflict
				} else {
					// doesn't exist, download a local copy
				}
			}
		}

		/*
		===============
		WriteFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public async ValueTask WriteFile( string fileName )
		{
			using var buffer = await _fileSystem.LoadFileAsync( fileName );
			SteamRemoteStorage.FileWriteAsync( fileName, buffer.ToArray(), (uint)buffer.Length );

			await Synchronize();
		}

		public ValueTask<bool> FileExists( string fileName )
		{
			throw new NotImplementedException();
		}

		public ValueTask ResolveConflict( string fileName, IBufferHandle localData, IBufferHandle cloudData )
		{
			throw new NotImplementedException();
		}

		public Task<bool> FileExistsAsync( string path, CancellationToken ct = default )
		{
			throw new NotImplementedException();
		}

		public Task<IFileReadStream> OpenReadAsync( string path, CancellationToken ct = default )
		{
			throw new NotImplementedException();
		}

		public Task WriteAsync( string path, IBufferHandle data, CancellationToken ct = default )
		{
			throw new NotImplementedException();
		}

		public Task<bool> DeleteAsync( string path, CancellationToken ct = default )
		{
			throw new NotImplementedException();
		}

		public Task SyncAsync( CancellationToken ct = default )
		{
			throw new NotImplementedException();
		}
	};
};
