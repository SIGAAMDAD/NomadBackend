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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Systems.EntitySystem.Domain.Models.Interfaces;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects {
	/*
	===================================================================================
	
	EntityStat
	
	===================================================================================
	*/
	/// <summary>
	/// For storing entity stats such as max health, max rage, etc.
	/// made this way to allow for rune and boon buffs/debuffs
	/// </summary>
	/// <typeparam name="T">The type of stat, must be a primitive.</typeparam>
	/// <remarks>
	/// 
	/// </remarks>
	/// <param name="value"></param>
	/// <param name="maxValue"></param>
	/// <param name="minValue"></param>

	public record struct EntityStat<T>( T maxValue, T minValue, T value ) : IEntityStat<T>
		where T : unmanaged
	{
		/// <summary>
		/// The maximum value allowed for the stat.
		/// </summary>
		public T MaxValue {
			readonly get => _maxValue;
			set => _maxValue = value;
		}
		private T _maxValue = maxValue;

		/// <summary>
		/// The maximum value allowed for the stat.
		/// </summary>
		public T MinValue {
			readonly get => _minValue;
			set => _minValue = value;
		}
		private T _minValue = minValue;

		/// <summary>
		/// The base value loaded or determined at compile time
		/// </summary>
		public readonly T BaseValue => _baseValue;
		private readonly T _baseValue = value;

		/// <summary>
		/// The value that can be changed and the value that is read at runtime
		/// </summary>
		public T Value {
			readonly get => _value;
			set => _value = value;
		}
		private T _value = value;

		public readonly Any GetValue() => Any.From( _value );
		public readonly Any GetMinValue() => Any.From( _minValue );
		public readonly Any GetMaxValue() => Any.From( _maxValue );
		public readonly Any GetBaseValue() => Any.From( _baseValue );

		public void SetValue( Any value ) => _value = value.GetValue<T>();
		public void SetMinValue( Any value ) => _minValue = value.GetValue<T>();
		public void SetMaxValue( Any value ) => _maxValue = value.GetValue<T>();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public readonly bool Equals( IEntityStat<T>? other ) {
			return other is not null && other.Value.Equals( _value ) && other.MaxValue.Equals( _maxValue ) && other.MinValue.Equals( _minValue ) && other.BaseValue.Equals( _baseValue );
		}

		/*
		===============
		Equals
		===============
		*/
		public bool Equals( IEntityStat? other ) {
			return other is not null && other.GetValue().Equals( GetValue() ) && other.GetMaxValue().Equals( GetMaxValue() ) && other.GetMinValue().Equals( GetMinValue() ) && other.GetBaseValue().Equals( GetBaseValue() );
		}
	};
};