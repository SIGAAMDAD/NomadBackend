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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Streams;

namespace Nomad.FileSystem.Private.FileStreams
{
	/*
	===================================================================================

	FileStreamBase

	===================================================================================
	*/
	/// <summary>
	/// Base implementation of a file stream.
	/// </summary>

	internal abstract class FileStreamBase : BaseStream, IFileStream
	{
		/// <summary>
		/// Indicates whether the stream supports seeking.
		/// </summary>
		public override bool CanSeek => !isDisposed ? fileStream.CanSeek : throw new ObjectDisposedException( nameof( FileStreamBase ) );

		/// <summary>
		/// Gets or sets the current position within the file stream.
		/// </summary>
		public override long Position {
			get => !isDisposed ? fileStream.Position : throw new ObjectDisposedException( nameof( FileStreamBase ) );
			set {
				StateGuard.ThrowIfDisposed( isDisposed, this );
				fileStream.Seek( value, SeekOrigin.Begin );
			}
		}

		/// <summary>
		/// Gets the length of the file stream in bytes.
		/// </summary>
		public override long Length {
			get => !isDisposed ? fileStream.Length : throw new ObjectDisposedException( nameof( FileStreamBase ) );
			set {
				StateGuard.ThrowIfDisposed( isDisposed, this );
				fileStream.SetLength( value );
			}
		}

		/// <summary>
		/// Gets the file path associated with the file stream.
		/// </summary>
		public string FilePath => !isDisposed ? fileStream.Name : throw new ObjectDisposedException( nameof( FileStreamBase ) );

		/// <summary>
		/// Indicates whether the file stream is currently open.
		/// </summary>
		public bool IsOpen => fileStream != null;

		/// <summary>
		/// The underlying file stream.
		/// </summary>
		protected FileStream fileStream;

		/*
		===============
		FileStreamBase
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="fileMode"></param>
		/// <param name="fileAccess"></param>
		public FileStreamBase( string filepath, FileMode fileMode, FileAccess fileAccess )
		{
			if ( string.IsNullOrEmpty( filepath ) || string.IsNullOrWhiteSpace( filepath ) ) {
				throw new ArgumentException( nameof( filepath ) );
			}
			fileStream = new FileStream( filepath, fileMode, fileAccess, FileShare.ReadWrite );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Releases all resources used by the file stream.
		/// </summary>
		public sealed override void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( isDisposed ) {
				return;
			}
			if ( disposing ) {
				fileStream?.Dispose();
				fileStream = null;
			}
			isDisposed = true;
		}

		/*
		===============
		DisposeAsync
		===============
		*/
		/// <summary>
		/// Asynchronously releases all resources used by the file stream.
		/// </summary>
		public sealed override async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore().ConfigureAwait( false );
			Dispose( false );
			GC.SuppressFinalize( this );
		}

		/*
		===============
		DisposeAsyncCore
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		protected virtual async ValueTask DisposeAsyncCore()
		{
			if ( isDisposed ) {
				return;
			}
			if ( fileStream is IAsyncDisposable asyncDisposable ) {
				await asyncDisposable.DisposeAsync().ConfigureAwait( false );
			} else {
				fileStream?.Dispose();
			}
		}

		/*
		===============
		Close
		===============
		*/
		/// <summary>
		/// Closes the file stream.
		/// </summary>
		public void Close()
		{
			Dispose();
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the file stream's buffer to the underlying file.
		/// </summary>
		public override void Flush()
		{
			ArgumentGuard.ThrowIfNull( fileStream );
			fileStream.Flush();
		}

		/*
		===============
		FlushAsync
		===============
		*/
		/// <summary>
		/// Asynchronously flushes the file stream's buffer to the underlying file.
		/// </summary>
		/// <param name="ct"></param>
		public override async ValueTask FlushAsync( CancellationToken ct = default )
		{
			ArgumentGuard.ThrowIfNull( fileStream );
			ct.ThrowIfCancellationRequested();
			await fileStream.FlushAsync( ct );
		}

		/*
		===============
		Seek
		===============
		*/
		/// <summary>
		/// Sets the file stream's position to the specified offset based on the given origin.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns>The new position in the file stream.</returns>
		public override long Seek( long offset, SeekOrigin origin )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			return fileStream.Seek( offset, origin );
		}

		/*
		===============
		SetLength
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="length"></param>
		public override void SetLength( long length )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			fileStream.SetLength( length );
		}
	};
};
