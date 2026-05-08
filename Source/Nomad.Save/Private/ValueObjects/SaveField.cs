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

using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Util;
using Nomad.Save.Exceptions;
using Nomad.Save.Private.Serialization.FieldSerializers;

namespace Nomad.Save.Private.ValueObjects
{
	/*
	===================================================================================

	SaveField

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal readonly struct SaveField
	{
		public static readonly SaveField Empty = default;

		public readonly string Name;
		public readonly AnyType Type;
		public readonly Any Value;

		/*
		===============
		SaveField
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		public SaveField( string name, AnyType type, Any value )
		{
			Name = name;
			Type = type;
			Value = value;
		}

		/*
		===============
		Write
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="section"></param>
		/// <param name="field"></param>
		/// <param name="stream"></param>
		public static void Write( string section, SaveField field, IWriteStream stream )
		{
			stream.WriteString( field.Name );
			stream.WriteUInt8( (byte)field.Type );
			FieldSerializerRegistry.GetSerializer( Any.GetAnyType( field.Type ) ).Serialize( stream, field.Value );
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
		public static SaveField Read( string section, int index, IMemoryReadStream stream )
		{
			string name = stream.ReadString();
			if ( string.IsNullOrEmpty( name ) || name.Length > Constants.MAX_FIELD_NAME_LENGTH ) {
				throw new FieldCorruptException( section, index, stream.Position, $"Field name length corrupted (0 or string overflow, {name.Length} bytes)" );
			}

			AnyType type = (AnyType)stream.ReadUInt8();
			if ( type < AnyType.Boolean || type >= AnyType.Count ) {
				throw new FieldCorruptException( section, index, stream.Position, $"Field type '{type}' isn't valid" );
			}

			Any value = FieldSerializerRegistry.GetSerializer( Any.GetAnyType( type ) ).Deserialize( stream );

			return new SaveField( name, type, value );
		}
	};
};
