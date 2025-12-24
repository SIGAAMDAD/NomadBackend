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
using Nomad.Core.Util;

namespace Nomad.Events.Private {
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
