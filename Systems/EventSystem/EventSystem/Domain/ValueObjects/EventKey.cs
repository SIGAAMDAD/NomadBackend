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

using NomadCore.Infrastructure.Collections;
using System;

namespace NomadCore.Systems.EventSystem.Domain.ValueObjects {
	/*
	===================================================================================
	
	EventKey
	
	===================================================================================
	*/
	/// <summary>
	/// Represents a <see cref="GameEvent"/>'s unique identifier.
	/// </summary>
	
	internal readonly record struct EventKey : IEquatable<EventKey> {
		public readonly InternString Name { get; }
		public readonly Type ArgsType { get; }
		private readonly int _hashCode;

		public EventKey( InternString name, Type argsType ) {
			Name = name;
			ArgsType = argsType;
			_hashCode = HashCode.Combine(
				name.GetHashCode(),
				argsType.AssemblyQualifiedName?.GetHashCode( StringComparison.Ordinal ) ?? 0
			);
		}

		public bool Equals( EventKey other ) => _hashCode == other._hashCode;
		public override int GetHashCode() => _hashCode;
	};
};