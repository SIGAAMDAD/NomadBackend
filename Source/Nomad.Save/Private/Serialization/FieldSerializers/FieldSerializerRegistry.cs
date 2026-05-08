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
using System.Runtime.CompilerServices;
using Nomad.Core.Util;

namespace Nomad.Save.Private.Serialization.FieldSerializers
{
	/*
	===================================================================================

	FieldSerializerRegistry

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class FieldSerializerRegistry
	{
		private static readonly Dictionary<Type, IFieldSerializer> _serializers = new Dictionary<Type, IFieldSerializer>();
		private static readonly Dictionary<AnyType, IFieldSerializer> _serializerByFieldType = new Dictionary<AnyType, IFieldSerializer>();

		/*
		===============
		FieldSerializerRegistry
		===============
		*/
		/// <summary>
		///
		/// </summary>
		static FieldSerializerRegistry()
		{
			Register( new ByteSerializer() );
			Register( new UShortSerializer() );
			Register( new UIntSerializer() );
			Register( new ULongSerializer() );
			Register( new SByteSerializer() );
			Register( new ShortSerializer() );
			Register( new IntSerializer() );
			Register( new LongSerializer() );
			Register( new FloatSerializer() );
			Register( new DoubleSerializer() );
			Register( new StringSerializer() );
			Register( new BooleanSerializer() );
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
		public static void Register<T>( IFieldSerializer<T> serializer )
		{
			_serializers[typeof( T )] = serializer;
			_serializerByFieldType[serializer.FieldType] = serializer;
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
		public static IFieldSerializer<T> GetSerializer<T>()
		{
			return _serializers.TryGetValue( typeof( T ), out var serializer ) ? (IFieldSerializer<T>)serializer : throw new InvalidOperationException( $"No serializer for {typeof( T )}" );
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
		public static IFieldSerializer GetSerializer( Type type )
		{
			if ( !_serializers.TryGetValue( type, out var serializer ) ) {
				throw new InvalidOperationException( $"No serializer registered for type {type.Name}" );
			}
			return serializer;
		}
	};
};
