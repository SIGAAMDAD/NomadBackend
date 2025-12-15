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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NomadCore.Infrastructure.Collections {
	/*
	===================================================================================
	
	InternString
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public readonly struct InternString : IEquatable<InternString> {
		private readonly int _id;
		
		internal InternString( int id ) {
			_id = id;
		}
		public InternString( ReadOnlySpan<char> value ) {
			StringPool.Intern( value, ref _id );
		}

		public static readonly InternString Empty = new InternString();
		
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( InternString other ) => other._id == _id;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override bool Equals( [NotNullWhen( true )] object?obj ) => obj is InternString other && other._id == _id;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override int GetHashCode() => _id;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static implicit operator string( InternString value ) => StringPool.FromInterned( value ) ?? String.Empty;

		public static bool operator==( InternString left, InternString right ) => left._id == right._id;
		public static bool operator!=( InternString left, InternString right ) => left._id != right._id;
	};
};