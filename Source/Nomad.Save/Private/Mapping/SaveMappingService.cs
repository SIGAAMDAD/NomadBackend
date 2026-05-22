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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Nomad.Core.Compatibility.Guards;
using Nomad.Save.Extensions.Attributes;
using Nomad.Save.Interfaces;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Private.Mapping
{
	/*
	===================================================================================

	SaveMappingService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveMappingService : ISaveMappingService
	{
		private const string MAPPING_PREFIX = "__NomadMapping";
		private readonly SaveMappingOptions _options;

		public SaveMappingService( SaveMappingOptions? options = null )
		{
			_options = options ?? new SaveMappingOptions();
		}

		public void SaveObject<T>( ISaveWriterService writer, T value )
			where T : notnull
		{
			ArgumentGuard.ThrowIfNull( writer );
			ArgumentGuard.ThrowIfNull( value );

			Type runtimeType = value.GetType();
			SaveTypeContract contract = SaveContractCache.Get( runtimeType );

			using ISaveSectionWriter section = writer.AddSection( contract.SectionName );

			var context = new SaveWriteContext( section );

			context.WriteScalar( $"{MAPPING_PREFIX}.SchemaVersion", contract.Version, typeof( int ) );
			context.WriteScalar( $"{MAPPING_PREFIX}.TypeName", contract.Type.FullName ?? contract.Type.Name, typeof( string ) );

			WriteObjectMembers(
				context,
				string.Empty,
				value,
				contract,
				depth: 0
			);

			context.WriteManifest();
		}

		public bool LoadObject<T>( ISaveReaderService reader, T value )
			where T : notnull
		{
			ArgumentGuard.ThrowIfNull( reader );
			ArgumentGuard.ThrowIfNull( value );

			Type runtimeType = value.GetType();
			SaveTypeContract contract = SaveContractCache.Get( runtimeType );

			ISaveSectionReader? section = reader.FindSection( contract.SectionName );
			if ( section == null ) {
				return false;
			}

			var context = new SaveReadContext( section );

			ReadObjectMembers(
				context,
				string.Empty,
				value,
				contract,
				depth: 0
			);

			return true;
		}

		private void WriteObjectMembers(
			SaveWriteContext context,
			string prefix,
			object instance,
			SaveTypeContract contract,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			foreach ( SaveMemberContract member in contract.Members ) {
				object? value = member.Getter( instance );
				string path = SavePath.Combine( prefix, member.SaveName );

				if ( member.Attribute is SaveCollectionAttribute collectionAttribute ) {
					WriteCollectionLike(
						context,
						path,
						value,
						member.MemberType,
						depth + 1,
						collectionAttribute.MaxCount
					);
					continue;
				}

				if ( SaveTypeTraits.IsCollectionLike( member.MemberType ) ) {
					throw new InvalidOperationException(
						$"Member '{member.Member.Name}' is a collection-like type. Use [SaveCollection], not [SaveField]."
					);
				}

				WriteValue(
					context,
					path,
					value,
					member.MemberType,
					depth + 1,
					_options.MaxCollectionCount
				);
			}
		}

		private void ReadObjectMembers(
			SaveReadContext context,
			string prefix,
			object instance,
			SaveTypeContract contract,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			foreach ( SaveMemberContract member in contract.Members ) {
				string path = SavePath.Combine( prefix, member.SaveName );

				if ( member.Attribute is SaveCollectionAttribute ) {
					ReadCollectionLikeMember( context, path, instance, member, depth + 1 );
					continue;
				}

				if ( SaveTypeTraits.IsCollectionLike( member.MemberType ) ) {
					throw new InvalidOperationException(
						$"Member '{member.Member.Name}' is a collection-like type. Use [SaveCollection], not [SaveField]."
					);
				}

				if ( !HasRequiredValue( context, path, member ) ) {
					continue;
				}

				object? value = ReadValue(
					context,
					path,
					member.MemberType,
					depth + 1
				);

				SetMemberValue( instance, member, value );
			}
		}

		private void WriteValue(
			SaveWriteContext context,
			string path,
			object? value,
			Type declaredType,
			int depth,
			int maxCollectionCount
		)
		{
			ThrowIfTooDeep( depth );

			if ( SaveTypeTraits.IsNullable( declaredType, out Type? nullableInnerType ) ) {
				bool hasValue = value != null;

				context.WriteScalar( $"{path}.HasValue", hasValue, typeof( bool ) );

				if ( hasValue ) {
					context.WriteScalar( $"{path}.Value", value, nullableInnerType! );
				}

				return;
			}

			if ( SaveTypeTraits.IsScalar( declaredType ) ) {
				context.WriteScalar( path, value, declaredType );
				return;
			}

			if ( SaveTypeTraits.TryGetDictionaryTypes( declaredType, out Type? keyType, out Type? valueType ) ) {
				WriteDictionary(
					context,
					path,
					value,
					keyType!,
					valueType!,
					depth + 1,
					maxCollectionCount
				);
				return;
			}

			if ( SaveTypeTraits.TryGetCollectionElementType( declaredType, out Type? elementType ) ) {
				WriteCollection(
					context,
					path,
					value,
					elementType!,
					depth + 1,
					maxCollectionCount
				);
				return;
			}

			if ( SaveTypeTraits.IsSaveObject( declaredType ) ) {
				WriteNestedObject(
					context,
					path,
					value,
					declaredType,
					depth + 1
				);
				return;
			}

			throw new NotSupportedException(
				$"Type '{declaredType.FullName}' is not saveable at path '{path}'."
			);
		}

		private object? ReadValue(
			SaveReadContext context,
			string path,
			Type declaredType,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			if ( SaveTypeTraits.IsNullable( declaredType, out Type? nullableInnerType ) ) {
				string hasValuePath = $"{path}.HasValue";

				if ( !context.HasField( hasValuePath ) ) {
					return null;
				}

				bool hasValue = (bool)ScalarFieldIO.Read( context.Section, hasValuePath, typeof( bool ) );
				if ( !hasValue ) {
					return null;
				}

				return ScalarFieldIO.Read( context.Section, $"{path}.Value", nullableInnerType! );
			}

			if ( SaveTypeTraits.IsScalar( declaredType ) ) {
				return ScalarFieldIO.Read( context.Section, path, declaredType );
			}

			if ( SaveTypeTraits.TryGetDictionaryTypes( declaredType, out Type? keyType, out Type? valueType ) ) {
				return ReadDictionary(
					context,
					path,
					declaredType,
					keyType!,
					valueType!,
					depth + 1
				);
			}

			if ( SaveTypeTraits.TryGetCollectionElementType( declaredType, out Type? elementType ) ) {
				return ReadCollection(
					context,
					path,
					declaredType,
					elementType!,
					depth + 1
				);
			}

			if ( SaveTypeTraits.IsSaveObject( declaredType ) ) {
				return ReadNestedObject(
					context,
					path,
					declaredType,
					depth + 1
				);
			}

			throw new NotSupportedException(
				$"Type '{declaredType.FullName}' is not loadable at path '{path}'."
			);
		}

		private void WriteNestedObject(
			SaveWriteContext context,
			string path,
			object? value,
			Type declaredType,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			bool hasValue = value != null;
			context.WriteScalar( $"{path}.HasValue", hasValue, typeof( bool ) );

			if ( !hasValue ) {
				return;
			}

			SaveTypeContract contract = SaveContractCache.Get( declaredType );

			context.WriteScalar( $"{path}.{MAPPING_PREFIX}.SchemaVersion", contract.Version, typeof( int ) );

			WriteObjectMembers(
				context,
				path,
				value!,
				contract,
				depth + 1
			);
		}

		private object? ReadNestedObject(
			SaveReadContext context,
			string path,
			Type declaredType,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			string hasValuePath = $"{path}.HasValue";

			if ( context.HasField( hasValuePath ) ) {
				bool hasValue = (bool)ScalarFieldIO.Read( context.Section, hasValuePath, typeof( bool ) );
				if ( !hasValue ) {
					return null;
				}
			}

			object instance = Activator.CreateInstance( declaredType )
				?? throw new InvalidOperationException(
					$"Could not create instance of save object type '{declaredType.FullName}'."
				);

			SaveTypeContract contract = SaveContractCache.Get( declaredType );

			ReadObjectMembers(
				context,
				path,
				instance,
				contract,
				depth + 1
			);

			return instance;
		}

		private void WriteCollectionLike(
			SaveWriteContext context,
			string path,
			object? value,
			Type declaredType,
			int depth,
			int maxCount
		)
		{
			if ( SaveTypeTraits.TryGetDictionaryTypes( declaredType, out Type? keyType, out Type? valueType ) ) {
				WriteDictionary(
					context,
					path,
					value,
					keyType!,
					valueType!,
					depth,
					maxCount
				);
				return;
			}

			if ( SaveTypeTraits.TryGetCollectionElementType( declaredType, out Type? elementType ) ) {
				WriteCollection(
					context,
					path,
					value,
					elementType!,
					depth,
					maxCount
				);
				return;
			}

			throw new InvalidOperationException(
				$"Path '{path}' was marked [SaveCollection], but type '{declaredType.FullName}' is not a supported collection."
			);
		}

		private void WriteCollection(
			SaveWriteContext context,
			string path,
			object? value,
			Type elementType,
			int depth,
			int maxCount
		)
		{
			ThrowIfTooDeep( depth );

			if ( value == null ) {
				context.WriteScalar( $"{path}.Count", 0, typeof( int ) );
				return;
			}

			if ( value is not IEnumerable enumerable ) {
				throw new InvalidOperationException(
					$"Path '{path}' is not enumerable."
				);
			}

			int count = 0;

			foreach ( object? element in enumerable ) {
				if ( count >= maxCount ) {
					throw new InvalidOperationException(
						$"Collection '{path}' exceeded max count {maxCount}."
					);
				}

				WriteValue(
					context,
					SavePath.Index( path, count ),
					element,
					elementType,
					depth + 1,
					maxCount
				);

				count++;
			}

			context.WriteScalar( $"{path}.Count", count, typeof( int ) );
		}

		private object ReadCollection(
			SaveReadContext context,
			string path,
			Type collectionType,
			Type elementType,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			int count = context.HasField( $"{path}.Count" )
				? (int)ScalarFieldIO.Read( context.Section, $"{path}.Count", typeof( int ) )
				: 0;

			if ( collectionType.IsArray ) {
				Array array = Array.CreateInstance( elementType, count );

				for ( int i = 0; i < count; i++ ) {
					object? element = ReadValue(
						context,
						SavePath.Index( path, i ),
						elementType,
						depth + 1
					);

					array.SetValue( element, i );
				}

				return array;
			}

			object collection = CreateCollectionInstance( collectionType, elementType );
			AddCollectionElements( context, path, collection, elementType, count, depth + 1 );

			return collection;
		}

		private void ReadCollectionLikeMember(
			SaveReadContext context,
			string path,
			object instance,
			SaveMemberContract member,
			int depth
		)
		{
			if ( !context.HasField( $"{path}.Count" ) ) {
				if ( member.Attribute.Required ) {
					throw new InvalidOperationException(
						$"Required save collection '{path}' is missing."
					);
				}

				return;
			}

			object? currentValue = member.Getter( instance );

			object loadedValue = ReadValue(
				context,
				path,
				member.MemberType,
				depth
			) ?? throw new InvalidOperationException(
				$"Could not load collection member '{member.Member.Name}'."
			);

			if ( member.MemberType.IsArray ) {
				SetMemberValue( instance, member, loadedValue );
				return;
			}

			if ( currentValue != null && !ReferenceEquals( currentValue, loadedValue ) ) {
				ClearCollectionLike( currentValue );
				CopyCollectionLike( loadedValue, currentValue, member.MemberType );
				return;
			}

			SetMemberValue( instance, member, loadedValue );
		}

		private void WriteDictionary(
			SaveWriteContext context,
			string path,
			object? value,
			Type keyType,
			Type valueType,
			int depth,
			int maxCount
		)
		{
			ThrowIfTooDeep( depth );

			if ( value == null ) {
				context.WriteScalar( $"{path}.Count", 0, typeof( int ) );
				return;
			}

			if ( value is not IEnumerable enumerable ) {
				throw new InvalidOperationException(
					$"Dictionary path '{path}' is not enumerable."
				);
			}

			int count = 0;

			foreach ( object entry in enumerable ) {
				if ( count >= maxCount ) {
					throw new InvalidOperationException(
						$"Dictionary '{path}' exceeded max count {maxCount}."
					);
				}

				object? key = entry.GetType().GetProperty( "Key" )?.GetValue( entry );
				object? entryValue = entry.GetType().GetProperty( "Value" )?.GetValue( entry );

				string entryPath = SavePath.Index( path, count );

				WriteValue(
					context,
					$"{entryPath}.Key",
					key,
					keyType,
					depth + 1,
					maxCount
				);

				WriteValue(
					context,
					$"{entryPath}.Value",
					entryValue,
					valueType,
					depth + 1,
					maxCount
				);

				count++;
			}

			context.WriteScalar( $"{path}.Count", count, typeof( int ) );
		}

		private object ReadDictionary(
			SaveReadContext context,
			string path,
			Type dictionaryType,
			Type keyType,
			Type valueType,
			int depth
		)
		{
			ThrowIfTooDeep( depth );

			int count = context.HasField( $"{path}.Count" )
				? (int)ScalarFieldIO.Read( context.Section, $"{path}.Count", typeof( int ) )
				: 0;

			object dictionary = CreateDictionaryInstance( dictionaryType, keyType, valueType );

			MethodInfo addMethod = dictionary.GetType().GetMethod( "Add", new[] { keyType, valueType } )
				?? throw new InvalidOperationException(
					$"Dictionary type '{dictionary.GetType().FullName}' does not expose Add({keyType.Name}, {valueType.Name})."
				);

			for ( int i = 0; i < count; i++ ) {
				string entryPath = SavePath.Index( path, i );

				object? key = ReadValue(
					context,
					$"{entryPath}.Key",
					keyType,
					depth + 1
				);

				object? value = ReadValue(
					context,
					$"{entryPath}.Value",
					valueType,
					depth + 1
				);

				addMethod.Invoke( dictionary, new[] { key, value } );
			}

			return dictionary;
		}

		private static object CreateCollectionInstance( Type collectionType, Type elementType )
		{
			if ( !collectionType.IsInterface && collectionType.GetConstructor( Type.EmptyTypes ) != null ) {
				return Activator.CreateInstance( collectionType )
					?? throw new InvalidOperationException(
						$"Could not create collection type '{collectionType.FullName}'."
					);
			}

			Type listType = typeof( List<> ).MakeGenericType( elementType );

			return Activator.CreateInstance( listType )
				?? throw new InvalidOperationException(
					$"Could not create fallback list type '{listType.FullName}'."
				);
		}

		private static object CreateDictionaryInstance( Type dictionaryType, Type keyType, Type valueType )
		{
			if ( !dictionaryType.IsInterface && dictionaryType.GetConstructor( Type.EmptyTypes ) != null ) {
				return Activator.CreateInstance( dictionaryType )
					?? throw new InvalidOperationException(
						$"Could not create dictionary type '{dictionaryType.FullName}'."
					);
			}

			Type fallbackType = typeof( Dictionary<,> ).MakeGenericType( keyType, valueType );

			return Activator.CreateInstance( fallbackType )
				?? throw new InvalidOperationException(
					$"Could not create fallback dictionary type '{fallbackType.FullName}'."
				);
		}

		private void AddCollectionElements(
			SaveReadContext context,
			string path,
			object collection,
			Type elementType,
			int count,
			int depth
		)
		{
			MethodInfo addMethod = collection.GetType().GetMethod( "Add", new[] { elementType } )
				?? throw new InvalidOperationException(
					$"Collection type '{collection.GetType().FullName}' does not expose Add({elementType.Name})."
				);

			for ( int i = 0; i < count; i++ ) {
				object? element = ReadValue(
					context,
					SavePath.Index( path, i ),
					elementType,
					depth + 1
				);

				addMethod.Invoke( collection, new[] { element } );
			}
		}

		private static void ClearCollectionLike( object collection )
		{
			MethodInfo? clearMethod = collection.GetType().GetMethod( "Clear", Type.EmptyTypes );
			clearMethod?.Invoke( collection, null );
		}

		private static void CopyCollectionLike( object source, object destination, Type declaredType )
		{
			if ( SaveTypeTraits.TryGetDictionaryTypes( declaredType, out Type? keyType, out Type? valueType ) ) {
				MethodInfo addMethod = destination.GetType().GetMethod( "Add", new[] { keyType!, valueType! } )
					?? throw new InvalidOperationException(
						$"Dictionary type '{destination.GetType().FullName}' does not expose Add({keyType!.Name}, {valueType!.Name})."
					);

				foreach ( object entry in (IEnumerable)source ) {
					object? key = entry.GetType().GetProperty( "Key" )?.GetValue( entry );
					object? value = entry.GetType().GetProperty( "Value" )?.GetValue( entry );

					addMethod.Invoke( destination, new[] { key, value } );
				}

				return;
			}

			if ( SaveTypeTraits.TryGetCollectionElementType( declaredType, out Type? elementType ) ) {
				MethodInfo addMethod = destination.GetType().GetMethod( "Add", new[] { elementType! } )
					?? throw new InvalidOperationException(
						$"Collection type '{destination.GetType().FullName}' does not expose Add({elementType!.Name})."
					);

				foreach ( object? element in (IEnumerable)source ) {
					addMethod.Invoke( destination, new[] { element } );
				}
			}
		}

		private static bool HasRequiredValue(
			SaveReadContext context,
			string path,
			SaveMemberContract member
		)
		{
			if ( context.HasField( path ) ) {
				return true;
			}

			if ( context.HasField( $"{path}.HasValue" ) ) {
				return true;
			}

			if ( member.Attribute.Required ) {
				throw new InvalidOperationException(
					$"Required save field '{path}' is missing."
				);
			}

			return false;
		}

		private static void SetMemberValue(
			object instance,
			SaveMemberContract member,
			object? value
		)
		{
			if ( member.Setter == null ) {
				throw new InvalidOperationException(
					$"Save member '{member.Member.Name}' is read-only and cannot be assigned during load."
				);
			}

			member.Setter( instance, value );
		}

		private void ThrowIfTooDeep( int depth )
		{
			if ( depth > _options.MaxObjectDepth ) {
				throw new InvalidOperationException(
					$"Save mapping exceeded max object depth {_options.MaxObjectDepth}."
				);
			}
		}
	};
};
