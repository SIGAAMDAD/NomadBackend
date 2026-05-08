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

using System.Runtime.CompilerServices;
using Nomad.Core.CVars;
using Nomad.CVars.Exceptions;

namespace Nomad.CVars.Private.Entities
{
	/*
	===================================================================================

	CVarConverter

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>

	internal struct CVarConverter<T>
	{
		public T Value;
		private readonly CVarType _type;

		/*
		===============
		CVarConverter
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="defaultValue"></param>
		public CVarConverter( CVarType type, T defaultValue )
		{
			_type = type;
			Value = defaultValue;
		}

		/*
		===============
		GetStringValue
		===============
		*/
		/// <summary>
		/// Retrieves the CVar's <see cref="Value"/> as a string.
		/// </summary>
		/// <returns>The <see cref="Value"/> in string format.</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public readonly string? GetStringValue()
		{
			return _type == CVarType.String ? Value?.ToString() : throw new CVarTypeMismatchException( typeof( string ), _type.GetSystemType() );
		}

		/*
		===============
		GetUIntegerValue
		===============
		*/
		/// <summary>
		/// Retrieves the CVar's <see cref="Value"/> as a 32-bit integer.
		/// </summary>
		/// <returns>The <see cref="Value"/> in 32-bit integer format, 0 by default if int.TryParse failed</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public readonly uint GetUIntegerValue()
		{
			return _type == CVarType.UInt ? Unsafe.As<T, uint>( ref Unsafe.AsRef( in Value ) ) : throw new CVarTypeMismatchException( typeof( uint ), _type.GetSystemType() );
		}

		/*
		===============
		GetIntegerValue
		===============
		*/
		/// <summary>
		/// Retrieves the CVar's <see cref="Value"/> as a 32-bit integer.
		/// </summary>
		/// <returns>The <see cref="Value"/> in 32-bit integer format, 0 by default if int.TryParse failed</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public readonly int GetIntegerValue()
		{
			return _type == CVarType.Int ? Unsafe.As<T, int>( ref Unsafe.AsRef( in Value ) ) : throw new CVarTypeMismatchException( typeof( int ), _type.GetSystemType() );
		}

		/*
		===============
		GetFloatValue
		===============
		*/
		/// <summary>
		/// Retrieves the CVar's <see cref="Value"/> as a 32-bit floating-point.
		/// </summary>
		/// <returns>The <see cref="Value"/> in 32-bit floating-point format, 0 by default if float.TryParse failed</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetDecimalValue()
		{
			return _type == CVarType.Decimal ? Unsafe.As<T, float>( ref Unsafe.AsRef( in Value ) ) : throw new CVarTypeMismatchException( typeof( float ), _type.GetSystemType() );
		}

		/*
		===============
		GetBooleanValue
		===============
		*/
		/// <summary>
		/// Retrieves the CVar's <see cref="Value"/> as an 8-bit boolean.
		/// </summary>
		/// <returns>The <see cref="Value"/> in 8-bit boolean format, false by default if bool.TryParse failed</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public readonly bool GetBooleanValue()
		{
			return _type == CVarType.Boolean ? Unsafe.As<T, bool>( ref Unsafe.AsRef( in Value ) ) : throw new CVarTypeMismatchException( typeof( bool ), _type.GetSystemType() );
		}

		/*
		===============
		SetIntegerValue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="CVarTypeMismatchException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetIntegerValue( int value )
		{
			Value = _type == CVarType.Int ? Unsafe.As<int, T>( ref value ) : throw new CVarTypeMismatchException( typeof( int ), _type.GetSystemType() );
		}

		/*
		===============
		SetUIntegerValue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="CVarTypeMismatchException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetUIntegerValue( uint value )
		{
			Value = _type == CVarType.UInt ? Unsafe.As<uint, T>( ref value ) : throw new CVarTypeMismatchException( typeof( uint ), _type.GetSystemType() );
		}

		/*
		===============
		SetBooleanValue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="CVarTypeMismatchException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetBooleanValue( bool value )
		{
			Value = _type == CVarType.Boolean ? Unsafe.As<bool, T>( ref value ) : throw new CVarTypeMismatchException( typeof( bool ), _type.GetSystemType() );
		}

		/*
		===============
		SetDecimalValue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="CVarTypeMismatchException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetDecimalValue( float value )
		{
			Value = _type == CVarType.Decimal ? Unsafe.As<float, T>( ref value ) : throw new CVarTypeMismatchException( typeof( float ), _type.GetSystemType() );
		}

		/*
		===============
		SetStringValue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="CVarTypeMismatchException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetStringValue( string value )
		{
			Value = _type == CVarType.String ? Unsafe.As<string, T>( ref value ) : throw new CVarTypeMismatchException( typeof( string ), _type.GetSystemType() );
		}

		/*
		===============
		SetValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <param name="value"></param>
		/// <exception cref="CVarTypeMismatchException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetValue<T1>( T1 value )
		{
			if ( typeof( T ) == typeof( T1 ) ) {
				Value = Unsafe.As<T1, T>( ref value );
			} else {
				throw new CVarTypeMismatchException( typeof( T1 ), _type.GetSystemType() );
			}
		}
	};
};
