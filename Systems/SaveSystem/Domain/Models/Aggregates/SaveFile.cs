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
using NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.SaveSystem.Domain.Models.Aggregates {
	/*
	===================================================================================
	
	SaveFile
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class SaveFile : ISaveSlot {
		public SaveFileId Id => _id;
		private readonly SaveFileId _id;

		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => _saveTime;
		private readonly DateTime _saveTime;

		public int Version => _version.ToInt();
		private readonly GameVersion _version;

		public IReadOnlyDictionary<SectionId, ISection> Sections => _sections;
		private readonly ImmutableDictionary<SectionId, ISection> _sections;

		/*
		===============
		SaveFile
		===============
		*/
		private SaveFile( SaveFileId id, ImmutableDictionary<SectionId, ISection> sections, DateTime createdAt ) {
			_id = id;
			_createdAt = createdAt;
			_sections = sections;
		}
		
		/*
		===============
		Equals
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool Equals( IEntity<SaveFileId>? other ) {
			return other is not null && other.Id == _id;
		}

		/*
		===============
		Create
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="sections"></param>
		/// <returns></returns>
		public static SaveFile Create( SaveFileId id, IReadOnlyList<ISection> sections ) {
			var builder = ImmutableDictionary.CreateBuilder<SectionId, ISection>();

			foreach ( var section in sections ) {
				builder[ section.Id ] = section;
			}

			return new SaveFile( id, builder.ToImmutable(), DateTime.UtcNow );
		}

		/*
		===============
		Save
		===============
		*/
		public void Save() {
		}
	};
};