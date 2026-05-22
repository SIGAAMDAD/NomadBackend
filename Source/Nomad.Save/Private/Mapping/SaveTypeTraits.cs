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
using System.Linq;
using System.Reflection;
using Nomad.Save.Extensions.Attributes;

namespace Nomad.Save.Private.Mapping
{
	/*
	===================================================================================

	SaveTypeTraits

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class SaveTypeTraits
	{
		public static bool IsScalar( Type type )
		{
			type = Nullable.GetUnderlyingType( type ) ?? type;

			return type == typeof( bool )
				|| type == typeof( byte )
				|| type == typeof( sbyte )
				|| type == typeof( short )
				|| type == typeof( ushort )
				|| type == typeof( int )
				|| type == typeof( uint )
				|| type == typeof( long )
				|| type == typeof( ulong )
				|| type == typeof( float )
				|| type == typeof( double )
				|| type == typeof( string )
				|| type.IsEnum;
		}

		public static bool IsNullable( Type type, out Type? innerType )
		{
			innerType = Nullable.GetUnderlyingType( type );
			return innerType != null;
		}

		public static bool IsSaveObject( Type type )
		{
			type = Nullable.GetUnderlyingType( type ) ?? type;
			return type.GetCustomAttribute<SaveObjectAttribute>() != null;
		}

		public static bool IsCollectionLike( Type type )
		{
			return TryGetCollectionElementType( type, out _ )
				|| TryGetDictionaryTypes( type, out _, out _ );
		}

		public static bool TryGetCollectionElementType( Type type, out Type? elementType )
		{
			elementType = null;

			if ( type == typeof( string ) ) {
				return false;
			}

			if ( type.IsArray ) {
				elementType = type.GetElementType();
				return elementType != null;
			}

			Type? enumerableType = type
				.GetInterfaces()
				.Concat( new[] { type } )
				.FirstOrDefault(
					candidate =>
						candidate.IsGenericType &&
						candidate.GetGenericTypeDefinition() == typeof( IEnumerable<> )
				);

			if ( enumerableType == null ) {
				return false;
			}

			elementType = enumerableType.GetGenericArguments()[0];

			// Dictionaries are handled separately.
			if ( TryGetDictionaryTypes( type, out _, out _ ) ) {
				elementType = null;
				return false;
			}

			return true;
		}

		public static bool TryGetDictionaryTypes( Type type, out Type? keyType, out Type? valueType )
		{
			keyType = null;
			valueType = null;

			Type? dictionaryType = type
				.GetInterfaces()
				.Concat( new[] { type } )
				.FirstOrDefault(
					candidate =>
						candidate.IsGenericType &&
						(
							candidate.GetGenericTypeDefinition() == typeof( IDictionary<,> ) ||
							candidate.GetGenericTypeDefinition() == typeof( IReadOnlyDictionary<,> )
						)
				);

			if ( dictionaryType == null ) {
				return false;
			}

			Type[] args = dictionaryType.GetGenericArguments();

			keyType = args[0];
			valueType = args[1];

			return true;
		}
	};
};
