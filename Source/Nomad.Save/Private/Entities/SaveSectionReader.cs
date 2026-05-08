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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Save.Exceptions;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.ValueObjects;

namespace Nomad.Save.Private.Entities
{
	/*
	===================================================================================

	SaveSectionReader

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveSectionReader : ISaveSectionReader
	{
		/// <summary>
		/// The name of the section.
		/// </summary>
		public string Name => _isDisposed ? throw new ObjectDisposedException( nameof( SaveSectionReader ) ) : _name;
		private readonly string _name;

		/// <summary>
		/// The number of fields in the section.
		/// </summary>
		public int FieldCount => _isDisposed ? throw new ObjectDisposedException( nameof( SaveSectionReader ) ) : _fields.Count;

		private readonly Dictionary<string, SaveField> _fields;
		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;

		/*
		===============
		SaveSectionReader
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="config"></param>
		/// <param name="index"></param>
		/// <param name="reader"></param>
		/// <param name="category"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="DuplicateFieldException"></exception>
		public SaveSectionReader( SaveConfig config, int index, IMemoryReadStream reader, ILoggerCategory category )
		{
			// TODO: implement a field pool or something of that nature.
			_fields = new Dictionary<string, SaveField>();
			_category = category ?? throw new ArgumentNullException( nameof( category ) );

			SectionHeader header = SectionHeader.Load( index, reader );

			if ( config.LogSerializationTree ) {
				_category.PrintLine( $"\t[Section] (NAME) {header.Name}" );
			}

			int fieldCount = header.FieldCount;
			_name = header.Name;

			_fields.EnsureCapacity( fieldCount );
			for ( int i = 0; i < fieldCount; i++ ) {
				SaveField field = SaveField.Read( Name, i, reader );

				if ( config.LogSerializationTree ) {
					_category.PrintLine( $"\t\t[Field] (NAME) {field.Name}, (TYPE) {field.Type}, (VALUE) {field.Value}" );
				}
				if ( _fields.ContainsKey( field.Name ) ) {
					throw new DuplicateFieldException( _name, field.Name );
				}

				_fields[field.Name] = field;
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
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_fields.Clear();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		GetField
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException"></exception>
		public T GetField<T>( string fieldName )
			where T : unmanaged
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNullOrEmpty( fieldName );

			T value = default;
			if ( _fields.TryGetValue( fieldName, out var field ) ) {
				if ( Any.GetType<T>() != field.Type ) {
					throw new InvalidCastException( "Field type found in file does not match the requested type" );
				}
				value = field.Value.GetPrimitiveValue<T>();
			}
			return value;
		}

		/*
		===============
		GetString
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException"></exception>
		public string GetString( string fieldName )
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
			ArgumentGuard.ThrowIfNullOrEmpty( fieldName );

			string value = string.Empty;
			if ( _fields.TryGetValue( fieldName, out var field ) ) {
				if ( field.Type != AnyType.String ) {
					throw new InvalidCastException( "Field type found in file does not match the requested type" );
				}
				value = field.Value.GetReferenceValue<string>() ?? string.Empty;
			}
			return value;
		}
	};
};
