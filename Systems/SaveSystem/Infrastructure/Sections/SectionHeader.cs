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

using NomadCore.Systems.SaveSystem.Errors;
using NomadCore.Systems.SaveSystem.Infrastructure.Serialization.Streams;
using System.IO;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Sections {
	/*
	===================================================================================
	
	SectionHeader
	
	===================================================================================
	*/
	/// <summary>
	/// Represents a save section's header containing the metadata.
	/// </summary>
	
	internal readonly ref struct SectionHeader( string name, int fieldCount, ulong checksum ) {
		private const int SECTION_NAME_MAX_LENGTH = 128;

		public readonly string Name = name;
		public readonly int FieldCount = fieldCount;
		public readonly ulong Checksum = checksum;

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static SectionHeader Load( in SaveStreamReader stream ) {
			string name = stream.ReadString();
			if ( name.Length <= 0 || name.Length > SECTION_NAME_MAX_LENGTH ) {
				throw new IOException( $"Section name corrupt or too long ({name.Length})" );
			}

			int fieldCount = stream.Read<int>();
			if ( fieldCount <= 0 ) {
				throw new FailedSectionLoadException( name, new System.Exception( "field count is corrupt" ) );
			}

			return new SectionHeader( name, fieldCount, stream.Read<ulong>() );
		}
	};
};