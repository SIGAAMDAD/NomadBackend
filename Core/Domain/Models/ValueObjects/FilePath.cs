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
using NomadCore.Interfaces.Common;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NomadCore.Domain.Models.ValueObjects {
	/*
	===================================================================================
	
	FilePath
	
	===================================================================================
	*/
	/// <summary>
	/// A wrapper class that holds a godot res:// or user:// path along with it's platform specific path.
	/// </summary>
	
	public record FilePath : IValueObject<FilePath> {
		public string OSPath { get; init; }
		public string GodotPath { get; init; }
		public PathType Type { get; init; }

		private readonly int _hashCode;

		/*
		===============
		FilePath
		===============
		*/
		public FilePath( string filePath, PathType type ) {
			ArgumentException.ThrowIfNullOrEmpty( filePath );

			switch ( type ) {
				case PathType.Native:
					OSPath = filePath;
					GodotPath = ProjectSettings.LocalizePath( OSPath );
					_hashCode = GodotPath.GetHashCode();
					break;
				case PathType.User:
				case PathType.Resource:
					GodotPath = filePath;
					OSPath = ProjectSettings.GlobalizePath( GodotPath );
					_hashCode = OSPath.GetHashCode();
					break;
				default:
					throw new ArgumentOutOfRangeException( $"Path type '{type}' isn't a valid PathType" );
			}
			Type = type;
		}

		/*
		===============
		GetFileName
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public string GetFileName() => GodotPath[ GodotPath.LastIndexOf( '/' ).. ];

		/*
		===============
		GetHashCode
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override int GetHashCode() => _hashCode;

		/*
		===============
		FromUserPath
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static FilePath FromUserPath( string path ) => new FilePath( path, PathType.User );

		/*
		===============
		FromResourcePath
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static FilePath FromResourcePath( string path ) => new FilePath( path, PathType.Resource );

		/*
		===============
		FromOSPath
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static FilePath FromOSPath( string path ) => new FilePath( path, PathType.Native );

		/*
		===============
		FromNative
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static FilePath FromNative( string path ) => new FilePath( path, PathType.Native );

		/*
		===============
		operator string
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static implicit operator string( FilePath path ) => path.Type switch {
			PathType.Native => path.OSPath,
			PathType.Resource or PathType.User => path.GodotPath,
			_ => throw new IndexOutOfRangeException( nameof( path.Type ) )
		};
	};
};