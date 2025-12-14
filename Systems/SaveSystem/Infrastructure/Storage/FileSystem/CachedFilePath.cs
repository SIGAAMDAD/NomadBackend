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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects;
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Storage.FileSystem {
	internal sealed class CachedFilePath() {
		public SaveFileId FileId { get; set; }
		public FilePath Path { get; set; }
		public DateTime AddedAt { get; set; }
		public DateTime LastAccessed { get; set; }
		public int AccessCount { get; set; }
		public int FileSize { get; set; }

		public void WriteTo( MemoryMappedViewAccessor accessor, ref int offset ) {
			accessor.Write( offset, FileId.Value );
			offset += sizeof( int );

			accessor.Write( offset, Path.GodotPath.Length );
			offset += sizeof( int );

			WriteTimestamp( accessor, AddedAt, ref offset );
			WriteTimestamp( accessor, LastAccessed, ref offset );

			accessor.Write( offset, AccessCount );
			offset += sizeof( int );

			accessor.Write( offset, FileSize );
			offset += sizeof( int );
		}

		public static CachedFilePath ReadFrom( MemoryMappedViewAccessor accessor, ref int offset ) {
			var cached = new CachedFilePath();

			accessor.Read( offset, out int slot );
			offset += sizeof( int );

			cached.FileId = new SaveFileId(
				Value: slot
			);

			cached.AddedAt = ReadTimestamp( accessor, ref offset );
			cached.LastAccessed = ReadTimestamp( accessor, ref offset );

			accessor.Read( offset, out int accessCount );
			offset += sizeof( int );
			cached.AccessCount = accessCount;

			accessor.Read( offset, out int fileSize );
			offset += sizeof( int );
			cached.FileSize = fileSize;

			return cached;
		}

		private static void WriteTimestamp( MemoryMappedViewAccessor accessor, DateTime dateTime, ref int offset ) {
			accessor.Write( offset, ref dateTime );
			offset += Marshal.SizeOf<DateTime>();
		}

		private static DateTime ReadTimestamp( MemoryMappedViewAccessor accessor, ref int offset ) {
			accessor.Read( offset, out DateTime timestamp );
			offset += Marshal.SizeOf<DateTime>();
			return timestamp;
		}
	};
};