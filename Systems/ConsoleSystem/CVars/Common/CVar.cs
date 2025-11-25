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

using NomadCore.Systems.EventSystem;
using Godot;
using NomadCore.Enums;
using NomadCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NomadCore.Abstractions.Services;
using NomadCore.Utilities;

namespace NomadCore.Systems.ConsoleSystem.CVars.Common {
	/*
	===================================================================================

	CVar

	===================================================================================
	*/
	/// <summary>
	/// Literally just a Quake 3/UE5 style CVar but without the copyright issues.
	/// A configurable variable that can be set from the console and settings.ini
	/// </summary>

	public sealed class CVar<T> : ICVar<T> {
		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		public readonly struct CVarValueChangedEventData( CVar<T> cvar, T value ) : ICVarValueChangedEventData<T> {
			public ICVar<T> CVar => _cvar;
			private readonly CVar<T> _cvar = cvar;

			public T Value => _value;
			private readonly T _value = value;
		};

		public T TypedValue {
			get => _value;
			set => Value = value;
		}

		/// <summary>
		/// The current value of the CVar.
		/// </summary>
		public T Value {
			get => _value;
			set {
				if ( IsReadOnly || !ValidateValue( value ) || EqualityComparer<T>.Default.Equals( _value, value ) ) {
					return;
				}
				_value = value;
				_valueChanged.Publish( new CVarValueChangedEventData( this, _value ) );
			}
		}
		private T _value;

		/// <summary>
		/// The default value of the CVar, what it resets to
		/// </summary>
		public T DefaultValue => _defaultValue;
		private readonly T _defaultValue;

		public CVarType Type => _type;
		private readonly CVarType _type;

		/// <summary>
		/// Event triggered the <see cref="Value"/> changes
		/// </summary>
		public IGameEvent<ICVarValueChangedEventData<T>> ValueChanged => _valueChanged;
		private readonly IGameEvent<ICVarValueChangedEventData<T>> _valueChanged;

		public Type ValueType => typeof( T );

		/// <summary>
		/// Name of the CVar, should preferrably be prefixed with the system it's tied to, for instance: display.WindowMode
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// A description of what the CVar does or impacts. Can be empty but preferrably isn't
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// The CVar's flags (permissions, abilities, capabilities)
		/// </summary>
		public CVarFlags Flags { get; }

		/// <summary>
		/// Queries if the CVar is marked as "Read-Only". Meaning it cannot change from its <see cref="DefaultValue"/>.
		/// </summary>
		public bool IsReadOnly => ( Flags & CVarFlags.ReadOnly ) != 0;

		/// <summary>
		/// Queries if the CVar is marked as "User Created". Meaning it was created from a console command.
		/// </summary>
		public bool IsUserCreated => ( Flags & CVarFlags.UserCreated ) != 0;

		/// <summary>
		/// Queries if the CVar is marked as "Archive". Meaning it will be written to the settings configuration file.
		/// </summary>
		public bool IsSaved => ( Flags & CVarFlags.Archive ) != 0;

		/// <summary>
		/// 
		/// </summary>
		public bool IsHidden => ( Flags & CVarFlags.Hidden ) != 0;


		/// <summary>
		/// Validator function for custom validation
		/// </summary>
		private readonly Func<T, bool>? Validator;

		/*
		===============
		CVar
		===============
		*/
		/// <summary>
		/// Constructs a cvar
		/// </summary>
		/// <param name="cvarSystem"></param>
		/// <param name="createInfo"></param>
		/// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is invalid as a CVar name</exception>
		/// <exception cref="InvalidOperationException">Thrown if <paramref name="defaultValue"/> is an invalid CVar value type</exception>
		internal CVar( ICVarSystemService? cvarSystem, IGameEventBusService? eventBus, in CVarCreateInfo<T> createInfo ) {
			if ( !( typeof( T ) == typeof( int ) || typeof( T ) == typeof( bool ) || typeof( T ).IsEnum
				|| typeof( T ) == typeof( string ) || typeof( T ) == typeof( float ) ) )
			{
				throw new InvalidCastException( nameof( T ) );
			}

			// sanity
			ArgumentException.ThrowIfNullOrEmpty( createInfo.Name );

			if ( !IsValidName( createInfo.Name ) ) {
				throw new ArgumentException( $"CVar name {createInfo.Name} contains invalid characters" );
			}

			Name = createInfo.Name;
			Flags = createInfo.Flags;
			
			_defaultValue = createInfo.DefaultValue;
			_value = createInfo.DefaultValue;
			_type = DetermineType( Value );
			_valueChanged = eventBus.CreateEvent<ICVarValueChangedEventData<T>>( nameof( ValueChanged ) );

			Validator = createInfo.Validator;
			Description = createInfo.Description ?? String.Empty;
		}

		/*
		===============
		Register
		===============
		*/
		public void Register() {
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
			Value = DefaultValue;
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
		public void SetFromString( string? value ) {
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
		public string GetStringValue() {
			return TypedValue?.ToString() ?? "";
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
			return uint.TryParse( Value.ToString(), out uint result ) ? result : 0;
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
			return int.TryParse( Value.ToString(), out int result ) ? result : 0;
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
			return float.TryParse( Value.ToString(), out float result ) ? result : 0.0f;
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
			return bool.TryParse( Value.ToString(), out bool result ) && result;
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
		ValidateValue
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private bool ValidateValue( T value ) {
			return Validator == null || Validator.Invoke( value );
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
		private static bool TryConvertStringToType( string? value, Type targetType, out object result ) {
			ArgumentNullException.ThrowIfNull( value );

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
				} else if ( targetType == typeof( NodePath ) ) {
					output = true;
					result = new NodePath( value );
				} else {
					throw new NotSupportedException( $"CVars do not support type {targetType.Name}" );
				}
				return output;
			} catch {
				result = null;
				return false;
			}
		}

		/*
		===============
		IsValidNameCharacter
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static bool IsValidNameCharacter( char c ) {
			return char.IsLetterOrDigit( c ) || c == '.' || c == '_';
		}

		/*
		===============
		IsValidName
		===============
		*/
		/// <summary>
		/// Ensures the provided <paramref name="name"/> string doesn't contain any invalid non alphanumeric characters
		/// </summary>
		/// <param name="name">The name to check</param>
		/// <returns>Returns true if the name is valid</returns>
		private static bool IsValidName( string? name ) {
			if ( string.IsNullOrEmpty( name ) ) {
				return false;
			}

			for ( int i = 0; i < name.Length; i++ ) {
				if ( !IsValidNameCharacter( name[ i ] ) ) {
					return false;
				}
			}
			return true;
		}

		/*
		===============
		DetermineType
		===============
		*/
		/// <summary>
		/// Determines the type of <see cref="Value"/>.
		/// </summary>
		/// <returns>Returns the CVar's value type.</returns>
		private static CVarType DetermineType<B>( B? value ) {
			ArgumentNullException.ThrowIfNull( value );

			Type type = value.GetType();
			if ( type == typeof( int ) ) {
				return CVarType.Int;
			} else if ( type == typeof( uint ) ) {
				return CVarType.UInt;
			} else if ( type == typeof( float ) ) {
				return CVarType.Decimal;
			} else if ( type == typeof( string ) ) {
				return CVarType.String;
			} else if ( type == typeof( NodePath ) ) {
				return CVarType.NodePath;
			} else if ( type == typeof( bool ) ) {
				return CVarType.Boolean;
			}
			return CVarType.Count;
		}
	};
};