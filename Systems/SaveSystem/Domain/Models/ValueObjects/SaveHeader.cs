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

using NomadCore.Systems.SaveSystem.Infrastructure.Serialization.Streams;
using System;

namespace NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects {
	public readonly record struct SaveHeader(
		GameVersion Version,
		int SectionCount,
		Checksum Checksum
	) : IEquatable<SaveHeader> {
		internal void Serialize( SaveStreamWriter writer ) {
			Version.Serialize( writer );
			writer.Write( SectionCount );
			writer.Write( Checksum.Value );
		}
		internal static SaveHeader Deserialize( SaveStreamReader reader ) {;
			GameVersion version = GameVersion.Deserialize( reader );
			int sectionCount = reader.Read<int>();
			uint checksum = reader.Read<uint>();
			
			return new SaveHeader {
				Version = version,
				SectionCount = sectionCount,
				Checksum = new Checksum { Value = checksum }
			};
		}
	};
};