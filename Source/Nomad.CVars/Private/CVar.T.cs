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
using System.Runtime.CompilerServices;
using Nomad.Core.Events;
using Nomad.Core;

namespace Nomad.CVars.Private {
	/*
	===================================================================================

	CVar

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal class CVar<T> : ICVar<T> {
		/// <summary>
		/// The current value of the CVar.
		/// </summary>
		public T Value {
			get => _value;
			set {
				if ( IsReadOnly || !_validator.ValidateValue( value ) || EqualityComparer<T>.Default.Equals( _value, value ) ) {
					return;
				}
				T old = _value;
				_value = value;
				_valueChanged.Publish( new CVarValueChangedEventArgs<T>( old, _value ) );
			}
		}
		private T _value;

		/// <summary>
		/// Event triggered the <see cref="Value"/> changes.
		/// </summary>
		public IGameEvent<CVarValueChangedEventArgs<T>> ValueChanged => _valueChanged;
		private readonly IGameEvent<CVarValueChangedEventArgs<T>> _valueChanged;

		/// <summary>
		/// The default value of the CVar, what it resets to/
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
		public string Name => _metadata.Name;

		/// <summary>
		/// A description of what the CVar does or impacts. Can be empty but preferrably shouldn't be.
		/// </summary>
		public string Description => _metadata.Description;

		/// <summary>
		/// The CVar's flags (permissions, abilities, capabilities)
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

		private readonly CVarMetadata _metadata;
		private readonly CVarValidator<T> _validator;

		/*
		===============
		CVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="createInfo"></param>
		internal CVar( IGameEventRegistryService eventFactory, in CVarCreateInfo<T> createInfo ) {
			_validator = new CVarValidator<T>( createInfo.Validator );
			if ( !_validator.ValidateCVarType() ) {
				throw new InvalidCastException();
			}
			_metadata = new CVarMetadata(
				createInfo.Name,
				createInfo.Description,
				createInfo.Flags,
				typeof( T ).GetCVarType()
			);

			_value = createInfo.DefaultValue;
			_defaultValue = createInfo.DefaultValue;
			_valueChanged = eventFactory.GetEvent<CVarValueChangedEventArgs<T>>( Constants.Events.CVars.CVAR_VALUE_CHANGED_EVENT );
		}

		/*
		===============
		Reset
		===============
		*/
		/// <summary>
		/// Resets the cvar's <see cref="Value"/> to the <see cref="DefaultValue"/>
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Reset() {
			Value = _defaultValue;
		}

		/*
		===============
		SetFromStrings
		===============
		*/
		/// <summary>
		/// Sets the CVar's <see cref="Value"/>.
		/// </summary>
		/// <param name="value">The value to set the CVar's <see cref="Value"/> to.</param>
		/// <exception cref="ArgumentException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetFromString( string value ) {
			ArgumentNullException.ThrowIfNull( value );
			if ( !TryConvertStringToType( value, typeof( T ), out object convertedValue ) ) {
				throw new ArgumentException( $"Failed to convert cvar value '{value}' to type {typeof( T ).Name}" );
			}
			Value = (T)convertedValue;
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
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Set( T value ) {
			_value = value;
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
		public string? GetStringValue() {
			return Value is string stringValue ? stringValue : null;
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
		public uint GetUIntegerValue() {
			ArgumentNullException.ThrowIfNull( Value );
			return Value is uint uintValue ? uintValue : 0;
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
		public int GetIntegerValue() {
			ArgumentNullException.ThrowIfNull( Value );
			return Value is int intValue ? intValue : 0;
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
		public float GetDecimalValue() {
			ArgumentNullException.ThrowIfNull( Value );
			return Value is float floatValue ? floatValue : 0.0f;
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
		public bool GetBooleanValue() {
			ArgumentNullException.ThrowIfNull( Value );
			return Value is bool boolValue && boolValue;
		}

		/*
		===============
		GetValue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T1"></typeparam>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public T1 GetValue<T1>() {
			return Value is T1 value ? value : default;
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
		public static implicit operator T( CVar<T> cvar ) {
			return cvar._value;
		}

		/*
		===============
		ConvertStringToType
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <param name="targetType"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">Thrown if the provided <paramref name="targetType"/> isn't a valid CVar <see cref="Value"/> type.</exception>
		private static bool TryConvertStringToType( string value, Type targetType, out object result ) {
			try {
				bool output;
				if ( targetType.IsEnum ) {
					result = Enum.Parse( targetType, value );
					return true;
				} else if ( targetType == typeof( sbyte ) ) {
					output = sbyte.TryParse( value, out sbyte data );
					result = data;
				} else if ( targetType == typeof( short ) ) {
					output = short.TryParse( value, out short data );
					result = data;
				} else if ( targetType == typeof( int ) ) {
					output = int.TryParse( value, out int data );
					result = data;
				} else if ( targetType == typeof( long ) ) {
					output = long.TryParse( value, out long data );
					result = data;
				} else if ( targetType == typeof( byte ) ) {
					output = byte.TryParse( value, out byte data );
					result = data;
				} else if ( targetType == typeof( ushort ) ) {
					output = ushort.TryParse( value, out ushort data );
					result = data;
				} else if ( targetType == typeof( uint ) ) {
					output = uint.TryParse( value, out uint data );
					result = data;
				} else if ( targetType == typeof( ulong ) ) {
					output = ulong.TryParse( value, out ulong data );
					result = data;
				} else if ( targetType == typeof( float ) ) {
					output = float.TryParse( value, out float data );
					result = data;
				} else if ( targetType == typeof( double ) ) {
					output = double.TryParse( value, out double data );
					result = data;
				} else if ( targetType == typeof( bool ) ) {
					output = bool.TryParse( value, out bool data );
					result = data;
				} else if ( targetType == typeof( string ) ) {
					output = true;
					result = value;
				} else {
					throw new NotSupportedException( $"CVars do not support type {targetType.Name}" );
				}
				return output;
			} catch {
				result = null;
				return false;
			}
		}
	};
};
