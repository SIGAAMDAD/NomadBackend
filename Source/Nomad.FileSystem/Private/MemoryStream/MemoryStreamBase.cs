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
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Memory.Buffers;

namespace Nomad.FileSystem.Private.MemoryStream
{
	/*
	===================================================================================

	MemoryStreamBase

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal abstract class MemoryStreamBase : BaseStream, IMemoryStream
	{
		public override long Length {
			get => !isDisposed ? length : throw new ObjectDisposedException( nameof( MemoryStreamBase ) );
			set => SetLength( value );
		}
		protected long length = 0;

		public override long Position {
			get => !isDisposed ? position : throw new ObjectDisposedException( nameof( MemoryStreamBase ) );
			set => Seek( value, SeekOrigin.Begin );
		}
		protected long position = 0;

		/// <summary>
		/// The handle to the underlying buffer.
		/// </summary>
		public IBufferHandle? Buffer => !isDisposed ? buffer : throw new ObjectDisposedException( nameof( MemoryStreamBase ) );
		protected IBufferHandle? buffer;

		/// <summary>
		/// Always <b>true</b>.
		/// </summary>
		public override bool CanSeek => true;

		/// <summary>
		/// Controls how we allocate the memory for the buffer internally.
		/// </summary>
		protected readonly AllocationStrategy strategy;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="strategy"></param>
		public MemoryStreamBase( AllocationStrategy strategy )
		{
			this.strategy = strategy;
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public override void Dispose()
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
				buffer?.Dispose();
				buffer = null;
			}
			isDisposed = true;
		}

		/*
		===============
		DisposeAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public override async ValueTask DisposeAsync()
		{
			await DisposeAsyncCore().ConfigureAwait( false );
			Dispose( false );
			GC.SuppressFinalize( this );
		}

		/*
		===============
		DisposeAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns>The <see cref="ValueTask"/> for the disposal method.</returns>
		protected virtual ValueTask DisposeAsyncCore()
		{
			if ( buffer != null ) {
				if ( buffer is IAsyncDisposable asyncBuffer ) {
					return asyncBuffer.DisposeAsync();
				} else {
					buffer.Dispose();
				}
				buffer = null;
			}
			return default;
		}

		/*
		===============
		Seek
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns></returns>
		/// <exception cref="IOException"></exception>
		public override long Seek( long offset, SeekOrigin origin )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			switch ( origin ) {
				case SeekOrigin.Begin:
					if ( offset > length ) {
						throw new IOException( "seek (begin) offset is larger than the stream's size!" );
					}
					position = offset;
					break;
				case SeekOrigin.Current:
					if ( position + offset > length ) {
						throw new IOException( "seek (current) offset + position is larger than the stream's size!" );
					}
					position += offset;
					break;
				case SeekOrigin.End:
					if ( offset > 0 ) {
						throw new IOException( "seek (end) offset is greater than 0!" );
					}
					position = length + offset;
					break;
				default:
					return -1;
			}
			return position;
		}
	};
};
