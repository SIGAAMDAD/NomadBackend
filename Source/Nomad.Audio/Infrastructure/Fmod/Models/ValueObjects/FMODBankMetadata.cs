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
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects {
	/*
	===================================================================================
	
	FMODBankMetadata
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal record FMODBankMetadata : IBankMetadata {
		public DateTime LoadedAt => _createdAt;

		public BankId Id => _id;
		private readonly BankId _id;

		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => null;
		public int Version => 0;

		public FMODBankMetadata( BankId id ) {
			_id = id;
			_createdAt = DateTime.UtcNow;
		}

		public bool Equals( IEntity<BankId>? other ) {
			return other?.Id == Id;
		}
	};
};