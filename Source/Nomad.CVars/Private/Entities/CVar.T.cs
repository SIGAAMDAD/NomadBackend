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
using System.Runtime.CompilerServices;
using Nomad.Core.Events;
using Nomad.Core;
using Nomad.Core.Compatibility.Guards;
using System.Collections.Generic;
using Nomad.Core.CVars;
using Nomad.CVars.Private.ValueObjects;
using Nomad.CVars.Exceptions;

namespace Nomad.CVars.Private.Entities
{
	/*
	===================================================================================

	CVar

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class CVar<T> : ICVar<T>
	{
		/// <summary>
		/// The current value of the CVar.
		/// </summary>
		public T Value {
			get => _converter.Value;
			set => Set( value );
		}

		/// <summary>
		/// The default value of the CVar, what it resets to.
		/// </summary>
		public T DefaultValue => _defaultValue;
		private readonly T _defaultValue;

		/// <summary>
		/// The CVar's value type.
		/// </summary>
		public CVarType Type => _metadata.Type;

		/// <summary>
		/// Name of the CVar, should preferrably be prefixed with the system it's tied to, for instance: display.WindowMode
		/// </summary>
		public string Name => (string)_metadata.Name;

		/// <summary>
		/// A description of what the CVar does or impacts. Can be empty but preferrably shouldn't be.
		/// </summary>
		public string Description => (string)_metadata.Description;

		/// <summary>
		/// The CVar's flags (permissions, abilities, capabilities).
		/// </summary>
		public CVarFlags Flags => _metadata.Flags;

		/// <summary>
		/// Queries if the CVar is marked as "Read-Only". Meaning it cannot change from its <see cref="DefaultValue"/>.
		/// </summary>
		public bool IsReadOnly => _metadata.IsReadOnly;

		/// <summary>
		/// Queries if the CVar is marked as "User Created". Meaning it was created from a console command.
		/// </summary>
		public bool IsUserCreated => _metadata.IsUserCreated;

		/// <summary>
		/// Queries if the CVar is marked as "Archive". Meaning it will be written to the settings configuration file.
		/// </summary>
		public bool IsSaved => _metadata.IsSaved;

		/// <summary>
		/// Queuries if the CVar is allowed to be shown in the console.
		/// </summary>
		public bool IsHidden => _metadata.IsHidden;

		/// <summary>
		/// 
		/// </summary>
		public bool IsDeveloper => _metadata.IsDeveloper;

		/// <summary>
		/// 
		/// </summary>
		public bool IsInitializationOnly => _metadata.IsInitializationOnly;

		private readonly CVarMetadata _metadata;
		private readonly CVarValidator<T> _validator;
		private CVarConverter<T> _converter;

		/// <summary>
		/// Event triggered the <see cref="Value"/> changes.
		/// </summary>
		public IGameEvent<CVarValueChangedEventArgs<T>> ValueChanged => _valueChanged;
		private readonly IGameEvent<CVarValueChangedEventArgs<T>> _valueChanged;

		/*
		===============
		CVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="eventFactory"></param>
		/// <param name="createInfo"></param>
		internal CVar( IGameEventRegistryService eventFactory, CVarCreateInfo<T> createInfo )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( createInfo.Name );
			ArgumentGuard.ThrowIfNull( createInfo.Description );

			_validator = new CVarValidator<T>( createInfo.Validator );
			if ( !CVarValidator<T>.ValidateCVarType() ) {
				throw new InvalidCastException( "Invalid CVar type!" );
			}

			_metadata = new CVarMetadata(
				createInfo.Name,
				createInfo.Description,
				createInfo.Flags,
				typeof( T ).GetCVarType()
			);

			_converter = new CVarConverter<T>( _metadata.Type, createInfo.DefaultValue );
			_defaultValue = createInfo.DefaultValue;

			_valueChanged = eventFactory.GetEvent<CVarValueChangedEventArgs<T>>( $"{createInfo.Name}:{CVarValueChangedEventArgs<T>.Name}", CVarValueChangedEventArgs<T>.NameSpace );
		}

		/*
		===============
		Reset
		===============
		*/
		/// <summary>
		/// Resets the cvar's <see cref="Value"/> to the <see cref="DefaultValue"/>.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Reset()
		{
			Value = _defaultValue;
		}

		/*
		===============
		SetFromString
		===============
		*/
		/// <summary>
		/// Sets the CVar's <see cref="Value"/>.
		/// </summary>
		/// <param name="value">The value to set the CVar's <see cref="Value"/> to.</param>
		/// <exception cref="ArgumentException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetFromString( string value )
		{
			ArgumentGuard.ThrowIfNull( value );
			if ( !CVarStringConverter.TryParse( value, typeof( T ), out object? convertedValue ) ) {
				throw new ArgumentException( $"Failed to convert cvar value '{value}' to type {typeof( T ).Name}" );
			}
			Value = (T)convertedValue!;
		}

		/*
		===============
		Set
		===============
		*/
		/// <summary>
		/// Sets the CVar's <see cref="Value"/> to the given value.
		/// </summary>
		/// <param name="value">The new value of the CVar.</param>
		public void Set( T value )
		{
			if ( IsReadOnly || !_validator.ValidateValue( value ) || EqualityComparer<T>.Default.Equals( _converter.Value, value ) ) {
				return;
			}
			T old = _converter.Value;
			_converter.Value = value;
			_valueChanged.Publish( new CVarValueChangedEventArgs<T>( old, value ) );
		}

		/*
		===============
		GetDecimalValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public float GetDecimalValue() => _converter.GetDecimalValue();

		/*
		===============
		GetIntegerValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int GetIntegerValue() => _converter.GetIntegerValue();

		/*
		===============
		GetUIntegerValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public uint GetUIntegerValue() => _converter.GetUIntegerValue();

		/*
		===============
		GetStringValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string? GetStringValue() => _converter.GetStringValue();

		/*
		===============
		GetBooleanValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool GetBooleanValue() => _converter.GetBooleanValue();

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
		public void SetDecimalValue( float value )
		{
			if ( _metadata.Type != CVarType.Decimal ) {
				throw new CVarTypeMismatchException( typeof( float ), _metadata.Type.GetSystemType() );
			}
			Set( Unsafe.As<float, T>( ref value ) );
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
		public void SetIntegerValue( int value )
		{
			if ( _metadata.Type != CVarType.Int ) {
				throw new CVarTypeMismatchException( typeof( int ), _metadata.Type.GetSystemType() );
			}
			Set( Unsafe.As<int, T>( ref value ) );
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
		public void SetUIntegerValue( uint value )
		{
			if ( _metadata.Type != CVarType.UInt ) {
				throw new CVarTypeMismatchException( typeof( uint ), _metadata.Type.GetSystemType() );
			}
			Set( Unsafe.As<uint, T>( ref value ) );
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
		public void SetBooleanValue( bool value )
		{
			if ( _metadata.Type != CVarType.Boolean ) {
				throw new CVarTypeMismatchException( typeof( bool ), _metadata.Type.GetSystemType() );
			}
			Set( Unsafe.As<bool, T>( ref value ) );
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
		public void SetStringValue( string value )
		{
			if ( _metadata.Type != CVarType.String ) {
				throw new CVarTypeMismatchException( typeof( string ), _metadata.Type.GetSystemType() );
			}
			Set( Unsafe.As<string, T>( ref value ) );
		}

		/*
		===============
		operator T
		===============
		*/
		/// <summary>
		/// An implicit conversion operator to smooth over abstraction when interacting with the CVar API.
		/// </summary>
		/// <param name="cvar"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static implicit operator T( CVar<T> cvar )
			=> cvar.Value;
	};
};
