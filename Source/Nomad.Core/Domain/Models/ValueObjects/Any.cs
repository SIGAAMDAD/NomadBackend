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

using NomadCore.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NomadCore.Domain.Models.ValueObjects {
	/*
	===================================================================================
	
	Any
	
	===================================================================================
	*/
	/// <summary>
	/// Everything occupies the same memory space because it's meant to be like c++'s std::any
	/// </summary>

	public readonly record struct Any : IValueObject<Any> {
		[StructLayout( LayoutKind.Explicit, Pack = 1 )]
		private struct Union {
			[FieldOffset( 0 )] public bool Boolean;

			[FieldOffset( 0 )] public sbyte Int8;
			[FieldOffset( 0 )] public short Int16;
			[FieldOffset( 0 )] public int Int32;
			[FieldOffset( 0 )] public long Int64;

			[FieldOffset( 0 )] public byte UInt8;
			[FieldOffset( 0 )] public ushort UInt16;
			[FieldOffset( 0 )] public uint UInt32;
			[FieldOffset( 0 )] public ulong UInt64;

			[FieldOffset( 0 )] public float Float32;
			[FieldOffset( 0 )] public double Float64;
		};

		private static readonly IReadOnlyDictionary<Type, AnyType> SystemTypeToAnyType = new Dictionary<Type, AnyType>() {
			{ typeof( bool ), AnyType.Boolean },
			{ typeof( sbyte ), AnyType.Int8 },
			{ typeof( short ), AnyType.Int16 },
			{ typeof( int ), AnyType.Int32 },
			{ typeof( long ), AnyType.Int64 },
			{ typeof( byte ), AnyType.UInt8 },
			{ typeof( ushort ), AnyType.UInt16 },
			{ typeof( uint ), AnyType.UInt32 },
			{ typeof( ulong ), AnyType.UInt64 },
			{ typeof( float ), AnyType.Float32 },
			{ typeof( double ), AnyType.Float64 }
		};
		private static readonly IReadOnlyDictionary<AnyType, Type> AnyTypeToSystemType = new Dictionary<AnyType, Type>() {
			{ AnyType.Boolean, typeof( bool ) },
			{ AnyType.Int8, typeof( sbyte ) },
			{ AnyType.Int16, typeof( short ) },
			{ AnyType.Int32, typeof( int ) },
			{ AnyType.Int64, typeof( long ) },
			{ AnyType.UInt8, typeof( byte ) },
			{ AnyType.UInt16, typeof( ushort ) },
			{ AnyType.UInt32, typeof( uint ) },
			{ AnyType.UInt64, typeof( ulong ) },
			{ AnyType.Float32, typeof( float ) },
			{ AnyType.Float64, typeof( double ) }
		};

		private readonly Union Value;
                              
		public Any( bool b ) => Value = new Union { Boolean = b };
		public Any( sbyte i8 ) => Value = new Union { Int8 = i8 };
		public Any( short i16 ) => Value = new Union { Int16 = i16 };
		public Any( int i32 ) => Value = new Union { Int32 = i32 };
		public Any( long i64 ) => Value = new Union { Int64 = i64 };
		public Any( byte u8 ) => Value = new Union { UInt8 = u8 };
		public Any( ushort u16 ) => Value = new Union { UInt16 = u16 };
		public Any( uint u32 ) => Value = new Union { UInt32 = u32 };
		public Any( ulong u64 ) => Value = new Union { UInt64 = u64 };
		public Any( float f32 ) => Value = new Union { Float32 = f32 };
		public Any( double f64 ) => Value = new Union { Float64 = f64 };

		public static implicit operator bool( Any value ) => value.Value.Boolean;
		public static implicit operator sbyte( Any value ) => value.Value.Int8;
		public static implicit operator short( Any value ) => value.Value.Int16;
		public static implicit operator int( Any value ) => value.Value.Int32;
		public static implicit operator long( Any value ) => value.Value.Int64;
		public static implicit operator byte( Any value ) => value.Value.UInt8;
		public static implicit operator ushort( Any value ) => value.Value.UInt16;
		public static implicit operator uint( Any value ) => value.Value.UInt32;
		public static implicit operator ulong( Any value ) => value.Value.UInt64;
		public static implicit operator float( Any value ) => value.Value.Float32;
		public static implicit operator double( Any value ) => value.Value.Float64;

		/*
		===============
		From
		===============
		*/
		/// <summary>
		/// Creates an Any object from the provided primitive value.
		/// </summary>
		/// <typeparam name="T">The type of the value</typeparam>
		/// <returns>A new Any object</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Any From<T>( T value ) where T : unmanaged => value switch {
			bool b => new Any( b ),
			sbyte i8 => new Any( i8 ),
			short i16 => new Any( i16 ),
			int i32 => new Any( i32 ),
			long i64 => new Any( i64 ),
			byte u8 => new Any( u8 ),
			ushort u16 => new Any( u16 ),
			uint u32 => new Any( u32 ),
			ulong u64 => new Any( u64 ),
			float f32 => new Any( f32 ),
			double f64 => new Any( f64 ),
			_ => throw new InvalidCastException( $"An Any object cannot hold a value of type {typeof( T )}" )
		};

		/*
		===============
		GetValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public T? GetValue<T>() => typeof( T ) switch {
			Type t when t == typeof( bool ) => (T)(object)Value.Boolean,
			Type t when t == typeof( sbyte ) => (T)(object)Value.Int8,
			Type t when t == typeof( short ) => (T)(object)Value.Int16,
			Type t when t == typeof( int ) => (T)(object)Value.Int32,
			Type t when t == typeof( long ) => (T)(object)Value.Int64,
			Type t when t == typeof( byte ) => (T)(object)Value.UInt8,
			Type t when t == typeof( ushort ) => (T)(object)Value.UInt16,
			Type t when t == typeof( uint ) => (T)(object)Value.UInt32,
			Type t when t == typeof( ulong ) => (T)(object)Value.UInt64,
			Type t when t == typeof( float ) => (T)(object)Value.Float32,
			Type t when t == typeof( double ) => (T)(object)Value.Float64,
		};

		/*
		===============
		GetAnyType
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static Type GetAnyType( AnyType type ) =>
			AnyTypeToSystemType.TryGetValue( type, out Type value ) ? value : throw new InvalidCastException( $"Invalid field type {type}" );

		/*
		===============
		GetAnyType
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static AnyType GetType<T>() =>
			SystemTypeToAnyType.TryGetValue( typeof( T ), out AnyType type ) ? type : throw new InvalidCastException( $"Invalid field type {typeof( T )}" );
	};
};