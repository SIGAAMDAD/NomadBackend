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

using NomadCore.Systems.SaveSystem.Enums;
using NomadCore.Systems.SaveSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Fields.Serializers {
	/*
	===================================================================================
	
	FieldSerializerRegistry
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal static class FieldSerializerRegistry {
		private static readonly Dictionary<Type, object> Serializers = new Dictionary<Type, object>();
		private static readonly Dictionary<FieldType, object> SerializerByFieldType = new Dictionary<FieldType, object>();

		static FieldSerializerRegistry() {
			Register( new ByteSerializer() );
			Register( new UShortSerializer() );
			Register( new UIntSerializer() );
			Register( new ULongSerializer() );
			Register( new SByteSerializer() );
			Register( new ShortSerializer() );
			Register( new IntSerializer() );
			Register( new LongSerializer() );
		}

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="serializer"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void Register<T>( IFieldSerializer<T> serializer ) {
			Serializers[ typeof( T ) ] = serializer;
			SerializerByFieldType[ serializer.FieldType ] = serializer;
		}

		/*
		===============
		GetSerializer
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IFieldSerializer<T> GetSerializer<T>() {
			if ( Serializers.TryGetValue( typeof( T ), out object? serializer ) ) {
				return (IFieldSerializer<T>)serializer;
			}
			throw new InvalidOperationException( $"No serializer for {typeof( T )}" );
		}

		/*
		===============
		GetSerializer
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IFieldSerializer GetSerializer( Type type ) {
			return (IFieldSerializer)Serializers[ type ];
		}
	};
};