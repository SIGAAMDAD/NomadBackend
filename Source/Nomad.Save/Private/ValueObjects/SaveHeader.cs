/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.FileSystem.Streams;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Private.ValueObjects
{
	/*
	===================================================================================
	
	SaveHeader
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal record SaveHeader
	{
		/// <summary>
		/// 
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// 
		/// </summary>
		public readonly GameVersion Version;

		/// <summary>
		/// 
		/// </summary>
		public readonly int SectionCount;

		/// <summary>
		/// 
		/// </summary>
		public readonly Checksum Checksum;

		/*
		===============
		SaveHeader
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="version"></param>
		/// <param name="sectionCount"></param>
		/// <param name="checksum"></param>
		public SaveHeader( string name, GameVersion version, int sectionCount, Checksum checksum )
		{
			Name = name;
			Version = version;
			SectionCount = sectionCount;
			Checksum = checksum;
		}

		/*
		===============
		Serialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="writer"></param>
		internal void Serialize( IWriteStream writer )
		{
			writer.WriteUInt64( Constants.HEADER_MAGIC );
			Version.Serialize( writer );
			writer.WriteString( Name );
			writer.WriteInt32( SectionCount );
			writer.WriteUInt64( Checksum.Value );
		}

		/*
		===============
		Serialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="magicMatches"></param>
		/// <returns></returns>
		internal static SaveHeader Deserialize( IReadStream reader, out bool magicMatches )
		{
			ulong headerMagic = reader.ReadUInt64();
			magicMatches = headerMagic == Constants.HEADER_MAGIC;

			GameVersion version = GameVersion.Deserialize( reader );
			string name = reader.ReadString();
			int sectionCount = reader.ReadInt32();
			ulong checksum = reader.ReadUInt64();

			return new SaveHeader(
				name: name,
				version: version,
				sectionCount: sectionCount,
				checksum: new Checksum( value: checksum )
			);
		}
	};
};
