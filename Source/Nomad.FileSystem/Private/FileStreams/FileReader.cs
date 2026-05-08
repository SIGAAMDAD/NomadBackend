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

namespace Nomad.FileSystem.Private.FileStreams
{
	/// <summary>
	/// A unified Reader that can Read to either a text (StreamReader) or binary (BinaryReader) stream.
	/// </summary>
	internal readonly struct FileReader
	{
		public bool IsText => _textReader != null;
		public long Position {
			get => _textReader != null ? _textReader.BaseStream.Position : _binaryReader.BaseStream.Position;
			set {
				if ( _textReader != null ) {
					_textReader.BaseStream.Position = value;
				} else {
					_binaryReader.BaseStream.Position = value;
				}
			}
		}

		private readonly StreamReader _textReader;
		private readonly BinaryReader _binaryReader;

		/// <summary>
		/// Initializes a new instance for text writing.
		/// </summary>
		/// <param name="reader">The underlying StreamReader.</param>
		public FileReader( StreamReader reader )
		{
			_textReader = reader;
			_binaryReader = null;
		}

		/// <summary>
		/// Initializes a new instance for binary writing.
		/// </summary>
		/// <param name="reader">The underlying BinaryReader.</param>
		public FileReader( BinaryReader reader )
		{
			_textReader = null;
			_binaryReader = reader;
		}

		public IDisposable GetStream()
		{
			return _textReader != null ? _textReader : _binaryReader;
		}

		public byte ReadByte()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadByte();
		}

		public sbyte ReadSByte()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadSByte();
		}

		public char ReadChar()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadChar();
		}

		public short ReadShort()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadInt16();
		}

		public ushort ReadUShort()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadUInt16();
		}

		public int ReadInt()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadInt32();
		}

		public uint ReadUInt()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadUInt32();
		}

		public long ReadLong()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadInt64();
		}

		public ulong ReadULong()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadUInt64();
		}

		public float ReadFloat()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadSingle();
		}

		public double ReadDouble()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadDouble();
		}

		public decimal ReadDecimal()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadDecimal();
		}

		public bool ReadBoolean()
		{
			if ( _textReader != null ) {
				throw new NotSupportedException();
			}
			return _binaryReader.ReadBoolean();
		}

		public string ReadString()
		{
			if ( _textReader != null ) {
				return _textReader.ReadLine();
			}
			return _binaryReader.ReadString();
		}

		public byte[] ReadToEnd()
		{
			long remaining;
			byte[] buffer;

			if ( _textReader != null ) {
				string str = _textReader.ReadToEnd();
				buffer = new byte[str.Length];
				Buffer.BlockCopy( str.AsSpan().ToArray(), 0, buffer, 0, buffer.Length );
				return buffer;
			}

			remaining = _binaryReader.BaseStream.Length - _binaryReader.BaseStream.Position;
			if ( remaining > int.MaxValue ) {
				throw new InvalidOperationException( "File is too large to read into a single array." );
			}

			buffer = new byte[remaining];
			int bytesRead = _binaryReader.Read( buffer, 0, (int)remaining );
			if ( bytesRead != remaining ) {
				throw new IOException( $"FileStream failed to read exactly {remaining} bytes!" );
			}
			return buffer;
		}
	};
};
