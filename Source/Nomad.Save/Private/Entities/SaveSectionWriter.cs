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
using System.Collections.Generic;
using Nomad.Core.FileSystem.Streams;
using Nomad.Save.Interfaces;
using Nomad.Save.Exceptions;
using Nomad.Save.Private.ValueObjects;
using Nomad.Core.Util;
using Nomad.Core.Logger;
using Nomad.Core.Compatibility.Guards;

namespace Nomad.Save.Private.Entities
{
	/*
	===================================================================================

	SaveSectionWriter

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveSectionWriter : ISaveSectionWriter
	{
		/// <summary>
		/// This section's name.
		/// </summary>
		public string Name => _isDisposed ? throw new ObjectDisposedException( nameof( SaveSectionWriter ) ) : _name;
		private readonly string _name;

		/// <summary>
		/// The number of fields in this section.
		/// </summary>
		public int FieldCount => _isDisposed ? throw new ObjectDisposedException( nameof( SaveSectionWriter ) ) : _fields.Count;

		private readonly Dictionary<string, SaveField> _fields;

		private readonly IMemoryFileWriteStream _writer;
		private readonly SaveConfig _config;
		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;
		private readonly object _disposeLock = new();

		/*
		===============
		SaveSectionWriter
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="config"></param>
		/// <param name="category"></param>
		/// <param name="name"></param>
		/// <param name="writer"></param>
		public SaveSectionWriter( SaveConfig config, ILoggerCategory category, string name, IMemoryFileWriteStream writer )
		{
			_name = name ?? throw new ArgumentException( "Section name cannot be null or empty" );
			_fields = new Dictionary<string, SaveField>();
			_writer = writer ?? throw new ArgumentNullException( nameof( writer ) );
			_config = config ?? throw new ArgumentNullException( nameof( config ) );
			_category = category ?? throw new ArgumentNullException( nameof( category ) );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			lock ( _disposeLock ) {
				if ( !_isDisposed ) {
					long offset = _writer.Position;
					{
						var header = new SectionHeader( _name, 0, FieldCount, Checksum.Empty );
						header.Save( _writer );
					}

					long start = offset + SectionHeader.HEADER_CHECKSUM_OFFSET;
					foreach ( var field in _fields ) {
						SaveField.Write( _name, field.Value, _writer );
					}

					long position = _writer.Position;
					long length = position - start;

					_writer.Seek( offset, System.IO.SeekOrigin.Begin );
					{
						var header = new SectionHeader( _name, (int)length, FieldCount, Checksum.Compute( _writer.Buffer!.GetSlice( (int)start, (int)length ) ) );
						header.Save( _writer );

						if ( _config.LogSerializationTree ) {
							_category.PrintLine( "Finalized section data:" );
							_category.PrintLine( $"\tName: {header.Name}" );
							_category.PrintLine( $"\tByteLength: {header.ByteLength}" );
							_category.PrintLine( $"\tFieldCount: {header.FieldCount}" );
							_category.PrintLine( $"\tChecksum64: {header.Checksum}" );
						}
					}
					_writer.Seek( position, System.IO.SeekOrigin.Begin );
					_fields.Clear();

					_isDisposed = true;
				}
			}
			GC.SuppressFinalize( this );
		}

		/*
		===============
		AddField
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fieldId"></param>
		/// <param name="value"></param>
		public void AddField<T>( string fieldId, T value )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNullOrEmpty( fieldId );

			if ( _fields.ContainsKey( fieldId ) ) {
				throw new DuplicateFieldException( _name, fieldId );
			}

			var type = Any.GetType<T>();
			var fieldValue = Any.From( value );

			_fields[fieldId] = new SaveField(
				fieldId,
				type,
				fieldValue
			);
			if ( _config.LogSerializationTree ) {
				_category.PrintLine( $"\t\t[Field] (NAME) {fieldId}, (TYPE) {type}, (VALUE) {fieldValue}" );
			}
		}

		/*
		===============
		HasField
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fieldId"></param>
		/// <returns></returns>
		public bool HasField<T>( string fieldId )
		{
			return _fields.TryGetValue( fieldId, out var field ) && field.Type == Any.GetType<T>();
		}
	};
};
