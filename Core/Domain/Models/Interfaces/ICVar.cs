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

using System;
using NomadCore.Domain.Events;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Infrastructure.Collections;

namespace NomadCore.Domain.Models.Interfaces {
	/*
	===================================================================================
	
	ICvar
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public interface ICVar {
		public InternString Name { get; }
		public InternString Description { get; }
		public CVarType Type { get; }
		public CVarFlags Flags { get; }

		public bool IsSaved { get; }
		public bool IsReadOnly { get; }
		public bool IsUserCreated { get; }
		public bool IsHidden { get; }

		public Type ValueType { get; }

		public float GetDecimalValue();
		public int GetIntegerValue();
		public uint GetUIntegerValue();
		public string? GetStringValue();
		public bool GetBooleanValue();
		public T GetValue<T>();

		public void Reset();
		public void SetFromString( string value );
	};
	public interface ICVar<T> : ICVar {
		public T Value { get; set; }
		public T DefaultValue { get; }

		public IGameEvent<CVarValueChangedEventData<T>> ValueChanged { get; }
	};
};