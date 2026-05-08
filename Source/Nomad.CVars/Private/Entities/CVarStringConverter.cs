/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Nomad.CVars.Private.Entities
{
	/*
	===================================================================================
	
	CVarStringConverter
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal static class CVarStringConverter
	{
		private delegate bool TryParseDelegate( string s, out object? result );
		private delegate bool TryParseUnmanaged<T>( string s, NumberStyles styles, IFormatProvider provider, out T value ) where T : unmanaged;

		private static readonly Dictionary<Type, TryParseDelegate> _parsers = new Dictionary<Type, TryParseDelegate>() {
			[typeof( sbyte )] = ( string s, out object? r ) => TryParsePrimitive<sbyte>( s, out r, NumberStyles.Integer, sbyte.TryParse ),
			[typeof( short )] = ( string s, out object? r ) => TryParsePrimitive<short>( s, out r, NumberStyles.Integer, short.TryParse ),
			[typeof( int )] = ( string s, out object? r ) => TryParsePrimitive<int>( s, out r, NumberStyles.Integer, int.TryParse ),
			[typeof( long )] = ( string s, out object? r ) => TryParsePrimitive<long>( s, out r, NumberStyles.Integer, long.TryParse ),
			[typeof( byte )] = ( string s, out object? r ) => TryParsePrimitive<byte>( s, out r, NumberStyles.Integer, byte.TryParse ),
			[typeof( ushort )] = ( string s, out object? r ) => TryParsePrimitive<ushort>( s, out r, NumberStyles.Integer, ushort.TryParse ),
			[typeof( uint )] = ( string s, out object? r ) => TryParsePrimitive<uint>( s, out r, NumberStyles.Integer, uint.TryParse ),
			[typeof( ulong )] = ( string s, out object? r ) => TryParsePrimitive<ulong>( s, out r, NumberStyles.Integer, ulong.TryParse ),
			[typeof( float )] = ( string s, out object? r ) => TryParsePrimitive<float>( s, out r, NumberStyles.Float, float.TryParse ),
			[typeof( double )] = ( string s, out object? r ) => TryParsePrimitive<double>( s, out r, NumberStyles.Float, double.TryParse ),
			[typeof( bool )] = ( string s, out object? r ) => TryParseBool( s, out r ),
			[typeof( string )] = ( string s, out object? r ) => { r = s; return true; }
		};

		/*
		===============
		TryParseBool
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private static bool TryParseBool( string s, out object? result )
		{
			if ( bool.TryParse( s, out bool value ) ) {
				result = value;
				return true;
			}
			result = null;
			return false;
		}

		/*
		===============
		TryParsePrimitive
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="s"></param>
		/// <param name="result"></param>
		/// <param name="style"></param>
		/// <param name="parser"></param>
		/// <returns></returns>
		private static bool TryParsePrimitive<T>( string s, out object? result, NumberStyles style, TryParseUnmanaged<T> parser )
			where T : unmanaged
		{
			if ( parser.Invoke( s, style, CultureInfo.InvariantCulture, out var value ) ) {
				result = value;
				return true;
			}
			result = null;
			return false;
		}

		/*
		===============
		TryParse
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="targetType"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public static bool TryParse( string str, Type targetType, out object? result )
		{
			if ( _parsers.TryGetValue( targetType, out var parser ) ) {
				return parser.Invoke( str, out result );
			}
			if ( targetType.IsEnum ) {
				return EnumTryParse( str, targetType, out result );
			}
			throw new NotSupportedException( $"CVars do not support type {targetType.Name}" );
		}

		/*
		===============
		EnumTryParse
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="targetType"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private static bool EnumTryParse( string str, Type targetType, out object? result )
		{
			// we only use Enum.IsDefined here because if we don't there's a bug where even if the integral value itself exceeds the enum, it'll still get parsed as valid. So we need a manual
			// bounds check here to ensure that never happens.
			if ( Enum.TryParse( targetType, str, ignoreCase: true, out result ) && Enum.IsDefined( targetType, result ) ) {
				return true;
			}
			if ( TryParse( str, Enum.GetUnderlyingType( targetType ), out object? underlying ) && Enum.IsDefined( targetType, underlying ) ) {
				result = Enum.ToObject( targetType, underlying );
				return true;
			}
			result = null;
			return false;
		}
	};
};
