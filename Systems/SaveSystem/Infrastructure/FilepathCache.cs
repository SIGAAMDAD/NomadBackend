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

using Godot;
using NomadCore.Abstractions.Services;
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Utilities;
using NomadCore.Enums;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.SaveSystem.Infrastructure {
	/*
	===================================================================================
	
	FilepathCache

	===================================================================================
	*/
	/// <summary>
	/// Caches all filepaths to reduce string allocation overhead.
	/// </summary>
	
	internal static class FilepathCache {
		public static readonly FilePath SavePath;
		private static readonly Dictionary<int, string> SlotPaths = new Dictionary<int, string>();
		private static readonly Dictionary<KeyValuePair<int, int>, string> BackupPaths = new Dictionary<KeyValuePair<int, int>, string>();

		/*
		===============
		FilepathCache
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		static FilepathCache() {
			ICVar<string>? saveLocation = ServiceRegistry.Get<ICVarSystemService>().GetCVar<string>( "game.SaveLocation" );
			ArgumentNullException.ThrowIfNull( saveLocation );
			SavePath = new FilePath( $"{saveLocation.Value}/", PathType.User );
		}

		/*
		===============
		GetSlotPath
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public static string GetSlotPath( int slot ) {
			if ( !SlotPaths.TryGetValue( slot, out string? value ) ) {
				value = $"{SavePath}GameData_{slot}.ngd";
				SlotPaths.Add( slot, value );
			}
			return value;
		}

		/*
		===============
		GetBackupPath
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="backupIndex"></param>
		/// <returns></returns>
		public static string GetBackupPath( int slot, int backupIndex ) {
			KeyValuePair<int, int> kvp = new KeyValuePair<int, int>( slot, backupIndex );
			if ( !BackupPaths.TryGetValue( kvp, out string? value ) ) {
				value = $"{SavePath}GameData_{slot}.ngd.backup{backupIndex}";
				BackupPaths.Add( kvp, value );
			}
			return value;
		}
	};
};