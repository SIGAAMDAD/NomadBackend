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
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.FileSystem.Private.FileStreams;
using Nomad.FileSystem.Private.MemoryStream;

namespace Nomad.FileSystem.Private.Services
{
	/*
	===================================================================================
	
	StreamFactory
	

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class StreamFactory : IDisposable
	{
		private readonly RecursiveFileSearcher _searchHelper;

		private bool _isDisposed = false;

		/*
		===============
		StreamFactory
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public StreamFactory( RecursiveFileSearcher searchHelper )
		{
			ArgumentGuard.ThrowIfNull( searchHelper );
			_searchHelper = searchHelper;
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
				GC.SuppressFinalize( this );
				_isDisposed = true;
			}
		}

		/*
		===============
		OpenRead
		===============
		*/
		/// <summary>
		/// Opens a memory-backed read stream after resolving the source file path through the search helper.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IMemoryFileReadStream? OpenRead( MemoryFileReadConfig config )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNull( config );
			ArgumentGuard.ThrowIfNullOrEmpty( config.FilePath, nameof( config.FilePath ) );

			string? fullPath = _searchHelper.FindFile( config.FilePath );
			if ( fullPath == null ) {
				return null;
			}

			return new MemoryFileReadStream( config with { FilePath = fullPath } );
		}

		/*
		===============
		OpenReadAsync
		===============
		*/
		/// <summary>
		/// Opens a memory-backed read stream asynchronously after resolving the source file path.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public ValueTask<IMemoryFileReadStream?> OpenReadAsync( MemoryFileReadConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<IMemoryFileReadStream?>( OpenRead( config ) );
		}

		/*
		===============
		OpenRead
		===============
		*/
		/// <summary>
		/// Opens a pure memory read stream.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IMemoryReadStream OpenRead( MemoryReadConfig config )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNull( config );
			return new MemoryReadStream( config );
		}

		/*
		===============
		OpenReadAsync
		===============
		*/
		/// <summary>
		/// Opens a pure memory read stream asynchronously.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public ValueTask<IMemoryReadStream> OpenReadAsync( MemoryReadConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<IMemoryReadStream>( OpenRead( config ) );
		}

		/*
		===============
		OpenWrite
		===============
		*/
		/// <summary>
		/// Opens a memory-backed write stream that flushes to a file on disposal.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IMemoryFileWriteStream OpenWrite( MemoryFileWriteConfig config )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNull( config );
			ArgumentGuard.ThrowIfNullOrEmpty( config.FilePath, nameof( config.FilePath ) );
			return new MemoryFileWriteStream( config );
		}

		/*
		===============
		OpenWriteAsync
		===============
		*/
		/// <summary>
		/// Opens a memory-backed write stream asynchronously.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public ValueTask<IMemoryFileWriteStream> OpenWriteAsync( MemoryFileWriteConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<IMemoryFileWriteStream>( OpenWrite( config ) );
		}

		/*
		===============
		OpenWrite
		===============
		*/
		/// <summary>
		/// Opens a pure memory write stream.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IMemoryWriteStream OpenWrite( MemoryWriteConfig config )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNull( config );
			return new MemoryWriteStream( config );
		}

		/*
		===============
		OpenWriteAsync
		===============
		*/
		/// <summary>
		/// Opens a pure memory write stream asynchronously.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public ValueTask<IMemoryWriteStream> OpenWriteAsync( MemoryWriteConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<IMemoryWriteStream>( OpenWrite( config ) );
		}

		/*
		===============
		OpenRead
		===============
		*/
		/// <summary>
		/// Opens a file-backed read stream after resolving the path through the search helper.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IFileReadStream? OpenRead( FileReadConfig config )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNull( config );
			ArgumentGuard.ThrowIfNullOrEmpty( config.FilePath, nameof( config.FilePath ) );

			string? fullPath = _searchHelper.FindFile( config.FilePath );
			if ( fullPath == null ) {
				return null;
			}

			return new FileReadStream( config with { FilePath = fullPath } );
		}

		/*
		===============
		OpenReadAsync
		===============
		*/
		/// <summary>
		/// Opens a file-backed read stream asynchronously.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public ValueTask<IFileReadStream?> OpenReadAsync( FileReadConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<IFileReadStream?>( OpenRead( config ) );
		}

		/*
		===============
		OpenWrite
		===============
		*/
		/// <summary>
		/// Opens a file-backed write stream.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		public IFileWriteStream OpenWrite( FileWriteConfig config )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNull( config );
			ArgumentGuard.ThrowIfNullOrEmpty( config.FilePath, nameof( config.FilePath ) );
			return new FileWriteStream( config );
		}

		/*
		===============
		OpenWriteAsync
		===============
		*/
		/// <summary>
		/// Opens a file-backed write stream asynchronously.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public ValueTask<IFileWriteStream> OpenWriteAsync( FileWriteConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<IFileWriteStream>( OpenWrite( config ) );
		}
	};
};
