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

using NomadCore.Abstractions.Services;
using NomadCore.Infrastructure;
using NomadCore.Systems.SaveSystem.Enums;
using NomadCore.Systems.SaveSystem.Errors;
using NomadCore.Systems.SaveSystem.Infrastructure.Fields;
using NomadCore.Systems.SaveSystem.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Sections {
	/*
	===================================================================================
	
	SectionReader
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class SectionReader : ISectionReader {
		public string Name => _name;
		private readonly string _name;

		public int FieldCount => _fieldCount;
		private readonly int _fieldCount;

		public IReadOnlyDictionary<string, SaveField> Fields => _fields;
		private readonly ConcurrentDictionary<string, SaveField> _fields = new ConcurrentDictionary<string, SaveField>();

		private readonly ILoggerService? Logger = ServiceRegistry.Get<ILoggerService>();

		public SaveField this[ string name ] => _fields[ name ];

		/*
		===============
		SectionReader
		===============
		*/
		public SectionReader( Streams.SaveReaderStream stream ) {
			SectionHeader header = SectionHeader.Load( stream );

			_name = header.Name;
			_fieldCount = header.FieldCount;

			LoadFields( stream, in header );
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T? GetField<T>( string? name ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			SaveField? field = LoadField( name, FieldValue.GetFieldType<T>() );
			if ( field != null && field.Value is T value ) {
				return value;
			}

			return default;
		}

		/*
		===============
		TryGetField
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns>True if the field exists, false if not found in cache.</returns>
		public bool TryGetField<T>( string? name, out T? value ) {
			ArgumentException.ThrowIfNullOrEmpty( name );

			FieldType type = FieldValue.GetFieldType<T>();
			SaveField? field = LoadField( name, type );
			if ( field.HasValue && field.Value.Type == type ) {
				value = field.Value.Value.GetValue<T>();
				return true;
			}
			value = default;
			return false;
		}

		/*
		===============
		LoadSByte
		===============
		*/
		/// <summary>
		/// Loads an sbyte from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public sbyte LoadSByte( string? name ) => GetField<sbyte>( name );

		/*
		===============
		LoadShort
		===============
		*/
		/// <summary>
		/// Loads a short from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public short LoadShort( string? name ) => GetField<short>( name );

		/*
		===============
		LoadInt
		===============
		*/
		/// <summary>
		/// Loads an int from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public int LoadInt( string? name ) => GetField<int>( name );

		/*
		===============
		LoadLong
		===============
		*/
		/// <summary>
		/// Loads a long from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public long LoadLong( string? name ) => GetField<long>( name );

		/*
		===============
		LoadByte
		===============
		*/
		/// <summary>
		/// Loads a byte from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public byte LoadByte( string? name ) => GetField<byte>( name );

		/*
		===============
		LoadUShort
		===============
		*/
		/// <summary>
		/// Loads a ushort from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ushort LoadUShort( string? name ) => GetField<ushort>( name );

		/*
		===============
		LoadUInt
		===============
		*/
		/// <summary>
		/// Loads a uint from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public uint LoadUInt( string? name ) => GetField<uint>( name );

		/*
		===============
		LoadULong
		===============
		*/
		/// <summary>
		/// Loads a ulong from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ulong LoadULong( string? name ) => GetField<ulong>( name );

		/*
		===============
		LoadFloat
		===============
		*/
		/// <summary>
		/// Loads a float from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float LoadFLoat( string? name ) => GetField<float>( name );

		/*
		===============
		LoadDouble
		===============
		*/
		/// <summary>
		/// Loads a double from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public double LoadDouble( string? name ) => GetField<double>( name );

		/*
		===============
		LoadString
		===============
		*/
		/// <summary>
		/// Loads a string from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public string LoadString( string? name ) => GetField<string>( name );

		/*
		===============
		LoadBoolean
		===============
		*/
		/// <summary>
		/// Loads a bool from the loaded save section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool LoadBoolean( string? name ) => GetField<bool>( name );

		/*
		===============
		LoadField
		===============
		*/
		/// <summary>
		/// Loads a field's value.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		/// <param name="type">The type of the field.</param>
		/// <returns>The loaded field, or empty if not found.</returns>
		/// <exception cref="InvalidCastException">Thrown if <paramref name="type"/> doesn't match what's found in the save data.</exception>
		private SaveField LoadField( string name, FieldType type ) {
			if ( Fields.TryGetValue( name, out SaveField field ) ) {
				// if the provided type and the loaded type are the same
				return field.Type != type ?
						throw new InvalidCastException( $"SaveField {name} from savefile isn't the same type as {type}" )
					:
						field;
			}

			// not the end of the world, just a missing field, so apply the default value
			Logger?.PrintError( $"...couldn't find save field {name}" );
			return new SaveField();
		}

		/*
		===============
		LoadSections
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="header"></param>
		/// <exception cref="FailedSectionLoadException">Throws if the section failed to load.</exception>
		private void LoadFields( in Streams.SaveReaderStream reader, in SectionHeader header ) {
			try {
				for ( int i = 0; i < header.FieldCount; i++ ) {
					var field = SaveField.Read( Name, i, reader );
					if ( !_fields.TryAdd( field.Name, field ) ) {
						Logger?.PrintError( $"Section.LoadFields: failed to add field {field.Name} to cache." );
						throw new FailedSectionLoadException( Name, null );
					}
				}
			} catch ( Exception e ) when ( e is not FailedSectionLoadException ) {
				throw new FailedSectionLoadException( Name, e );
			}
		}
	};
};