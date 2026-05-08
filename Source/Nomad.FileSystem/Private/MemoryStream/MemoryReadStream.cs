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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Memory.Buffers;

namespace Nomad.FileSystem.Private.MemoryStream
{
	/*
	===================================================================================

	MemoryReadStream

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal class MemoryReadStream : MemoryStreamBase, IMemoryReadStream
	{
		/// <summary>
		/// Whether the stream can be read from.
		/// </summary>
		public override bool CanRead => true;

		/// <summary>
		/// Whether the stream can be written to.
		/// </summary>
		public override bool CanWrite => false;

		/*
		===============
		MemoryReadStream
		===============
		*/
		/// <summary>
		/// Initializes a new instance of the MemoryReadStream class with the specified buffer and length.
		/// </summary>
		/// <param name="config"></param>
		/// <param name="createBuffer"></param>
		public MemoryReadStream( MemoryReadConfig config, bool createBuffer = true )
			: base( config.Strategy )
		{
			long maxCapacity = config.MaxCapacity ?? Constants.FileSystem.MAXIMUM_MEMORY_STREAM_CAPACITY;
			if ( createBuffer ) {
				buffer = config.Buffer ?? AllocateBuffer( maxCapacity );
			}
			if ( config.Buffer != null ) {
				length = config.Buffer.Length;
			}
			position = 0;
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the stream. (Not implemented for read-only streams)
		/// </summary>
		/// <exception cref="NotSupportedException"></exception>
		public override void Flush()
		{
			throw new NotSupportedException( "Cannot flush a MemoryReadStream" );
		}

		/*
		===============
		FlushAsync
		===============
		*/
		/// <summary>
		/// Asynchronously flushes the stream. (Not implemented for read-only streams)
		/// </summary>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous flush operation.</returns>
		/// <exception cref="NotSupportedException"></exception>
		public override async ValueTask FlushAsync( CancellationToken ct = default )
		{
			throw new NotSupportedException( "Cannot flush a MemoryReadStream" );
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
			this.length = length;
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public int Read( byte[] buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			if ( offset + count >= buffer.Length ) {
				count = buffer.Length - offset;
			}
			this.buffer!.CopyTo( buffer, offset, count, (int)position );
			position += count;
			return count;
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public int Read( byte[] buffer )
			=> Read( buffer, 0, buffer.Length );

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the current stream into a span and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">A span of bytes. When this method returns, the span contains the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the span. This can be less than the length of the span if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public int Read( Span<byte> buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			if ( offset + count >= buffer.Length ) {
				count = buffer.Length - offset;
			}
			this.buffer!.CopyTo( buffer, offset, count, (int)position );
			position += count;
			return count;
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the current stream into a span and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">A span of bytes. When this method returns, the span contains the bytes read from the current source.</param>
		/// <returns>The total number of bytes read into the span. This can be less than the length of the span if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public int Read( Span<byte> buffer )
			=> Read( buffer, 0, buffer.Length );

		/*
		===============
		ReadAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous read operation, containing the total number of bytes read into the buffer.</returns>
		public async ValueTask<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			ct.ThrowIfCancellationRequested();
			return Read( buffer, offset, count );
		}

		/*
		===============
		ReadAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads a sequence of bytes from the current stream into a memory buffer and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">A memory buffer. When this method returns, the buffer contains the bytes read from the current source.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous read operation, containing the total number of bytes read into the buffer.</returns>
		public async ValueTask<int> ReadAsync( Memory<byte> buffer, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( buffer );

			ct.ThrowIfCancellationRequested();
			return Read( buffer.Span );
		}

		/*
		===============
		ReadToEnd
		===============
		*/
		/// <summary>
		/// Reads all remaining bytes from the current position to the end of the stream.
		/// </summary>
		/// <returns>A byte array containing the remaining data in the stream.</returns>
		public byte[] ReadToEnd()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			long remaining = length - position;
			byte[] result = new byte[remaining];
			buffer!.CopyTo( result, 0, (int)remaining, (int)position );
			position += remaining;
			return result;
		}

		/*
		===============
		ReadToEndAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads all remaining bytes from the current position to the end of the stream.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous read operation, containing a byte array with the remaining data.</returns>
		public async ValueTask<byte[]> ReadToEndAsync( CancellationToken cancellationToken = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			cancellationToken.ThrowIfCancellationRequested();
			return ReadToEnd();
		}

		/*
		===============
		WriteToStream
		===============
		*/
		/// <summary>
		/// Writes the entire content of this stream to the provided stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public void WriteToStream( IWriteStream stream )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			stream.Write( buffer!.Span, 0, (int)length );
		}

		/*
		===============
		WriteToStreamAsync
		===============
		*/
		/// <summary>
		/// Asynchronously writes the entire content of this stream to the provided stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="ct">The token to monitor for cancellation requests.</param>
		public async ValueTask WriteToStreamAsync( IWriteStream stream, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ct.ThrowIfCancellationRequested();

			await stream.WriteAsync( buffer!.Memory, ct );
		}

		/*
		===============
		ToArray
		===============
		*/
		/// <summary>
		/// Returns the underlying buffer as a byte array.
		/// </summary>
		/// <returns>The underlying byte array.</returns>
		public byte[] ToArray()
			=> buffer!.ToArray();

		/*
		===============
		ReadSByte
		===============
		*/
		/// <summary>
		/// Reads a signed byte from the stream.
		/// </summary>
		/// <returns>The signed byte value read from the stream.</returns>
		public sbyte ReadSByte()
			=> Read<sbyte>( sizeof( sbyte ) );

		/*
		===============
		ReadShort
		===============
		*/
		/// <summary>
		/// Reads a 16-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 16-bit signed integer value read from the stream.</returns>
		public short ReadShort()
			=> Read<short>( sizeof( short ) );

		/*
		===============
		ReadInt
		===============
		*/
		/// <summary>
		/// Reads a 32-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 32-bit signed integer value read from the stream.</returns>
		public int ReadInt()
			=> Read<int>( sizeof( int ) );

		/*
		===============
		ReadLong
		===============
		*/
		/// <summary>
		/// Reads a 64-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 64-bit signed integer value read from the stream.</returns>
		public long ReadLong()
			=> Read<long>( sizeof( long ) );

		/*
		===============
		ReadByte
		===============
		*/
		/// <summary>
		/// Reads an unsigned byte from the stream.
		/// </summary>
		/// <returns>The unsigned byte value read from the stream.</returns>
		public byte ReadByte()
			=> Read<byte>( sizeof( byte ) );

		/*
		===============
		ReadUShort
		===============
		*/
		/// <summary>
		/// Reads a 16-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 16-bit unsigned integer value read from the stream.</returns>
		public ushort ReadUShort()
			=> Read<ushort>( sizeof( ushort ) );

		/*
		===============
		ReadUInt
		===============
		*/
		/// <summary>
		/// Reads a 32-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 32-bit unsigned integer value read from the stream.</returns>
		public uint ReadUInt()
			=> Read<uint>( sizeof( uint ) );

		/*
		===============
		ReadULong
		===============
		*/
		/// <summary>
		/// Reads a 64-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 64-bit unsigned integer value read from the stream.</returns>
		public ulong ReadULong()
			=> Read<ulong>( sizeof( ulong ) );

		/*
		===============
		ReadInt8
		===============
		*/
		/// <summary>
		/// Reads an 8-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 8-bit signed integer value read from the stream.</returns>
		public sbyte ReadInt8()
			=> Read<sbyte>( sizeof( sbyte ) );

		/*
		===============
		ReadInt16
		===============
		*/
		/// <summary>
		/// Reads a 16-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 16-bit signed integer value read from the stream.</returns>
		public short ReadInt16()
			=> Read<short>( sizeof( short ) );

		/*
		===============
		ReadInt32
		===============
		*/
		/// <summary>
		/// Reads a 32-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 32-bit signed integer value read from the stream.</returns>
		public int ReadInt32()
			=> Read<int>( sizeof( int ) );

		/*
		===============
		ReadInt64
		===============
		*/
		/// <summary>
		/// Reads a 64-bit signed integer from the stream.
		/// </summary>
		/// <returns>The 64-bit signed integer value read from the stream.</returns>
		public long ReadInt64()
			=> Read<long>( sizeof( long ) );

		/*
		===============
		ReadUInt8
		===============
		*/
		/// <summary>
		/// Reads an 8-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 8-bit unsigned integer value read from the stream.</returns>
		public byte ReadUInt8()
			=> Read<byte>( sizeof( byte ) );

		/*
		===============
		ReadUInt16
		===============
		*/
		/// <summary>
		/// Reads a 16-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 16-bit unsigned integer value read from the stream.</returns>
		public ushort ReadUInt16()
			=> Read<ushort>( sizeof( ushort ) );

		/*
		===============
		ReadUInt32
		===============
		*/
		/// <summary>
		/// Reads a 32-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 32-bit unsigned integer value read from the stream.</returns>
		public uint ReadUInt32()
			=> Read<uint>( sizeof( uint ) );

		/*
		===============
		ReadUInt64
		===============
		*/
		/// <summary>
		/// Reads a 64-bit unsigned integer from the stream.
		/// </summary>
		/// <returns>The 64-bit unsigned integer value read from the stream.</returns>
		public ulong ReadUInt64()
			=> Read<ulong>( sizeof( ulong ) );

		/*
		===============
		ReadFloat
		===============
		*/
		/// <summary>
		/// Reads a 32-bit floating-point number from the stream.
		/// </summary>
		/// <returns>The 32-bit floating-point value read from the stream.</returns>
		public float ReadFloat()
			=> Read<float>( sizeof( float ) );

		/*
		===============
		ReadSingle
		===============
		*/
		/// <summary>
		/// Reads a single-precision floating-point number from the stream.
		/// </summary>
		/// <returns>The single-precision floating-point value read from the stream.</returns>
		public float ReadSingle()
			=> Read<float>( sizeof( float ) );

		/*
		===============
		ReadDouble
		===============
		*/
		/// <summary>
		/// Reads a double-precision floating-point number from the stream.
		/// </summary>
		/// <returns>The double-precision floating-point value read from the stream.</returns>
		public double ReadDouble()
			=> Read<double>( sizeof( double ) );

		/*
		===============
		ReadFloat32
		===============
		*/
		/// <summary>
		/// Reads a 32-bit floating-point number from the stream.
		/// </summary>
		/// <returns>The 32-bit floating-point value read from the stream.</returns>
		public float ReadFloat32()
			=> Read<float>( sizeof( float ) );

		/*
		===============
		ReadFloat64
		===============
		*/
		/// <summary>
		/// Reads a 64-bit floating-point number from the stream.
		/// </summary>
		/// <returns>The 64-bit floating-point value read from the stream.</returns>
		public double ReadFloat64()
			=> Read<double>( sizeof( double ) );

		/*
		===============
		ReadBoolean
		===============
		*/
		/// <summary>
		/// Reads an 8-bit value from the stream.
		/// </summary>
		/// <returns>The 8-bit value read from the stream</returns>
		public bool ReadBoolean()
			=> Read<bool>( sizeof( bool ) );

		/*
		===============
		ReadString
		===============
		*/
		/// <summary>
		/// Reads a UTF-8 encoded string from the stream, prefixed with its length as a 7-bit encoded integer.
		/// </summary>
		/// <returns>The string read from the stream.</returns>
		public string ReadString()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			int byteCount = Read7BitEncodedInt();
			if ( byteCount == 0 ) {
				return string.Empty;
			}

			string value;
			unsafe {
				fixed ( byte* src = buffer!.GetSlice( (int)position, byteCount ) ) {
					value = Encoding.UTF8.GetString( src, byteCount );
				}
			}
			position += byteCount;
			return value;
		}

		/*
		===============
		Read7BitEncodedInt
		===============
		*/
		/// <summary>
		/// Reads an encoded/compressed integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="FormatException"></exception>
		public int Read7BitEncodedInt()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			int value = 0;
			int shift = 0;
			byte b;

			do {
				b = ReadUInt8();
				value |= (b & 0x7F) << shift;
				shift += 7;
				if ( shift > 35 ) {
					throw new FormatException( "Invalid 7-bit encoded integer formatting in stream." );
				}
			} while ( (b & 0x80) != 0 );

			return value;
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private T Read<T>( int size )
			where T : unmanaged
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			if ( position + size > length ) {
				throw new EndOfStreamException();
			}

			T value;
			unsafe {
				fixed ( void* ptr = buffer!.GetSlice( (int)position, size ) ) {
					value = Unsafe.ReadUnaligned<T>( ptr );
				}
			}
			position += size;
			return value;
		}

		/*
		===============
		AllocateBuffer
		===============
		*/
		/// <summary>
		/// Creates a new <see cref="IBufferHandle"/> based on the allocation strategy provided.
		/// </summary>
		/// <param name="length"></param>
		/// <returns></returns>
		protected virtual IBufferHandle AllocateBuffer( long length )
			=> strategy switch {
				AllocationStrategy.FromFile => throw new NotSupportedException( "AllocationStrategy cannot be 'FromFile' with a pure memory stream." ),
				AllocationStrategy.Pooled => new PooledBufferHandle( (int)length ),
				AllocationStrategy.Standard => new StandardBufferHandle( (int)length ),
				_ => throw new IndexOutOfRangeException( nameof( strategy ) )
			};
	};
};
