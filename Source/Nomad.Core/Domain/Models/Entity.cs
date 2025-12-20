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

using NomadCore.Interfaces.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Abstractions.Models {
	/*
	===================================================================================
	
	Entity
	
	===================================================================================
	*/
	/// <summary>
	/// The base entity type for all entities in the game.
	/// </summary>
	
	public abstract class Entity<TId> : IEntity<TId>
		where TId : notnull, IEquatable<TId>
	{
		public TId Id { get; protected init; }
		public DateTime CreatedAt { get; protected init; }
		public DateTime? ModifiedAt { get; protected set; }
		public int Version { get; protected set; }

		protected Entity() {
			CreatedAt = DateTime.UtcNow;
		}

		protected Entity( TId id )
			: this()
		{
			Id = id;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( IEntity<TId>? other ) => other is not null && Id.Equals( other.Id );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override bool Equals( object? obj ) => obj is Entity<TId> entity && Equals( entity );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override int GetHashCode() => Id.GetHashCode();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool operator ==( Entity<TId>? left, Entity<TId>? right ) => Equals( left, right );

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool operator !=( Entity<TId>? left, Entity<TId>? right ) => !Equals( left, right );
	};
};