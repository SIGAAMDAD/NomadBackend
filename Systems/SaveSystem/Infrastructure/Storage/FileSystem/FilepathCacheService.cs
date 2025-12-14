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

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects;
using System.IO;
using System.Threading;
using System.IO.MemoryMappedFiles;
using NomadCore.GameServices;
using System.Linq;
using NomadCore.Domain.Models.ValueObjects;

namespace NomadCore.Systems.SaveSystem.Infrastructure.Storage.FileSystem {
	/*
	===================================================================================
	
	FilepathCacheService

	===================================================================================
	*/
	/// <summary>
	/// Caches all filepaths to reduce string allocation overhead.
	/// </summary>
	
	internal sealed class FilepathCacheService {
		private readonly ConcurrentDictionary<SaveFileId, CachedFilePath> _pathCache;
		private readonly ConcurrentDictionary<string, SaveFileId> _reverseLookup;
		private readonly FileSystemWatcher _watcher;
		private readonly FilePath _cacheFilePath;
		private readonly Timer _flushTimer;
		private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
		private readonly MemoryMappedFile _mmapManager;

		private readonly FilePath _savePath;
		private readonly Dictionary<int, string> _slotPaths = new Dictionary<int, string>();
		private readonly Dictionary<KeyValuePair<int, int>, string> _backupPaths = new Dictionary<KeyValuePair<int, int>, string>();

		/*
		===============
		FilepathCache
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public FilepathCacheService( ICVarSystemService cvarSystem ) {
			var saveLocation = cvarSystem.GetCVar<string>( "save.BasePath" ) ?? throw new Exception( "Missing CVar 'save.BasePath" );

			_pathCache = new ConcurrentDictionary<SaveFileId, CachedFilePath>();
			_reverseLookup = new ConcurrentDictionary<string, SaveFileId>();
			_cacheFilePath = new FilePath( Path.Combine( saveLocation.Value, "savepath_cache.bin" ), PathType.User );

			_mmapManager = MemoryMappedFile.CreateNew( _cacheFilePath.OSPath, 1 * 1024 * 1024 );

			_watcher = new FileSystemWatcher( saveLocation.Value ) {
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
				IncludeSubdirectories = true,
				EnableRaisingEvents = true
			};

			_watcher.Changed += OnFileSystemChanged;
			_watcher.Created += OnFileSystemChanged;
			_watcher.Deleted += OnFileSystemChanged;
			_watcher.Renamed += OnFileSystemChanged;

			_savePath = new FilePath( $"{saveLocation.Value}/", PathType.User );

			_flushTimer = new Timer( FlushCacheToDisk, null, 1, 5 );

			LoadCacheFromDisk();
		}

		/*
		===============
		TryGetPath
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileId"></param>
		/// <param name="fullPath"></param>
		/// <returns></returns>
		public bool TryGetPath( SaveFileId fileId, out string? fullPath ) {
			if ( _pathCache.TryGetValue( fileId, out var cached ) ) {
				if ( File.Exists( cached.Path.OSPath ) ) {
					fullPath = cached.Path.OSPath;
					cached.LastAccessed = DateTime.UtcNow;
					cached.AccessCount++;
					return true;
				} else {
					_pathCache.TryRemove( fileId, out _ );
					_reverseLookup.TryRemove( cached.Path.GodotPath, out _ );
				}
			}
			fullPath = null;
			return false;
		}

		public void AddOrUpdate( SaveFileId fileId, string fullPath ) {
			var cached = new CachedFilePath {
				FileId = fileId,
				Path = new FilePath( fullPath, PathType.User ),
				AddedAt = DateTime.UtcNow,
				LastAccessed = DateTime.UtcNow,
				AccessCount = 1
			};

			_pathCache[ fileId ] = cached;
			_reverseLookup[ fullPath ] = fileId;
		}

		/*
		===============
		Invalidate
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileId"></param>
		public void Invalidate( SaveFileId fileId ) {
			if ( _pathCache.TryGetValue( fileId, out var cached ) ) {
				_reverseLookup.TryRemove( cached.Path.GodotPath, out _ );
			}
		}

		/*
		===============
		InvalidateByPath
		===============
		*/
		public void InvalidateByPath( string fullPath ) {
			if ( _reverseLookup.TryRemove( fullPath, out var fileId ) ) {
				_pathCache.TryRemove( fileId, out _ );
			}
		}

		private void OnFileSystemChanged( object sender, FileSystemEventArgs e ) {
			if ( Directory.Exists( e.FullPath ) ) {
				var toRemove = _reverseLookup.Keys
					.Where( p => p.StartsWith( e.FullPath ) )
					.ToList();
				
				foreach ( var path in toRemove ) {
					
				}
			}
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
		public string GetSlotPath( int slot ) {
			if ( !_slotPaths.TryGetValue( slot, out string? value ) ) {
				value = $"{_savePath}GameData_{slot}.ngd";
				_slotPaths.Add( slot, value );
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
		public string GetBackupPath( int slot, int backupIndex ) {
			var kvp = new KeyValuePair<int, int>( slot, backupIndex );
			if ( !_backupPaths.TryGetValue( kvp, out string? value ) ) {
				value = $"{_savePath}GameData_{slot}.ngd.backup{backupIndex}";
				_backupPaths.Add( kvp, value );
			}
			return value;
		}

		/*
		===============
		LoadCacheFromDisk
		===============
		*/
		private void LoadCacheFromDisk() {
			try {
				using var accessor = _mmapManager.CreateViewAccessor();
				int count = accessor.ReadInt32( 0 );
				int offset = sizeof( int );

				for ( int i = 0; i < count; i++ ) {
					var cached = CachedFilePath.ReadFrom( accessor, ref offset );
					_pathCache[ cached.FileId ] = cached;
					_reverseLookup[ cached.Path.GodotPath ] = cached.FileId;
				}
			}
			catch ( FileNotFoundException ) {
			}
		}

		/*
		===============
		FlushCacheToDisk
		===============
		*/
		private void FlushCacheToDisk( object? state ) {
			_cacheLock.EnterWriteLock();
			try {
				using var accessor = _mmapManager.CreateViewAccessor();
				int offset = 0;
				accessor.Write( offset, _pathCache.Count );
				offset += sizeof( int );

				foreach ( var cached in _pathCache.Values ) {
					cached.WriteTo( accessor, ref offset );
				}

				accessor.Flush();
			}
			finally {
				_cacheLock.ExitWriteLock();
			}
		}
	};
};