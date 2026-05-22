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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Nomad.Save.Extensions.Attributes;

namespace Nomad.Save.Private.Mapping
{
	/*
	===================================================================================

	SaveContractCache

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class SaveContractCache
	{
		private static readonly ConcurrentDictionary<Type, SaveTypeContract> _contracts = new ConcurrentDictionary<Type, SaveTypeContract>();

		public static SaveTypeContract Get( Type type )
		{
			return _contracts.GetOrAdd( type, Build );
		}

		private static SaveTypeContract Build( Type type )
		{
			SaveObjectAttribute? objectAttribute = type.GetCustomAttribute<SaveObjectAttribute>();
			if ( objectAttribute == null ) {
				throw new InvalidOperationException(
					$"Type '{type.FullName}' is not marked with [SaveObject]."
				);
			}

			var members = new List<SaveMemberContract>();

			foreach ( MemberInfo member in GetCandidateMembers( type ) ) {
				if ( member.GetCustomAttribute<SaveIgnoreAttribute>() != null ) {
					continue;
				}

				SaveFieldAttribute? attribute =
					member.GetCustomAttribute<SaveCollectionAttribute>() ??
					member.GetCustomAttribute<SaveFieldAttribute>();

				if ( attribute == null ) {
					continue;
				}

				Type memberType = GetMemberType( member );

				members.Add(
					new SaveMemberContract(
						attribute.Name ?? member.Name,
						memberType,
						member,
						attribute,
						CreateGetter( member ),
						CreateSetter( member )
					)
				);
			}

			string sectionName = objectAttribute.Name;
			if ( string.IsNullOrWhiteSpace( sectionName ) ) {
				sectionName = type.FullName?.Replace( '+', '.' ) ?? type.Name;
			}

			return new SaveTypeContract(
				type,
				sectionName,
				objectAttribute.Version,
				members
			);
		}

		private static IEnumerable<MemberInfo> GetCandidateMembers( Type type )
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

			foreach ( PropertyInfo property in type.GetProperties( flags ) ) {
				if ( property.GetIndexParameters().Length == 0 && property.GetMethod != null ) {
					yield return property;
				}
			}

			foreach ( FieldInfo field in type.GetFields( flags ) ) {
				yield return field;
			}
		}

		private static Type GetMemberType( MemberInfo member )
		{
			return member switch {
				PropertyInfo property => property.PropertyType,
				FieldInfo field => field.FieldType,
				_ => throw new NotSupportedException( $"Unsupported member '{member.Name}'." )
			};
		}

		private static Func<object, object?> CreateGetter( MemberInfo member )
		{
			return member switch {
				PropertyInfo property => instance => property.GetValue( instance ),
				FieldInfo field => instance => field.GetValue( instance ),
				_ => throw new NotSupportedException()
			};
		}

		private static Action<object, object?>? CreateSetter( MemberInfo member )
		{
			return member switch {
				PropertyInfo property when property.SetMethod != null =>
					( instance, value ) => property.SetValue( instance, value ),

				FieldInfo field when !field.IsInitOnly =>
					( instance, value ) => field.SetValue( instance, value ),

				_ => null
			};
		}
	};
};
