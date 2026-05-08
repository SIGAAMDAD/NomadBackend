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
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Configs;
using System.Buffers;

namespace Nomad.FileSystem.Private.FileStreams
{
	/*
	===================================================================================

	FileReadStream

	===================================================================================
	*/
	/// <summary>
	/// Represents a read-only file stream.
	/// </summary>

	internal sealed class FileReadStream : FileStreamBase, IFileReadStream
	{
		/// <summary>
		/// Indicates whether the stream supports reading.
		/// </summary>
		public override bool CanRead => true;

		/// <summary>
		/// Indicates whether the stream supports writing.
		/// </summary>
		public override bool CanWrite => false;

		/// <summary>
		/// The binary reader for the file stream.
		/// </summary>
		private readonly FileReader _streamReader;

		/// <summary>
		/// 
		/// </summary>
		private readonly StreamFormat _format;

		/*
		===============
		FileReadStream
		===============
		*/
		/// <summary>
		/// Initializes a new instance of the FileReadStream class with the specified file path, mode, and access.
		/// </summary>
		/// <param name="config">The path to the file to read from.</param>
		public FileReadStream( FileReadConfig config )
			: base( config.FilePath!, FileMode.Open, FileAccess.Read )
		{
			ArgumentGuard.ThrowIfNull( fileStream );
			_format = config.Format;
			switch ( _format ) {
				case StreamFormat.Binary:
					_streamReader = new FileReader( new BinaryReader( fileStream ) );
					break;
				case StreamFormat.Utf8:
					_streamReader = new FileReader( new StreamReader( fileStream ) );
					break;
				default:
					Dispose();
					throw new ArgumentOutOfRangeException( nameof( config ) );
			}
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if ( isDisposed ) {
				return;
			}
			if ( disposing ) {
				_streamReader.GetStream()?.Dispose();
			}
			base.Dispose( disposing );
			isDisposed = true;
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the file stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public int Read( byte[] buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( buffer );
			return fileStream.Read( buffer, offset, count );
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the file stream and advances the position within the stream by the number of bytes read.
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
		/// Reads a sequence of bytes from the file stream into a span and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">A span of bytes. When this method returns, the span contains the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the span. This can be less than the length of the span if that many bytes are not currently available, or zero if the end of the stream has been reached.</returns>
		public int Read( Span<byte> buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			return fileStream.Read( buffer.Slice( offset, count ) );
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Reads a sequence of bytes from the file stream into a span and advances the position within the stream by the number of bytes read.
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
		/// Asynchronously reads a sequence of bytes from the file stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous read operation, containing the total number of bytes read into the buffer.</returns>
		public ValueTask<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ct.ThrowIfCancellationRequested();
			return fileStream.ReadAsync( buffer.AsMemory( offset, count ), ct );
		}

		/*
		===============
		ReadAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads a sequence of bytes from the file stream into a memory buffer and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">A memory buffer. When this method returns, the buffer contains the bytes read from the current source.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous read operation, containing the total number of bytes read into the buffer.</returns>
		public ValueTask<int> ReadAsync( Memory<byte> buffer, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ct.ThrowIfCancellationRequested();
			return fileStream.ReadAsync( buffer, ct );
		}

		/*
		===============
		ReadToEnd
		===============
		*/
		/// <summary>
		/// Reads all remaining bytes from the current position to the end of the file stream.
		/// </summary>
		/// <returns>A byte array containing the remaining data in the stream.</returns>
		public byte[] ReadToEnd()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			return _streamReader.ReadToEnd();
		}

		/*
		===============
		ReadToEndAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads all remaining bytes from the current position to the end of the file stream.
		/// </summary>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous read operation, containing a byte array with the remaining data.</returns>
		public async ValueTask<byte[]> ReadToEndAsync( CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			ct.ThrowIfCancellationRequested();

			long remaining = fileStream.Length - fileStream.Position;
			if ( remaining > int.MaxValue ) {
				throw new InvalidOperationException( "File is too large to read into a single array." );
			}
			byte[] buffer = new byte[remaining];
			int bytesRead = await fileStream.ReadAsync( buffer.AsMemory( 0, (int)remaining ), ct );
			if ( bytesRead < remaining ) {
				Array.Resize( ref buffer, bytesRead );
			}
			return buffer;
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

			long position = _streamReader.Position;
			_streamReader.Position = 0;

			int length = (int)Length;
			byte[] buffer = ArrayPool<byte>.Shared.Rent( length );
			Read( buffer, 0, length );
			stream.Write( buffer, 0, length );
			ArrayPool<byte>.Shared.Return( buffer );
			_streamReader.Position = position;
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

			long position = _streamReader.Position;
			_streamReader.Position = 0;

			int length = (int)Length;
			byte[] buffer = new byte[length];
			await ReadAsync( buffer, 0, length, ct );
			await stream.WriteAsync( buffer, ct );

			_streamReader.Position = position;
		}

		/*
		===============
		ToArray
		===============
		*/
		/// <summary>
		/// Reads the entire file stream into a byte array.
		/// </summary>
		/// <returns>A byte array containing all data from the file stream.</returns>
		public byte[] ToArray()
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			long originalPosition = fileStream.Position;
			fileStream.Position = 0;
			try {
				int length = (int)fileStream.Length;
				if ( length > int.MaxValue ) {
					throw new InvalidOperationException( "File is too large to read into a single array." );
				}
				byte[] buffer = new byte[length];
				int bytesRead = fileStream.Read( buffer, 0, length );
				if ( bytesRead < length ) {

				}
				return buffer;
			} finally {
				fileStream.Position = originalPosition;
			}
		}

		/*
		===============
		Read7BitEncodedInt
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		/// <exception cref="FormatException"></exception>
		public int Read7BitEncodedInt()
		{
			int value = 0;
			int shift = 0;
			byte b;

			do {
				b = ReadUInt8();
				value |= (b & 0x7F) << shift;
				shift += 7;
				if ( shift > 35 ) {
					throw new FormatException( "Invalid 7-bit encoded integer formatting in file." );
				}
			} while ( (b & 0x80) != 0 );

			return value;
		}

		/*
		===============
		ReadByte
		===============
		*/
		/// <summary>
		/// Reads an unsigned byte from the file stream.
		/// </summary>
		/// <returns>The unsigned byte value read from the stream.</returns>
		public byte ReadByte()
			=> _streamReader.ReadByte();

		/*
		===============
		ReadDouble
		===============
		*/
		/// <summary>
		/// Reads a double-precision floating-point number from the file stream.
		/// </summary>
		/// <returns>The double value read from the stream.</returns>
		public double ReadDouble()
			=> _streamReader.ReadDouble();

		/*
		===============
		ReadFloat
		===============
		*/
		/// <summary>
		/// Reads a single-precision floating-point number from the file stream.
		/// </summary>
		/// <returns>The float value read from the stream.</returns>
		public float ReadFloat()
			=> _streamReader.ReadFloat();

		/*
		===============
		ReadFloat32
		===============
		*/
		/// <summary>
		/// Reads a 32-bit floating-point number from the file stream.
		/// </summary>
		/// <returns>The 32-bit float value read from the stream.</returns>
		public float ReadFloat32()
			=> _streamReader.ReadFloat();

		/*
		===============
		ReadFloat64
		===============
		*/
		/// <summary>
		/// Reads a 64-bit floating-point number from the file stream.
		/// </summary>
		/// <returns>The 64-bit double value read from the stream.</returns>
		public double ReadFloat64()
			=> _streamReader.ReadDouble();

		/*
		===============
		ReadInt
		===============
		*/
		/// <summary>
		/// Reads a 32-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 32-bit signed integer value read from the stream.</returns>
		public int ReadInt()
			=> _streamReader.ReadInt();

		/*
		===============
		ReadInt16
		===============
		*/
		/// <summary>
		/// Reads a 16-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 16-bit signed integer value read from the stream.</returns>
		public short ReadInt16()
			=> _streamReader.ReadShort();

		/*
		===============
		ReadInt32
		===============
		*/
		/// <summary>
		/// Reads a 32-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 32-bit signed integer value read from the stream.</returns>
		public int ReadInt32()
			=> _streamReader.ReadInt();

		/*
		===============
		ReadInt64
		===============
		*/
		/// <summary>
		/// Reads a 64-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 64-bit signed integer value read from the stream.</returns>
		public long ReadInt64()
			=> _streamReader.ReadLong();

		/*
		===============
		ReadInt8
		===============
		*/
		/// <summary>
		/// Reads an 8-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 8-bit signed integer value read from the stream.</returns>
		public sbyte ReadInt8()
			=> _streamReader.ReadSByte();

		/*
		===============
		ReadLong
		===============
		*/
		/// <summary>
		/// Reads a 64-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 64-bit signed integer value read from the stream.</returns>
		public long ReadLong()
			=> _streamReader.ReadLong();

		/*
		===============
		ReadSByte
		===============
		*/
		/// <summary>
		/// Reads a signed byte from the file stream.
		/// </summary>
		/// <returns>The signed byte value read from the stream.</returns>
		public sbyte ReadSByte()
			=> _streamReader.ReadSByte();

		/*
		===============
		ReadShort
		===============
		*/
		/// <summary>
		/// Reads a 16-bit signed integer from the file stream.
		/// </summary>
		/// <returns>The 16-bit signed integer value read from the stream.</returns>
		public short ReadShort()
			=> _streamReader.ReadShort();

		/*
		===============
		ReadSingle
		===============
		*/
		/// <summary>
		/// Reads a single-precision floating-point number from the file stream.
		/// </summary>
		/// <returns>The single-precision float value read from the stream.</returns>
		public float ReadSingle()
			=> _streamReader.ReadFloat();

		/*
		===============
		ReadString
		===============
		*/
		/// <summary>
		/// Reads a string from the file stream.
		/// </summary>
		/// <returns>The string value read from the stream.</returns>
		public string ReadString()
			=> _streamReader.ReadString();

		/*
		===============
		ReadUInt
		===============
		*/
		/// <summary>
		/// Reads a 32-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 32-bit unsigned integer value read from the stream.</returns>
		public uint ReadUInt()
			=> _streamReader.ReadUInt();

		/*
		===============
		ReadUInt16
		===============
		*/
		/// <summary>
		/// Reads a 16-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 16-bit unsigned integer value read from the stream.</returns>
		public ushort ReadUInt16()
			=> _streamReader.ReadUShort();

		/*
		===============
		ReadUInt32
		===============
		*/
		/// <summary>
		/// Reads a 32-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 32-bit unsigned integer value read from the stream.</returns>
		public uint ReadUInt32()
			=> _streamReader.ReadUInt();

		/*
		===============
		ReadUInt64
		===============
		*/
		/// <summary>
		/// Reads a 64-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 64-bit unsigned integer value read from the stream.</returns>
		public ulong ReadUInt64()
			=> _streamReader.ReadULong();

		/*
		===============
		ReadUInt8
		===============
		*/
		/// <summary>
		/// Reads an 8-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 8-bit unsigned integer value read from the stream.</returns>
		public byte ReadUInt8()
			=> _streamReader.ReadByte();

		/*
		===============
		ReadULong
		===============
		*/
		/// <summary>
		/// Reads a 64-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 64-bit unsigned integer value read from the stream.</returns>
		public ulong ReadULong()
			=> _streamReader.ReadULong();

		/*
		===============
		ReadUShort
		===============
		*/
		/// <summary>
		/// Reads a 16-bit unsigned integer from the file stream.
		/// </summary>
		/// <returns>The 16-bit unsigned integer value read from the stream.</returns>
		public ushort ReadUShort()
			=> _streamReader.ReadUShort();

		/*
		===============
		ReadBoolean
		===============
		*/
		/// <summary>
		/// Reads an 8-bit boolean from the file stream.
		/// </summary>
		/// <returns>The 8-bit boolean value read from the stream.</returns>
		public bool ReadBoolean()
			=> _streamReader.ReadBoolean();
	}
};
