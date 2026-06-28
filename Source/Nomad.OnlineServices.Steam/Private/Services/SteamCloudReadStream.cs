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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.FileSystem.Streams;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamCloudReadStream

	===================================================================================
	*/
	/// <summary>
	/// Read-only in-memory stream for Steam cloud file payloads.
	/// </summary>

	internal sealed class SteamCloudReadStream : IFileReadStream
	{
		public string FilePath { get; }
		public bool IsOpen => !_isDisposed;

		public long Length {
			get => _stream.Length;
			set => SetLength( value );
		}

		public long Position {
			get => _stream.Position;
			set => _stream.Position = value;
		}

		public bool CanRead => !_isDisposed;
		public bool CanWrite => false;
		public bool CanSeek => !_isDisposed;

		private readonly MemoryStream _stream;
		private readonly BinaryReader _reader;

		private bool _isDisposed = false;

		public SteamCloudReadStream( string filePath, byte[] data )
		{
			FilePath = filePath ?? throw new ArgumentNullException( nameof( filePath ) );
			_stream = new MemoryStream( data ?? throw new ArgumentNullException( nameof( data ) ), writable: false );
			_reader = new BinaryReader( _stream, Encoding.UTF8, leaveOpen: true );
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			_reader.Dispose();
			_stream.Dispose();
			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return default;
		}

		public void Close()
			=> Dispose();

		public void Flush()
			=> throw new NotSupportedException( "Cannot flush a read-only Steam cloud stream." );

		public ValueTask FlushAsync( CancellationToken ct = default )
			=> throw new NotSupportedException( "Cannot flush a read-only Steam cloud stream." );

		public long Seek( long offset, SeekOrigin origin )
		{
			ThrowIfDisposed();
			return _stream.Seek( offset, origin );
		}

		public void SetLength( long length )
			=> throw new NotSupportedException( "Cannot set the length of a read-only Steam cloud stream." );

		public int Read( byte[] buffer, int offset, int count )
		{
			ThrowIfDisposed();
			return _stream.Read( buffer, offset, count );
		}

		public int Read( byte[] buffer )
			=> Read( buffer, 0, buffer.Length );

		public int Read( Span<byte> buffer, int offset, int count )
		{
			ThrowIfDisposed();
			return _stream.Read( buffer.Slice( offset, count ) );
		}

		public int Read( Span<byte> buffer )
		{
			ThrowIfDisposed();
			return _stream.Read( buffer );
		}

		public ValueTask<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();
			return new ValueTask<int>( _stream.Read( buffer, offset, count ) );
		}

		public ValueTask<int> ReadAsync( Memory<byte> buffer, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			ct.ThrowIfCancellationRequested();
			return new ValueTask<int>( _stream.Read( buffer.Span ) );
		}

		public byte[] ReadToEnd()
		{
			ThrowIfDisposed();

			byte[] data = new byte[(int)( _stream.Length - _stream.Position )];
			ReadExactly( _stream, data );
			return data;
		}

		public ValueTask<byte[]> ReadToEndAsync( CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return new ValueTask<byte[]>( ReadToEnd() );
		}

		public void WriteToStream( IWriteStream stream )
		{
			ThrowIfDisposed();
			if ( stream == null ) {
				throw new ArgumentNullException( nameof( stream ) );
			}

			long oldPosition = _stream.Position;
			_stream.Position = 0;
			stream.Write( ToArray() );
			_stream.Position = oldPosition;
		}

		public async ValueTask WriteToStreamAsync( IWriteStream stream, CancellationToken ct = default )
		{
			ThrowIfDisposed();
			if ( stream == null ) {
				throw new ArgumentNullException( nameof( stream ) );
			}

			ct.ThrowIfCancellationRequested();

			long oldPosition = _stream.Position;
			_stream.Position = 0;
			await stream.WriteAsync( ToArray(), 0, (int)_stream.Length, ct ).ConfigureAwait( false );
			_stream.Position = oldPosition;
		}

		public byte[] ToArray()
			=> _stream.ToArray();

		public int Read7BitEncodedInt()
		{
			ThrowIfDisposed();

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

		public sbyte ReadSByte()
			=> _reader.ReadSByte();

		public short ReadShort()
			=> _reader.ReadInt16();

		public int ReadInt()
			=> _reader.ReadInt32();

		public long ReadLong()
			=> _reader.ReadInt64();

		public byte ReadByte()
			=> _reader.ReadByte();

		public ushort ReadUShort()
			=> _reader.ReadUInt16();

		public uint ReadUInt()
			=> _reader.ReadUInt32();

		public ulong ReadULong()
			=> _reader.ReadUInt64();

		public sbyte ReadInt8()
			=> ReadSByte();

		public short ReadInt16()
			=> ReadShort();

		public int ReadInt32()
			=> ReadInt();

		public long ReadInt64()
			=> ReadLong();

		public byte ReadUInt8()
			=> ReadByte();

		public ushort ReadUInt16()
			=> ReadUShort();

		public uint ReadUInt32()
			=> ReadUInt();

		public ulong ReadUInt64()
			=> ReadULong();

		public float ReadFloat()
			=> _reader.ReadSingle();

		public float ReadSingle()
			=> ReadFloat();

		public double ReadDouble()
			=> _reader.ReadDouble();

		public float ReadFloat32()
			=> ReadFloat();

		public double ReadFloat64()
			=> ReadDouble();

		public bool ReadBoolean()
			=> _reader.ReadBoolean();

		public string ReadString()
		{
			ThrowIfDisposed();

			int byteCount = Read7BitEncodedInt();
			if ( byteCount == 0 ) {
				return string.Empty;
			}

			byte[] data = _reader.ReadBytes( byteCount );
			if ( data.Length != byteCount ) {
				throw new EndOfStreamException( "Unexpected end of stream while reading a string." );
			}

			return Encoding.UTF8.GetString( data, 0, data.Length );
		}

		private void ThrowIfDisposed()
		{
			if ( _isDisposed ) {
				throw new ObjectDisposedException( nameof( SteamCloudReadStream ) );
			}
		}

		private static void ReadExactly( Stream stream, Span<byte> buffer )
		{
			int totalRead = 0;
			while ( totalRead < buffer.Length ) {
				int bytesRead = stream.Read( buffer.Slice( totalRead ) );
				if ( bytesRead == 0 ) {
					throw new EndOfStreamException();
				}

				totalRead += bytesRead;
			}
		}
	};
};
