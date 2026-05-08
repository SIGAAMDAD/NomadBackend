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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nomad.Core.Logger;

namespace Nomad.FileSystem.Private.Services
{
	/*
	===================================================================================
	
	RecursiveFileSearcher
	
	===================================================================================
	*/
	/// <summary>
	/// A Quake 3‑style file search system that searches through multiple base directories
	/// in priority order. Files are located by exact relative paths only.
	/// </summary>

	internal sealed class RecursiveFileSearcher : IDisposable
	{
		/// <summary>
		/// Gets all search directories in their current priority order.
		/// </summary>
		public IReadOnlyList<string> SearchDirectories => _searchDirectories.AsReadOnly();

		private readonly List<string> _searchDirectories = new List<string>();
		private readonly bool _ignoreCase;
		private readonly bool _useIndex;
		private readonly Dictionary<string, List<string>> _fileIndex; // relative path -> full paths (one per base dir)
		private readonly FileSystemWatcher _indexWatcher; // optional, to keep index fresh
		private bool _isDisposed = false;

		private readonly ILoggerCategory _category;

		/*
		===============
		RecursiveFileSearcher
		===============
		*/
		/// <summary>
		/// Initializes a new instance of the <see cref="RecursiveFileSearcher"/> class.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="ignoreCase">If true, file lookups are case‑insensitive (recommended for cross‑platform).</param>
		/// <param name="useIndex">If true, builds an in‑memory index of all files for faster lookups (may impact startup time).</param>
		public RecursiveFileSearcher( ILoggerCategory category, bool? ignoreCase = null, bool useIndex = false )
		{
			_ignoreCase = ignoreCase ??
#if WINDOWS
				true;
#else
				false;
#endif
			_useIndex = useIndex;
			_category = category;
			if ( _useIndex ) {
				_fileIndex = new Dictionary<string, List<string>>( _ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal );
				_indexWatcher = new FileSystemWatcher();
				// Optional: set up a FileSystemWatcher to keep index fresh (not implemented here for simplicity)
			}
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_indexWatcher?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		AddSearchDirectory
		===============
		*/
		/// <summary>
		/// Adds a search directory with optional priority.
		/// </summary>
		/// <param name="directory">Directory to add.</param>
		/// <param name="highPriority">If true, adds to beginning of search list; otherwise appends.</param>
		/// <exception cref="ArgumentException">Thrown if directory is null or empty.</exception>
		/// <exception cref="DirectoryNotFoundException">Thrown if directory does not exist.</exception>
		public void AddSearchDirectory( string directory, bool highPriority = false )
		{
			if ( string.IsNullOrWhiteSpace( directory ) ) {
				throw new ArgumentException( "Directory cannot be null or empty.", nameof( directory ) );
			}

			string normalizedPath = NormalizeDirectoryPath( directory );
			if ( !Directory.Exists( normalizedPath ) ) {
				throw new DirectoryNotFoundException( $"Directory not found: {directory}" );
			}

			if ( highPriority ) {
				_searchDirectories.Insert( 0, normalizedPath );
			} else {
				_searchDirectories.Add( normalizedPath );
			}

			if ( _useIndex ) {
				IndexDirectory( normalizedPath );
			}
		}

		/*
		===============
		FindFile
		===============
		*/
		/// <summary>
		/// Finds a file by its exact relative path. Returns the full path of the first match
		/// according to search directory priority, or null if not found.
		/// </summary>
		/// <param name="relativePath">Relative path to the file (e.g., "Textures/wood.png").</param>
		/// <returns>Full path to the found file, or null.</returns>
		/// <exception cref="ArgumentException">Thrown if relativePath is null or empty.</exception>
		public string? FindFile( string relativePath )
		{
			if ( string.IsNullOrWhiteSpace( relativePath ) ) {
				throw new ArgumentException( "Relative path cannot be null or empty.", nameof( relativePath ) );
			}

			string cleanPath = NormalizePath( relativePath );

			if ( _useIndex ) {
				return FindFileFromIndex( cleanPath );
			}

			// Exact match search without index
			foreach ( string baseDir in _searchDirectories ) {
				foreach ( string candidatePath in GetRelativePathCandidates( baseDir, cleanPath ) ) {
					if ( string.IsNullOrEmpty( candidatePath ) ) {
						continue;
					}

					string? fullPath = GetSafeFullPath( baseDir, candidatePath );
					if ( fullPath != null && FileExists( fullPath ) ) {
						return NormalizePath( fullPath );
					}
				}
			}
			return null;
		}

		/*
		===============
		FindDirectory
		===============
		*/
		/// <summary>
		/// Finds a directory by its relative path or by a path already rooted in a registered search directory.
		/// </summary>
		/// <param name="path">The directory path to resolve.</param>
		/// <returns>The resolved full path when found; otherwise <see langword="null"/>.</returns>
		public string? FindDirectory( string path )
		{
			if ( string.IsNullOrWhiteSpace( path ) ) {
				throw new ArgumentException( "Directory path cannot be null or empty.", nameof( path ) );
			}

			if ( Path.IsPathRooted( path ) && Directory.Exists( path ) ) {
				return NormalizePath( Path.GetFullPath( path ) );
			}

			string cleanPath = NormalizePath( path ).TrimEnd( Path.DirectorySeparatorChar );
			foreach ( string baseDir in _searchDirectories ) {
				foreach ( string candidatePath in GetRelativePathCandidates( baseDir, cleanPath ) ) {
					if ( string.IsNullOrEmpty( candidatePath ) ) {
						return NormalizePath( baseDir.TrimEnd( Path.DirectorySeparatorChar ) );
					}

					string? fullPath = GetSafeFullPath( baseDir, candidatePath );
					if ( fullPath != null && Directory.Exists( fullPath ) ) {
						return NormalizePath( fullPath );
					}
				}
			}

			return Directory.Exists( path )
				? NormalizePath( Path.GetFullPath( path ) )
				: null;
		}

		/*
		===============
		FindAllFiles
		===============
		*/
		/// <summary>
		/// Finds all occurrences of a file across all search directories, ordered by priority.
		/// </summary>
		/// <param name="relativePath">Relative path to search for.</param>
		/// <returns>List of full paths (may be empty).</returns>
		public List<string> FindAllFiles( string relativePath )
		{
			if ( string.IsNullOrWhiteSpace( relativePath ) ) {
				return new List<string>();
			}

			string cleanPath = NormalizePath( relativePath );
			var results = new List<string>();

			if ( _useIndex ) {
				results.AddRange( FindAllFilesFromIndex( cleanPath ) );
				return results;
			}

			foreach ( string baseDir in _searchDirectories ) {
				foreach ( string candidatePath in GetRelativePathCandidates( baseDir, cleanPath ) ) {
					if ( string.IsNullOrEmpty( candidatePath ) ) {
						continue;
					}

					string? fullPath = GetSafeFullPath( baseDir, candidatePath );
					if ( fullPath != null && FileExists( fullPath ) ) {
						results.Add( fullPath );
						break;
					}
				}
			}
			return results;
		}

		/*
		===============
		FindFiles
		===============
		*/
		/// <summary>
		/// Finds files matching a pattern (e.g., "*.png") in a specific relative subdirectory.
		/// Returns a dictionary mapping each base directory to the list of matching files.
		/// </summary>
		/// <param name="searchDir"></param>
		/// <param name="searchPattern">Search pattern (supports * and ?).</param>
		/// <returns>Dictionary with results per base directory.</returns>
		public List<string>? FindFiles( string searchDir, string searchPattern )
		{
			if ( !Directory.Exists( searchDir ) ) {
				return null;
			}

			try {
				return GetFilesAsList( searchDir, searchPattern, false );
			} catch ( UnauthorizedAccessException ) {
				_category.PrintWarning( $"Attempted access to directory {searchDir} (UnauthorizedAccess) denied." );
			} catch ( PathTooLongException ) {
				_category.PrintWarning( $"Attempted access to directory {searchDir} denied, path was too long." );
			}
			// Other IO exceptions could be caught as needed
			return null;
		}

		/*
		===============
		FindFileWithExtensions
		===============
		*/
		/// <summary>
		/// Finds a file by trying multiple file extensions.
		/// </summary>
		/// <param name="relativePathWithoutExtension">Relative path without extension.</param>
		/// <param name="extensions">Extensions to try (e.g., ".json", ".yaml").</param>
		/// <returns>Full path of first matching file, or null.</returns>
		public string? FindFileWithExtensions( string relativePathWithoutExtension, params string[] extensions )
		{
			foreach ( string ext in extensions ) {
				string? path = FindFile( relativePathWithoutExtension + ext );
				if ( path != null ) {
					return path;
				}
			}
			return null;
		}

		/*
		===============
		NormalizeDirectoryPath
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string NormalizeDirectoryPath( string path )
		{
			string fullPath = Path.GetFullPath( path );
			if ( !fullPath.EndsWith( Path.DirectorySeparatorChar.ToString(), StringComparison.CurrentCultureIgnoreCase ) ) {
				fullPath += Path.DirectorySeparatorChar;
			}
			return fullPath;
		}

		/*
		===============
		NormalizePath
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string NormalizePath( string path )
		{
			return path.Trim()
				.Replace( '/', Path.DirectorySeparatorChar )
				.Replace( '\\', Path.DirectorySeparatorChar );
		}

		/*
		===============
		GetSafeFullPath
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseDir"></param>
		/// <param name="relativePath"></param>
		/// <returns></returns>
		private string? GetSafeFullPath( string baseDir, string relativePath )
		{
			try {
				string combined = Path.Combine( baseDir, relativePath );
				string fullPath = Path.GetFullPath( combined ); // resolves .. and .
				if ( fullPath.StartsWith( baseDir, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal ) ) {
					return fullPath;
				}
			} catch ( Exception ) // PathTooLongException, ArgumentException, etc.
			  {
				// If the path is invalid or escapes, treat as not found
			}
			return null;
		}

		/*
		===============
		FileExists
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private bool FileExists( string path )
		{
			try {
				return _ignoreCase
					? Directory.EnumerateFiles( Path.GetDirectoryName( path ) ?? string.Empty, Path.GetFileName( path ) )
							   .Any( f => string.Equals( f, path, StringComparison.OrdinalIgnoreCase ) )
					: File.Exists( path );
			} catch {
				return false;
			}
		}

		/*
		===============
		GetRelativePathCandidates
		===============
		*/
		/// <summary>
		/// Generates compatible relative-path candidates for a registered search directory.
		/// This allows callers to pass either paths relative to the search root (for example
		/// "Config/Bindings/file.json") or project-style paths that repeat the search-root
		/// folder name (for example "Assets/Config/Bindings/file.json").
		/// </summary>
		/// <param name="baseDir"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		private IEnumerable<string> GetRelativePathCandidates( string baseDir, string path )
		{
			yield return path;

			string searchRootName = Path.GetFileName( baseDir.TrimEnd( Path.DirectorySeparatorChar ) );
			if ( string.IsNullOrEmpty( searchRootName ) ) {
				yield break;
			}

			StringComparison comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			if ( path.Equals( searchRootName, comparison ) ) {
				yield return string.Empty;
				yield break;
			}

			string prefix = searchRootName + Path.DirectorySeparatorChar;
			if ( path.StartsWith( prefix, comparison ) ) {
				yield return path.Substring( prefix.Length );
			}
		}

		/*
		===============
		IndexDirectory
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="directory"></param>
		private void IndexDirectory( string directory )
		{
			// Build index of all files relative to this directory
			foreach ( string file in Directory.EnumerateFiles( directory, "*", SearchOption.AllDirectories ) ) {
				string relative = NormalizePath( Path.GetRelativePath( directory, file ) );
				if ( !_fileIndex.TryGetValue( relative, out var list ) ) {
					list = new List<string>();
					_fileIndex[relative] = list;
				}
				list.Add( NormalizePath( file ) );
			}
		}

		/*
		===============
		FindFileFromIndex
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="relativePath"></param>
		/// <returns></returns>
		private string? FindFileFromIndex( string relativePath )
		{
			foreach ( string baseDir in _searchDirectories ) {
				foreach ( string candidatePath in GetRelativePathCandidates( baseDir, relativePath ) ) {
					if ( string.IsNullOrEmpty( candidatePath ) ) {
						continue;
					}

					if ( _fileIndex.TryGetValue( candidatePath, out var fullPaths ) ) {
						string? match = fullPaths.FirstOrDefault( path =>
							path.StartsWith( baseDir, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal ) );
						if ( match != null ) {
							return match;
						}
					}
				}
			}
			return null;
		}

		/*
		===============
		FindAllFilesFromIndex
		===============
		*/
		/// <summary>
		/// Resolves every indexed match for a relative path while respecting search directory order.
		/// </summary>
		/// <param name="relativePath"></param>
		/// <returns></returns>
		private List<string> FindAllFilesFromIndex( string relativePath )
		{
			var results = new List<string>();

			foreach ( string baseDir in _searchDirectories ) {
				foreach ( string candidatePath in GetRelativePathCandidates( baseDir, relativePath ) ) {
					if ( string.IsNullOrEmpty( candidatePath ) ) {
						continue;
					}

					if ( !_fileIndex.TryGetValue( candidatePath, out var fullPaths ) ) {
						continue;
					}

					string? match = fullPaths.FirstOrDefault( path =>
						path.StartsWith( baseDir, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal ) );
					if ( match != null ) {
						results.Add( match );
						break;
					}
				}
			}

			return results;
		}

		/*
		===============
		GetFilesAsList
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="rootDir"></param>
		/// <param name="searchPattern"></param>
		/// <param name="skipReparsePoints"></param>
		/// <returns></returns>
		private List<string> GetFilesAsList( string rootDir, string searchPattern, bool skipReparsePoints = true )
		{
			var result = new List<string>();
			var stack = new Stack<string>();

			if ( !Directory.Exists( rootDir ) ) {
				_category.PrintLine( $"Directory {rootDir} doesn't exist." );
				return result;
			}

			stack.Push( rootDir );

			while ( stack.Count > 0 ) {
				var dir = stack.Pop();

				try {
					// Optionally skip reparse points (symlinks/junctions) to prevent infinite loops
					if ( skipReparsePoints ) {
						var attrs = File.GetAttributes( dir );
						if ( (attrs & FileAttributes.ReparsePoint) != 0 ) {
							continue;
						}
					}

					// Add files in the current directory
					var files = Directory.GetFiles( dir, searchPattern, SearchOption.TopDirectoryOnly );
					if ( files.Length > 0 ) {
						result.AddRange( files );
					}

					// Traverse subdirectories
					var subDirs = Directory.GetDirectories( dir, "*", SearchOption.TopDirectoryOnly );
					for ( int i = 0; i < subDirs.Length; i++ ) {
						// You can insert custom rules here (e.g., exclude hidden/system)
						stack.Push( subDirs[i] );
					}
				} catch ( UnauthorizedAccessException ) {
					// Skip directories you can't access
					continue;
				} catch ( DirectoryNotFoundException ) {
					// Directory may have been removed during traversal
					continue;
				} catch ( PathTooLongException ) {
					// Skip problematic paths
					continue;
				}
			}
			return result;
		}
	};
};
