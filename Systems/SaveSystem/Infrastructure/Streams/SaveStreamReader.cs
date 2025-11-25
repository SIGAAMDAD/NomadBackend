/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Streams {
	/*
	===================================================================================
	
	SaveReaderStream
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// Reads strings in <see cref="Encoding.UTF8"/> format.
	/// </remarks>

	internal ref struct SaveReaderStream : ISaveFileStream {
		/// <summary>
		/// The length of the stream (the file's size).
		/// </summary>
		public readonly int Length => _length;
		private readonly int _length = 0;

		/// <summary>
		/// The current offset in the buffer.
		/// </summary>
		public readonly int Position => _position;
		private int _position = 0;

		/// <summary>
		/// The buffer the stream reads from.
		/// </summary>
		public readonly byte[]? Buffer => _buffer;
		private readonly byte[] _buffer;

		/*
		===============
		SaveReaderStream
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// Exceptions such as IO or FileNotFound should be handled outside of this class.
		/// </remarks>
		/// <param name="stream"></param>
		public SaveReaderStream( string? filepath ) {
			ArgumentException.ThrowIfNullOrEmpty( filepath );

			using System.IO.FileStream stream = new System.IO.FileStream( filepath, System.IO.FileMode.Open, System.IO.FileAccess.Read );
			
			_length = (int)( stream.Length - stream.Position );
			_buffer = ArrayPool<byte>.Shared.Rent( Length );
			stream.ReadExactly( _buffer, 0, Length );

			// kill the stream now that we've read it all
			stream.Dispose();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Releases the read stream buffer's memory.
		/// </summary>
		public readonly void Dispose() {
			if ( _buffer != null ) {
				ArrayPool<byte>.Shared.Return( _buffer );
			}
		}

		/*
		===============
		Seek
		===============
		*/
		/// <summary>
		/// Sets the stream offset to the desired <paramref name="position"/> based on the <paramref name="origin"/>
		/// </summary>
		/// <param name="position"></param>
		/// <param name="origin"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void Seek( int position, System.IO.SeekOrigin origin ) {
			ArgumentOutOfRangeException.ThrowIfLessThan( position, 0 );
			ArgumentNullException.ThrowIfNull( Buffer );

			switch ( origin ) {
				case System.IO.SeekOrigin.Begin:
					ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( position, Buffer.Length );
					_position = position;
					break;
				case System.IO.SeekOrigin.Current:
					ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( position + Position, Buffer.Length );
					_position += position;
					break;
				case System.IO.SeekOrigin.End:
					ArgumentOutOfRangeException.ThrowIfGreaterThan( position, 0 );
					_position = Buffer.Length - 1;
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof( origin ) );
			}
		}

		/*
		===============
		Reset
		===============
		*/
		/// <summary>
		/// Resets all read progress in the stream.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Reset() => _position = 0;

		/*
		===============
		ReadInt8
		===============
		*/
		/// <summary>
		/// Reads a 8 bit signed integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public sbyte ReadInt8() => ReadPrimitive<sbyte>();

		/*
		===============
		ReadInt16
		===============
		*/
		/// <summary>
		/// Reads a 16 bit signed integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public short ReadInt16() => ReadPrimitive<short>();

		/*
		===============
		ReadInt32
		===============
		*/
		/// <summary>
		/// Reads a 32 bit signed integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public int ReadInt32() => ReadPrimitive<int>();

		/*
		===============
		ReadInt64
		===============
		*/
		/// <summary>
		/// Reads a 64 bit signed integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public long ReadInt64() => ReadPrimitive<long>();

		/*
		===============
		ReadUInt8
		===============
		*/
		/// <summary>
		/// Reads an 8 bit unsigned integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public byte ReadUInt8() => ReadPrimitive<byte>();

		/*
		===============
		ReadUInt16
		===============
		*/
		/// <summary>
		/// Reads a 16 bit unsigned integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ushort ReadUInt16() => ReadPrimitive<ushort>();

		/*
		===============
		ReadUInt32
		===============
		*/
		/// <summary>
		/// Reads a 32 bit unsigned integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public uint ReadUInt32() => ReadPrimitive<uint>();

		/*
		===============
		ReadUInt64
		===============
		*/
		/// <summary>
		/// Reads a 64 bit unsigned integer from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ulong ReadUInt64() => ReadPrimitive<ulong>();

		/*
		===============
		ReadFloat
		===============
		*/
		/// <summary>
		/// Reads a 32 bit floating point value from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float ReadFloat() => ReadPrimitive<float>();

		/*
		===============
		ReadDouble
		===============
		*/
		/// <summary>
		/// Reads a 64 bit floating point value from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public double ReadDouble() => ReadPrimitive<double>();

		/*
		===============
		ReadBoolean
		===============
		*/
		/// <summary>
		/// Reads an 8 bit boolean value from the save file stream.
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool ReadBoolean() => ReadPrimitive<bool>();

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
		public int Read7BitEncodedInt() {
			int value = 0;
			int shift = 0;
			byte b;

			do {
				b = ReadUInt8();
				value |= ( b & 0x7F ) << shift;
				shift += 7;
				if ( shift > 35 ) {
					throw new FormatException( "Invalid 7-bit encoded integer formatting in save file." );
				}
			} while ( ( b & 0x80 ) != 0 );

			return value;
		}

		/*
		===============
		ReadString
		===============
		*/
		/// <summary>
		/// Reads a string from the save file stream.
		/// </summary>
		/// <returns></returns>
		public string ReadString() {
			int byteCount = Read7BitEncodedInt();
			CheckRead( byteCount );
			string value = Encoding.UTF8.GetString( _buffer, Position, byteCount );
			_position += value.Length;
			return value;
		}

		/*
		===============
		ReadBuffer
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		public void ReadExactly( byte[] buffer, int offset, int length ) {
			ArgumentNullException.ThrowIfNull( buffer );
			ArgumentOutOfRangeException.ThrowIfLessThan( offset, 0 );
			ArgumentOutOfRangeException.ThrowIfLessThan( length, 0 );
			ArgumentOutOfRangeException.ThrowIfGreaterThan( offset + length, buffer.Length );

			CheckRead( length - offset );
			System.Buffer.BlockCopy( _buffer, Position, buffer, offset, length );
			_position += length;
		}

		/*
		===============
		CheckRead
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="size"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private readonly void CheckRead( int size ) {
			if ( Position + size >= _buffer.Length ) {
				throw new System.IO.EndOfStreamException( "End of save reader stream hit!" );
			}
		}

		/*
		===============
		ReadPrimitive
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private T ReadPrimitive<T>() where T : unmanaged {
			int sizeOfData = Marshal.SizeOf<T>();

			CheckRead( sizeOfData );
			T value = Unsafe.ReadUnaligned<T>( ref _buffer[ Position ] );
			_position += sizeOfData;
			return value;
		}
	};
};