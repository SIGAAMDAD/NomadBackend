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
using Nomad.Save.Interfaces;

namespace Nomad.Save.Private.Mapping
{
	/*
	===================================================================================

	ScalarFieldIO

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal static class ScalarFieldIO
	{
		public static void Write( ISaveSectionWriter section, string path, object? value, Type declaredType )
		{
			Type type = Nullable.GetUnderlyingType( declaredType ) ?? declaredType;

			if ( value == null ) {
				if ( type == typeof( string ) ) {
					section.AddField( path, string.Empty );
					return;
				}

				throw new InvalidOperationException(
					$"Cannot write null to non-nullable field '{path}' of type '{declaredType.Name}'." );
			}

			if ( type.IsEnum ) {
				WriteEnum( section, path, value, type );
				return;
			}

			if ( type == typeof( bool ) ) {
				section.AddField( path, (bool)value );
			} else if ( type == typeof( byte ) ) {
				section.AddField( path, (byte)value );
			} else if ( type == typeof( sbyte ) ) {
				section.AddField( path, (sbyte)value );
			} else if ( type == typeof( short ) ) {
				section.AddField( path, (short)value );
			} else if ( type == typeof( ushort ) ) {
				section.AddField( path, (ushort)value );
			} else if ( type == typeof( int ) ) {
				section.AddField( path, (int)value );
			} else if ( type == typeof( uint ) ) {
				section.AddField( path, (uint)value );
			} else if ( type == typeof( long ) ) {
				section.AddField( path, (long)value );
			} else if ( type == typeof( ulong ) ) {
				section.AddField( path, (ulong)value );
			} else if ( type == typeof( float ) ) {
				section.AddField( path, (float)value );
			} else if ( type == typeof( double ) ) {
				section.AddField( path, (double)value );
			} else if ( type == typeof( string ) ) {
				section.AddField( path, (string)value );
			} else {
				throw new NotSupportedException(
					$"Field '{path}' has unsupported primitive type '{type.FullName}'." );
			}
		}

		public static object? Read( ISaveSectionReader section, string path, Type declaredType )
		{
			Type type = Nullable.GetUnderlyingType( declaredType ) ?? declaredType;

			if ( type.IsEnum ) {
				return ReadEnum( section, path, type );
			}

			if ( type == typeof( bool ) ) {
				return section.GetField<bool>( path );
			}

			if ( type == typeof( byte ) ) {
				return section.GetField<byte>( path );
			}

			if ( type == typeof( sbyte ) ) {
				return section.GetField<sbyte>( path );
			}

			if ( type == typeof( short ) ) {
				return section.GetField<short>( path );
			}

			if ( type == typeof( ushort ) ) {
				return section.GetField<ushort>( path );
			}

			if ( type == typeof( int ) ) {
				return section.GetField<int>( path );
			}

			if ( type == typeof( uint ) ) {
				return section.GetField<uint>( path );
			}

			if ( type == typeof( long ) ) {
				return section.GetField<long>( path );
			}

			if ( type == typeof( ulong ) ) {
				return section.GetField<ulong>( path );
			}

			if ( type == typeof( float ) ) {
				return section.GetField<float>( path );
			}

			if ( type == typeof( double ) ) {
				return section.GetField<double>( path );
			}

			if ( type == typeof( string ) ) {
				return section.GetString( path );
			}

			throw new NotSupportedException(
				$"Field '{path}' has unsupported primitive type '{type.FullName}'."
			);
		}

		private static void WriteEnum( ISaveSectionWriter section, string path, object value, Type enumType )
		{
			Type underlyingType = Enum.GetUnderlyingType( enumType );
			object converted = Convert.ChangeType( value, underlyingType );
			Write( section, path, converted, underlyingType );
		}

		private static object ReadEnum( ISaveSectionReader section, string path, Type enumType )
		{
			Type underlyingType = Enum.GetUnderlyingType( enumType );
			object? rawValue = Read( section, path, underlyingType );

			return Enum.ToObject( enumType, rawValue! );
		}
	};
};
