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
using NomadCore.Enums;
using System;

namespace NomadCore.Utilities {
	/*
	===================================================================================
	
	FilePath
	
	===================================================================================
	*/
	/// <summary>
	/// Handles the conversion from a native OS path to a localized godot path and vice versa.
	/// </summary>
	
	public sealed class FilePath {
		public string OSPath => _osPath;
		private readonly string _osPath;

		public string GodotPath => _godotPath;
		private readonly string _godotPath;

		public readonly PathType Type;

		/*
		===============
		FilePath
		===============
		*/
		public FilePath( string? filePath, PathType type ) {
			ArgumentException.ThrowIfNullOrEmpty( filePath );

			switch ( type ) {
				case PathType.Native:
					_osPath = filePath;
					_godotPath = ProjectSettings.LocalizePath( _osPath );
					break;
				case PathType.User:
				case PathType.Resource:
					_godotPath = filePath;
					_osPath = ProjectSettings.GlobalizePath( _osPath );
					break;
				default:
					throw new ArgumentOutOfRangeException( $"Path type '{type}' isn't a valid PathType" );
			}
			Type = type;
		}
	};
};