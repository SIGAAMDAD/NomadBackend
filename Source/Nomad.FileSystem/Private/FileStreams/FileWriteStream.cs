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
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.FileSystem.Configs;

namespace Nomad.FileSystem.Private.FileStreams
{
	/*
	===================================================================================

	FileWriteStream

	===================================================================================
	*/
	/// <summary>
	/// Represents a write-only file stream.
	/// </summary>

	internal sealed class FileWriteStream : FileStreamBase, IFileWriteStream
	{
		/// <summary>
		/// Indicates whether the stream supports reading.
		/// </summary>
		public override bool CanRead => false;

		/// <summary>
		/// Indicates whether the stream supports writing.
		/// </summary>
		public override bool CanWrite => true;

		/// <summary>
		/// The binary writer for the file stream.
		/// </summary>
		private readonly FileWriter _streamWriter;

		/// <summary>
		/// The current output format of the stream.
		/// </summary>
		private readonly StreamFormat _format;

		/*
		===============
		FileWriteStream
		===============
		*/
		/// <summary>
		/// Initializes a new instance of the FileWriteStream class with the specified file path, mode, and access.
		/// </summary>
		/// <param name="config"></param>
		public FileWriteStream( FileWriteConfig config )
			: base( config.FilePath, config.Append ? FileMode.Append : FileMode.Create, FileAccess.Write )
		{
			ArgumentGuard.ThrowIfNull( fileStream );
			_format = config.Format;
			switch ( _format ) {
				case StreamFormat.Binary:
					_streamWriter = new FileWriter( new BinaryWriter( fileStream ) );
					break;
				case StreamFormat.Utf8:
					_streamWriter = new FileWriter( new StreamWriter( fileStream ) );
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
				_streamWriter.GetStream()?.Dispose();
			}
			base.Dispose( disposing );
			isDisposed = true;
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
			base.Flush();
			_streamWriter.Flush();
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the file stream's buffer to the underlying file.
		/// </summary>
		public override async ValueTask FlushAsync( CancellationToken ct = default )
		{
			await base.FlushAsync( ct );
			await _streamWriter.FlushAsync( ct );
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public void Write( byte[] buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( fileStream );
			fileStream.Write( buffer, offset, count );
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
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
		/// Writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only span of bytes. This method copies the contents of the span to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public void Write( ReadOnlySpan<byte> buffer, int offset, int count )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( fileStream );
			fileStream.Write( buffer.Slice( offset, count ) );
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// Writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
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
		/// Asynchronously writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public async ValueTask WriteAsync( byte[] buffer, int offset, int count, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( fileStream );
			ArgumentGuard.ThrowIfNull( buffer );

			ct.ThrowIfCancellationRequested();
			await fileStream.WriteAsync( buffer.AsMemory( offset, count ), ct );
		}

		/*
		===============
		WriteAsync
		===============
		*/
		/// <summary>
		/// Asynchronously writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only memory buffer. This method copies the contents of the buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public async ValueTask WriteAsync( ReadOnlyMemory<byte> buffer, int offset, int count, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( fileStream );
			ArgumentGuard.ThrowIfNull( buffer );

			ct.ThrowIfCancellationRequested();
			await fileStream.WriteAsync( buffer.Slice( offset, count ), ct );
		}

		/*
		===============
		WriteAsync
		===============
		*/
		/// <summary>
		/// Asynchronously writes a sequence of bytes to the file stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">A read-only memory buffer. This method copies the contents of the buffer to the current stream.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public async ValueTask WriteAsync( ReadOnlyMemory<byte> buffer, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( fileStream );
			ArgumentGuard.ThrowIfNull( buffer );

			ct.ThrowIfCancellationRequested();
			await fileStream.WriteAsync( buffer, ct );
		}

		/*
		===============
		Write7BitEncodedInteger
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void Write7BitEncodedInt( int value )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );

			uint uValue = (uint)value;
			while ( uValue >= 0x80 ) {
				WriteUInt8( (byte)(uValue | 0x80) );
				uValue >>= 7;
			}
			WriteUInt8( (byte)uValue );
		}

		/*
		===============
		WriteByte
		===============
		*/
		/// <summary>
		/// Writes a byte to the file stream.
		/// </summary>
		/// <param name="value">The byte value to write.</param>
		public void WriteByte( byte value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteDouble
		===============
		*/
		/// <summary>
		/// Writes a double-precision floating-point number to the file stream.
		/// </summary>
		/// <param name="value">The double value to write.</param>
		public void WriteDouble( double value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteFloat
		===============
		*/
		/// <summary>
		/// Writes a single-precision floating-point number to the file stream.
		/// </summary>
		/// <param name="value">The float value to write.</param>
		public void WriteFloat( float value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteFloat32
		===============
		*/
		/// <summary>
		/// Writes a 32-bit floating-point number to the file stream.
		/// </summary>
		/// <param name="value">The 32-bit float value to write.</param>
		public void WriteFloat32( float value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteFloat64
		===============
		*/
		/// <summary>
		/// Writes a 64-bit floating-point number to the file stream.
		/// </summary>
		/// <param name="value">The 64-bit double value to write.</param>
		public void WriteFloat64( double value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteFromStream
		===============
		*/
		/// <summary>
		/// Reads all bytes from the specified read stream and writes them to the file stream.
		/// </summary>
		/// <param name="stream">The read stream to copy from.</param>
		public void WriteFromStream( IReadStream stream )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( stream );
			ArgumentGuard.ThrowIfNull( fileStream );

			long current = stream.Position;

			byte[] buffer = ArrayPool<byte>.Shared.Rent( 4096 );
			int bytesRead;
			stream.Position = 0;
			while ( (bytesRead = stream.Read( buffer, 0, buffer.Length )) > 0 ) {
				fileStream.Write( buffer, 0, bytesRead );
			}
			stream.Position = current;
		}

		/*
		===============
		WriteFromStreamAsync
		===============
		*/
		/// <summary>
		/// Asynchronously reads all bytes from the specified read stream and writes them to the file stream.
		/// </summary>
		/// <param name="stream">The read stream to copy from.</param>
		/// <param name="ct">A token to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous copy operation.</returns>
		public async ValueTask WriteFromStreamAsync( IReadStream stream, CancellationToken ct = default )
		{
			StateGuard.ThrowIfDisposed( isDisposed, this );
			ArgumentGuard.ThrowIfNull( stream );
			ArgumentGuard.ThrowIfNull( fileStream );

			byte[] buffer = ArrayPool<byte>.Shared.Rent( 4096 );
			int bytesRead;
			while ( (bytesRead = await stream.ReadAsync( buffer, 0, buffer.Length, ct )) > 0 ) {
				await fileStream.WriteAsync( buffer.AsMemory( 0, bytesRead ), ct );
			}
		}

		/*
		===============
		WriteInt
		===============
		*/
		/// <summary>
		/// Writes a 32-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 32-bit signed integer value to write.</param>
		public void WriteInt( int value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteInt16
		===============
		*/
		/// <summary>
		/// Writes a 16-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 16-bit signed integer value to write.</param>
		public void WriteInt16( short value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteInt32
		===============
		*/
		/// <summary>
		/// Writes a 32-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 32-bit signed integer value to write.</param>
		public void WriteInt32( int value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteInt64
		===============
		*/
		/// <summary>
		/// Writes a 64-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 64-bit signed integer value to write.</param>
		public void WriteInt64( long value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteInt8
		===============
		*/
		/// <summary>
		/// Writes an 8-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 8-bit signed integer value to write.</param>
		public void WriteInt8( sbyte value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteLong
		===============
		*/
		/// <summary>
		/// Writes a 64-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 64-bit signed integer value to write.</param>
		public void WriteLong( long value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteSByte
		===============
		*/
		/// <summary>
		/// Writes a signed byte to the file stream.
		/// </summary>
		/// <param name="value">The signed byte value to write.</param>
		public void WriteSByte( sbyte value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteShort
		===============
		*/
		/// <summary>
		/// Writes a 16-bit signed integer to the file stream.
		/// </summary>
		/// <param name="value">The 16-bit signed integer value to write.</param>
		public void WriteShort( short value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteSingle
		===============
		*/
		/// <summary>
		/// Writes a single-precision floating-point number to the file stream.
		/// </summary>
		/// <param name="value">The single-precision float value to write.</param>
		public void WriteSingle( float value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteString
		===============
		*/
		/// <summary>
		/// Writes a string to the file stream.
		/// </summary>
		/// <param name="value">The string value to write.</param>
		public void WriteString( string value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteUInt
		===============
		*/
		/// <summary>
		/// Writes a 32-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 32-bit unsigned integer value to write.</param>
		public void WriteUInt( uint value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteUInt16
		===============
		*/
		/// <summary>
		/// Writes a 16-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 16-bit unsigned integer value to write.</param>
		public void WriteUInt16( ushort value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteUInt32
		===============
		*/
		/// <summary>
		/// Writes a 32-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 32-bit unsigned integer value to write.</param>
		public void WriteUInt32( uint value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteUInt64
		===============
		*/
		/// <summary>
		/// Writes a 64-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 64-bit unsigned integer value to write.</param>
		public void WriteUInt64( ulong value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteUInt8
		===============
		*/
		/// <summary>
		/// Writes an 8-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 8-bit unsigned integer value to write.</param>
		public void WriteUInt8( byte value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteULong
		===============
		*/
		/// <summary>
		/// Writes a 64-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 64-bit unsigned integer value to write.</param>
		public void WriteULong( ulong value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteUShort
		===============
		*/
		/// <summary>
		/// Writes a 16-bit unsigned integer to the file stream.
		/// </summary>
		/// <param name="value">The 16-bit unsigned integer value to write.</param>
		public void WriteUShort( ushort value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteBoolean
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void WriteBoolean( bool value )
			=> _streamWriter.Write( value );

		/*
		===============
		WriteLine
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		public void WriteLine( string line )
			=> _streamWriter.Write( $"{line}\n" );

		/*
		===============
		WriteLine
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		public void WriteLine( ReadOnlySpan<char> line )
		{
			_streamWriter.Write( line );
			_streamWriter.Write( '\n' );
		}

		/*
		===============
		WriteLineAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async ValueTask WriteLineAsync( string line, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			_streamWriter.Write( $"{line}\n" );
		}

		/*
		===============
		WriteLineAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async ValueTask WriteLineAsync( ReadOnlyMemory<char> line, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			_streamWriter.Write( line.Span );
			_streamWriter.Write( '\n' );
		}
	};
};
