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

using NomadCore.Abstractions.Services;
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Systems.SaveSystem.Infrastructure.Streams;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.SaveSystem.Infrastructure {
	/*
	===================================================================================
	
	SaveHeader
	
	===================================================================================
	*/
	/// <summary>
	/// Represents a save section's header containing the metadata.
	/// </summary>
	
	internal readonly ref struct SaveHeader( uint versionMajor, uint versionMinor, uint versionPatch, int sectionCount, ulong checksum ) {
		public readonly uint VersionMajor = versionMajor;
		public readonly uint VersionMinor = versionMinor;
		public readonly uint VersionPatch = versionPatch;
		public readonly int SectionCount = sectionCount;
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
		public static SaveHeader Load( in SaveReaderStream stream ) {
			return new SaveHeader( stream.ReadUInt32(), stream.ReadUInt32(), stream.ReadUInt32(), stream.ReadInt32(), stream.ReadUInt64() );
		}

		/*
		===============
		Write
		===============
		*/
		public static void Write( Slot slot, SaveStreamWriter stream ) {
			var cvarSystem = ServiceRegistry.Get<ICVarSystemService>();

			ICVar<uint>? versionMajor = cvarSystem.GetCVar<uint>( "game.versionMajor" );
			ICVar<uint>? versionMinor = cvarSystem.GetCVar<uint>( "game.versionMinor" );
			ICVar<uint>? versionPatch = cvarSystem.GetCVar<uint>( "game.versionPatch" );

			stream.Write( versionMajor.Value );
			stream.Write( versionMinor.Value );
			stream.Write( versionPatch.Value );
		}
	};
};