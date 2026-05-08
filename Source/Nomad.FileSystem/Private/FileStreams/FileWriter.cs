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

namespace Nomad.FileSystem.Private.FileStreams
{
	/// <summary>
	/// A unified writer that can write to either a text (StreamWriter) or binary (BinaryWriter) stream.
	/// </summary>
	internal readonly struct FileWriter
	{
		private readonly StreamWriter _textWriter;
		private readonly BinaryWriter _binaryWriter;

		/// <summary>
		/// Initializes a new instance for text writing.
		/// </summary>
		/// <param name="writer">The underlying StreamWriter.</param>
		public FileWriter( StreamWriter writer )
		{
			_textWriter = writer;
			_binaryWriter = null;
		}

		/// <summary>
		/// Initializes a new instance for binary writing.
		/// </summary>
		/// <param name="writer">The underlying BinaryWriter.</param>
		public FileWriter( BinaryWriter writer )
		{
			_textWriter = null;
			_binaryWriter = writer;
		}

		public IDisposable GetStream()
		{
			return _textWriter != null ? _textWriter : _binaryWriter;
		}

		public void Write( byte value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( sbyte value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( char value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( short value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( ushort value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( int value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( uint value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( long value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( ulong value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( float value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( double value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( decimal value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( bool value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		public void Write( string value )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( value );
			} else {
				_binaryWriter.Write( value );
			}
		}

		// -----------------------------------------------------------------
		// Write methods for arrays / spans (if needed)
		// -----------------------------------------------------------------

		public void Write( byte[] buffer )
		{
			if ( _textWriter != null ) {
				// StreamWriter doesn't have a Write(byte[]) overload,
				// so we write as characters (maybe not ideal, but demonstrates the pattern).
				// In practice you'd likely avoid this or use a different approach.
				_textWriter.Write( System.Text.Encoding.UTF8.GetString( buffer ) );
			} else {
				_binaryWriter.Write( buffer );
			}
		}

		public void Write( ReadOnlySpan<byte> buffer )
		{
			if ( _textWriter != null ) {
				// StreamWriter doesn't have a Write(byte[]) overload,
				// so we write as characters (maybe not ideal, but demonstrates the pattern).
				// In practice you'd likely avoid this or use a different approach.
				_textWriter.Write( System.Text.Encoding.UTF8.GetString( buffer ) );
			} else {
				_binaryWriter.Write( buffer );
			}
		}

		public void Write( char[] chars )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( chars );
			} else {
				_binaryWriter.Write( chars );
			}
		}

		public void Write( ReadOnlySpan<char> chars )
		{
			if ( _textWriter != null ) {
				_textWriter.Write( chars );
			} else {
				_binaryWriter.Write( chars );
			}
		}

		public void Flush()
		{
			if ( _textWriter != null ) {
				_textWriter.Flush();
			} else {
				_binaryWriter.Flush();
			}
		}

		public async ValueTask FlushAsync( CancellationToken ct = default )
		{
			if ( _textWriter != null ) {
				await _textWriter.FlushAsync();
			} else {
				_binaryWriter.Flush();
			}
		}
	}
}
