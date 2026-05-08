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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Memory.Buffers;

namespace Nomad.FileSystem.Private.MemoryStream
{
	/*
	===================================================================================

	MemoryWriteStream

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal class MemoryWriteStream : MemoryStreamBase, IMemoryWriteStream
	{
		/// <summary>
		/// The maximum possible length of the buffer. Set to 1 GiB because of integer limitations.
		/// </summary>
		public const int MAX_CAPACITY = 1 * 1024 * 1024 * 1024;

		/// <summary>
		/// The maximum length of a string allowed for stack-based writes before we start writing segments.
		/// </summary>
		public const int STACK_ALLOC_THRESHOLD = 256;

		/// <summary>
		/// The default initial capacity of a <see cref="MemoryWriteStream"/>.
		/// </summary>
		public const int DEFAULT_CAPACITY = 8192;

		/// <summary>
		/// Marks whether the stream can be read from. Always <b>false</b>.
		/// </summary>
		public override bool CanRead => false;

		/// <summary>
		/// Marks whether the stream can be read from. Always <b>true</b>.
		/// </summary>
		public override bool CanWrite => true;

		private readonly bool _fixedSize;

		/*
		===============
		MemoryWriteStream
		===============
		*/
		/// <summary>
		/// Initializes a new instance of the MemoryWriteStream class with the specified initial length and fixed size option.
		/// </summary>
		/// <param name="config"></param>
		public MemoryWriteStream( MemoryWriteConfig config )
			: base( config.Strategy )
		{
			if ( config.InitialCapacity < 0 || config.InitialCapacity > config.MaxCapacity || config.InitialCapacity > MAX_CAPACITY || config.MaxCapacity > MAX_CAPACITY ) {
				throw new ArgumentOutOfRangeException( nameof( config ) );
			}

			buffer = AllocateBuffer( config.InitialCapacity );
			_fixedSize = config.FixedSize;

			length = 0;
			position = 0;
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the stream. This operation is a no-op for memory streams.
		/// </summary>
		public override void Flush()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
		}

		/*
		===============
		FlushAsync
		===============
		*/
		/// <summary>
		/// Asynchronously flushes the stream. This operation is a no-op for memory streams.
		/// </summary>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous flush operation.</returns>
		public override async ValueTask FlushAsync( CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ct.ThrowIfCancellationRequested();
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
			if ( this.length < length ) {
				ResizeBuffer( length );
			}
			if ( position > length ) {
				position = length;
			}
			this.length = length;
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public void Write( byte[] buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( buffer );
			if ( offset < 0 || offset + count > buffer.Length ) {
				throw new ArgumentOutOfRangeException( nameof( offset ) );
			}
			if ( count < 0 || count > buffer.Length ) {
				throw new ArgumentOutOfRangeException( nameof( count ) );
			}
			if ( count == 0 || buffer.Length == 0 ) {
				return; // no-op
			}

			EnsureCapacity( count );
			this.buffer!.CopyFrom( buffer, offset, count, (int)position );
			BumpPosition( count );
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		public void Write( byte[] buffer )
			=> Write( buffer, 0, buffer.Length );

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only span of bytes. This method copies the contents of the span to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public void Write( ReadOnlySpan<byte> buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			if ( offset < 0 || offset + count > buffer.Length ) {
				throw new ArgumentOutOfRangeException( nameof( offset ) );
			}
			RangeGuard.ThrowIfOutOfRange( count, 0, buffer.Length, nameof( count ) );
			if ( count == 0 || buffer.Length == 0 ) {
				return; // no-op
			}

			EnsureCapacity( count );
			this.buffer!.CopyFrom( buffer, offset, count, (int)position );
			BumpPosition( count );
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only span of bytes. This method copies the contents of the span to the current stream.</param>
		public void Write( ReadOnlySpan<byte> buffer )
			=> Write( buffer, 0, buffer.Length );

		/*
		===============
		WriteAsync
		===============
		*/
		/// <summary>
		/// Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public async ValueTask WriteAsync( byte[] buffer, int offset, int count, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( buffer );
			if ( offset < 0 || offset + count > buffer.Length ) {
				throw new ArgumentOutOfRangeException( nameof( offset ) );
			}
			if ( count < 0 || count > buffer.Length ) {
				throw new ArgumentOutOfRangeException( nameof( count ) );
			}
			if ( count == 0 ) {
				return; // no-op
			}

			ct.ThrowIfCancellationRequested();
			Write( buffer, offset, count );
		}

		/*
		===============
		WriteAsync
		===============
		*/
		/// <summary>
		/// Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only memory buffer. This method copies the contents of the buffer to the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public async ValueTask WriteAsync( ReadOnlyMemory<byte> buffer, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			ct.ThrowIfCancellationRequested();
			Write( buffer.Span );
		}

		/*
		===============
		WriteAsync
		===============
		*/
		/// <summary>
		/// Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only memory buffer. This method copies the contents of the buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public async ValueTask WriteAsync( ReadOnlyMemory<byte> buffer, int offset, int count, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			ct.ThrowIfCancellationRequested();
			Write( buffer.Slice( offset, count ).Span );
		}

		/*
		===============
		WriteFromStream
		===============
		*/
		/// <summary>
		/// Reads all bytes from the specified read stream and writes them to the current stream.
		/// </summary>
		/// <param name="stream">The read stream to copy from.</param>
		public void WriteFromStream( IReadStream stream )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( stream );

			byte[] buffer = stream.ReadToEnd();
			Write( buffer, 0, buffer.Length );
		}

		/*
		===============
		WriteFromStreamAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads all bytes from the specified read stream and writes them to the current stream.
		/// </summary>
		/// <param name="stream">The read stream to copy from.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		public async ValueTask WriteFromStreamAsync( IReadStream stream, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( stream );

			ct.ThrowIfCancellationRequested();
			byte[] buffer = await stream.ReadToEndAsync( ct );
			await WriteAsync( buffer, 0, buffer.Length, ct );
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a UTF-8 encoded string to the stream, prefixed with its length as a 7-bit encoded integer.
		/// </summary>
		/// <param name="value">The string to write.</param>
		public void WriteString( string value )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( value );

			if ( value.Length == 0 ) {
				// if the length is 0, just write it and return, otherwise we crash on the conversion.
				Write7BitEncodedInt( 0 );
				return;
			}

			int maxByteCount = Encoding.UTF8.GetMaxByteCount( value.Length );
			int byteCount;

			if ( maxByteCount <= STACK_ALLOC_THRESHOLD ) {
				Span<byte> tempBuffer = stackalloc byte[maxByteCount];

				byteCount = Encoding.UTF8.GetBytes( value, tempBuffer );
				Write7BitEncodedInt( byteCount );
				EnsureCapacity( byteCount );

				buffer!.CopyFrom( tempBuffer.Slice( 0, byteCount ), 0, byteCount, (int)position );
			} else {
				byteCount = Encoding.UTF8.GetByteCount( value );
				Write7BitEncodedInt( byteCount );
				EnsureCapacity( byteCount );

				byteCount = Encoding.UTF8.GetBytes( value.AsSpan(), buffer!.GetSlice( (int)position, byteCount ) );
			}

			BumpPosition( byteCount );
		}

		/*
		===============
		Write7BitEncodedInt
		===============
		*/
		/// <summary>
		/// Writes an integer to the stream using 7-bit encoded format.
		/// </summary>
		/// <param name="value">The integer value to write.</param>
		public void Write7BitEncodedInt( int value )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			uint uValue = (uint)value;
			while ( uValue >= 0x80 ) {
				Write( (byte)(uValue | 0x80), sizeof( byte ) );
				uValue >>= 7;
			}
			Write( (byte)uValue, sizeof( byte ) );
		}

		/*
		===============
		WriteByte
		===============
		*/
		/// <summary>
		/// Writes a byte to the current stream and advances the current position within this stream by one byte.
		/// </summary>
		/// <param name="value">The byte to write to the stream.</param>
		public void WriteByte( byte value )
			=> Write( value, sizeof( byte ) );

		/*
		===============
		WriteSByte
		===============
		*/
		/// <summary>
		/// Writes a signed byte to the stream.
		/// </summary>
		/// <param name="value">The signed byte value to write.</param>
		public void WriteSByte( sbyte value )
			=> Write( value, sizeof( sbyte ) );

		/*
		===============
		WriteShort
		===============
		*/
		/// <summary>
		/// Writes a 16-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 16-bit signed integer value to write.</param>
		public void WriteShort( short value )
			=> Write( value, sizeof( short ) );

		/*
		===============
		WriteInt
		===============
		*/
		/// <summary>
		/// Writes a 32-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 32-bit signed integer value to write.</param>
		public void WriteInt( int value )
			=> Write( value, sizeof( int ) );

		/*
		===============
		WriteLong
		===============
		*/
		/// <summary>
		/// Writes a 64-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 64-bit signed integer value to write.</param>
		public void WriteLong( long value )
			=> Write( value, sizeof( long ) );

		/*
		===============
		WriteUShort
		===============
		*/
		/// <summary>
		/// Writes a 16-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 16-bit unsigned integer value to write.</param>
		public void WriteUShort( ushort value )
			=> Write( value, sizeof( ushort ) );

		/*
		===============
		WriteUInt
		===============
		*/
		/// <summary>
		/// Writes a 32-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 32-bit unsigned integer value to write.</param>
		public void WriteUInt( uint value )
			=> Write( value, sizeof( uint ) );

		/*
		===============
		WriteULong
		===============
		*/
		/// <summary>
		/// Writes a 64-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 64-bit unsigned integer value to write.</param>
		public void WriteULong( ulong value )
			=> Write( value, sizeof( ulong ) );

		/*
		===============
		WriteInt8
		===============
		*/
		/// <summary>
		/// Writes an 8-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 8-bit signed integer value to write.</param>
		public void WriteInt8( sbyte value )
			=> Write( value, sizeof( sbyte ) );

		/*
		===============
		WriteInt16
		===============
		*/
		/// <summary>
		/// Writes a 16-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 16-bit signed integer value to write.</param>
		public void WriteInt16( short value )
			=> Write( value, sizeof( short ) );

		/*
		===============
		WriteInt32
		===============
		*/
		/// <summary>
		/// Writes a 32-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 32-bit signed integer value to write.</param>
		public void WriteInt32( int value )
			=> Write( value, sizeof( int ) );

		/*
		===============
		WriteInt64
		===============
		*/
		/// <summary>
		/// Writes a 64-bit signed integer to the stream.
		/// </summary>
		/// <param name="value">The 64-bit signed integer value to write.</param>
		public void WriteInt64( long value )
			=> Write( value, sizeof( long ) );

		/*
		===============
		WriteUInt8
		===============
		*/
		/// <summary>
		/// Writes an 8-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 8-bit unsigned integer value to write.</param>
		public void WriteUInt8( byte value )
			=> Write( value, sizeof( byte ) );

		/*
		===============
		WriteUInt16
		===============
		*/
		/// <summary>
		/// Writes a 16-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 16-bit unsigned integer value to write.</param>
		public void WriteUInt16( ushort value )
			=> Write( value, sizeof( ushort ) );

		/*
		===============
		WriteUInt32
		===============
		*/
		/// <summary>
		/// Writes a 32-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 32-bit unsigned integer value to write.</param>
		public void WriteUInt32( uint value )
			=> Write( value, sizeof( uint ) );

		/*
		===============
		WriteUInt64
		===============
		*/
		/// <summary>
		/// Writes a 64-bit unsigned integer to the stream.
		/// </summary>
		/// <param name="value">The 64-bit unsigned integer value to write.</param>
		public void WriteUInt64( ulong value )
			=> Write( value, sizeof( ulong ) );

		/*
		===============
		WriteFloat
		===============
		*/
		/// <summary>
		/// Writes a 32-bit floating-point number to the stream.
		/// </summary>
		/// <param name="value">The 32-bit floating-point value to write.</param>
		public void WriteFloat( float value )
			=> Write( value, sizeof( float ) );

		/*
		===============
		WriteSingle
		===============
		*/
		/// <summary>
		/// Writes a single-precision floating-point number to the stream.
		/// </summary>
		/// <param name="value">The single-precision floating-point value to write.</param>
		public void WriteSingle( float value )
			=> Write( value, sizeof( float ) );

		/*
		===============
		WriteDouble
		===============
		*/
		/// <summary>
		/// Writes a double-precision floating-point number to the stream.
		/// </summary>
		/// <param name="value">The double-precision floating-point value to write.</param>
		public void WriteDouble( double value )
			=> Write( value, sizeof( double ) );

		/*
		===============
		WriteFloat32
		===============
		*/
		/// <summary>
		/// Writes a 32-bit floating-point number to the stream.
		/// </summary>
		/// <param name="value">The 32-bit floating-point value to write.</param>
		public void WriteFloat32( float value )
			=> Write( value, sizeof( float ) );

		/*
		===============
		WriteFloat64
		===============
		*/
		/// <summary>
		/// Writes a 64-bit floating-point number to the stream.
		/// </summary>
		/// <param name="value">The 64-bit floating-point value to write.</param>
		public void WriteFloat64( double value )
			=> Write( value, sizeof( double ) );

		/*
		===============
		WriteBoolean
		===============
		*/
		/// <summary>
		/// Writes an 8-bit boolean value to the stream.
		/// </summary>
		/// <param name="value">The 8-bit boolean value to write</param>
		public void WriteBoolean( bool value )
			=> Write( value, sizeof( bool ) );

		/*
		===============
		EnsureCapacity
		===============
		*/
		/// <summary>
		/// Ensures we have enough space in the buffer to support the provided required <paramref name="required"/>.
		/// </summary>
		/// <remarks>
		/// We're already checking the buffer is null from all the calling functions, so a null check isn't necessary here.
		/// </remarks>
		/// <param name="required">The bytes needed to write the data.</param>
		private void EnsureCapacity( long required )
		{
			if ( position + required > buffer!.Length ) {
				ResizeBuffer( Math.Max( buffer.Length * 2, position + required ) );
			}
		}

		/*
		===============
		ResizeBuffer
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="newLength"></param>
		/// <exception cref="InvalidOperationException"></exception>
		private void ResizeBuffer( long newLength )
		{
			if ( _fixedSize ) {
				throw new InvalidOperationException( "MemoryWriteStream was created with _fixedTrue = true, cannot attempt resize" );
			}
			if ( newLength > MAX_CAPACITY ) {
				throw new InvalidOperationException( $"MemoryWriteStream size has exceeded {MAX_CAPACITY} bytes... what the hell are you doing?" );
			}
			var newBuffer = new PooledBufferHandle( (int)newLength );
			if ( length > 0 ) {
				buffer!.CopyTo( newBuffer, 0, (int)length, 0 );
			}
			buffer?.Dispose();
			buffer = newBuffer;
		}

		/*
		===============
		AllocateBuffer
		===============
		*/
		/// <summary>
		/// Creates a new <see cref="IBufferHandle"/> based on the allocation strategy provided
		/// </summary>
		/// <param name="length"></param>
		/// <returns></returns>
		protected IBufferHandle AllocateBuffer( long length )
			=> strategy switch {
				AllocationStrategy.FromFile => throw new NotSupportedException( "AllocationStrategy cannot be 'FromFile' when writing to a stream." ),
				AllocationStrategy.Pooled => new PooledBufferHandle( (int)length ),
				AllocationStrategy.Standard => new StandardBufferHandle( (int)length ),
				_ => throw new IndexOutOfRangeException( nameof( strategy ) )
			};

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a value of an unmanaged type to the stream.
		/// </summary>
		/// <typeparam name="T">The type of the value to write.</typeparam>
		/// <param name="value">The value to write.</param>
		/// <param name="size">The size in bytes of the primitive we're writing.</param>
		private void Write<T>( T value, int size )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			EnsureCapacity( size );
			unsafe {
				fixed ( void* dest = buffer!.GetSlice( (int)position, size ) ) {
					Unsafe.WriteUnaligned( dest, value );
				}
			}
			BumpPosition( size );
		}

		/*
		===============
		BumpPosition
		===============
		*/
		/// <summary>
		/// Adds <paramref name="byteCount"/> to the position, increasing the stream's length if required.
		/// </summary>
		/// <param name="byteCount">The number of bytes we're adding to the stream.</param>
		private void BumpPosition( int byteCount )
		{
			long newPosition = position + byteCount;
			if ( newPosition > length ) {
				length = newPosition;
			}
			position += byteCount;
		}
	};
};
