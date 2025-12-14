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

using NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects;
using NomadCore.Systems.SaveSystem.Errors;
using NomadCore.Systems.SaveSystem.Infrastructure.Serialization.FieldSerializers;
using NomadCore.Systems.SaveSystem.Infrastructure.Serialization.Streams;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Fields {
	/*
	===================================================================================
	
	SaveField
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal readonly struct SaveField( string name, FieldType type, FieldValue value ) {
		public static readonly SaveField Empty = new SaveField();

		private const int MAX_FIELD_NAME_LENGTH = 256;

		public readonly string Name => _name;
		private readonly string _name = name;

		public readonly FieldType Type => _type;
		private readonly FieldType _type = type;

		public readonly FieldValue Value => _value;
		private readonly FieldValue _value = value;

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="section"></param>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="stream"></param>
		public static void Write<T>( string section, string name, T value, SaveStreamWriter stream ) {
			stream.Write( name );
			stream.Write( FieldValue.GetFieldType<T>() );
			FieldSerializerRegistry.GetSerializer<T>().Serialize( stream, value );
		}

		/*
		===============
		Read
		===============
		*/
		/// <summary>
		/// Loads a SaveField object into memory from a save file stream.
		/// </summary>
		/// <param name="section">The section the field is bound to.</param>
		/// <param name="index">The index of the field.</param>
		/// <param name="stream">The file stream to read from.</param>
		/// <returns>A new SaveField object.</returns>
		/// <exception cref="FieldCorruptException">Thrown if the field's data is invalid.</exception>
		public static SaveField Read( string section, int index, SaveStreamReader stream ) {
			string name = stream.ReadString();
			if ( name.Length <= 0 || name.Length > MAX_FIELD_NAME_LENGTH ) {
				throw new FieldCorruptException( section, index, stream.Position, $"Field name length corrupted (0 or string overflow, {name.Length} bytes)" );
			}

			FieldType type = stream.Read<FieldType>();
			if ( type < FieldType.Int8 || type >= FieldType.Count ) {
				throw new FieldCorruptException( section, index, stream.Position, $"Field type '{type}' isn't valid" );
			}

			FieldValue value = FieldSerializerRegistry.GetSerializer( FieldValue.GetFieldType( type ) ).Deserialize( stream );

			return new SaveField( name, type, value );
		}
	};
};