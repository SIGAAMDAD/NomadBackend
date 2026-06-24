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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.ValueObjects;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Private.Repositories
{
	/*
	===================================================================================

	SlotRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SlotRepository : IDisposable
	{
		private readonly Dictionary<string, SaveSlot> _saveSlots = new Dictionary<string, SaveSlot>();

		private readonly IFileSystem _fileSystem;
		private readonly ILoggerCategory _category;

		private readonly FileSystemWatcher _fileWatcher;

		private readonly SaveConfig _config;

		private bool _isDisposed = false;

		/*
		===============
		SlotRepository
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fileSystem"></param>
		/// <param name="logger"></param>
		/// <param name="config"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SlotRepository( IFileSystem fileSystem, ILoggerService logger, SaveConfig config )
		{
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_config = config ?? throw new ArgumentNullException( nameof( config ) );
			_category = logger.CreateCategory(
				nameof( SlotRepository ),
				LogLevel.Info,
				true
			);

			_fileWatcher = new FileSystemWatcher( _config.DataPath, "*.ngd" );
			_fileWatcher.Changed += OnSaveFileChanged;
			_fileWatcher.Deleted += OnSaveFileChanged;
			_fileWatcher.Renamed += OnSaveFileChanged;

			RefreshSlots();
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
			if ( _isDisposed ) {
				return;
			}

			_category?.Dispose();

			_fileWatcher.Changed -= OnSaveFileChanged;
			_fileWatcher.Deleted -= OnSaveFileChanged;
			_fileWatcher.Renamed -= OnSaveFileChanged;
			_fileWatcher.Dispose();

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		RemoveSaveFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		public void RemoveSaveFile( string name )
		{
			if ( !_saveSlots.TryGetValue( name, out SaveSlot slot ) ) {
				return;
			}

			_category.PrintLine( $"" );
			_fileSystem.DeleteFile( slot.FileName );
			_saveSlots.Remove( name );
		}

		/*
		===============
		AddSaveFile
		===============
		*/
		/// <summary>
		/// Checks the save slot cache to see if we already have the slot indexed, if not,
		/// it's added.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="autoSave"></param>
		/// <returns></returns>
		public string AddSaveFile( string name, bool autoSave = false )
		{
			DateTime lastAccessTime = DateTime.UtcNow;
			SaveFileMetadata metadata;

			if ( _saveSlots.TryGetValue( name, out var slotData ) ) {
				metadata = slotData.Metadata with {
					LastAccessTime = lastAccessTime
				};
			} else {
				// ensure we don't have any issues where the lastAccessTime is before the creationTime
				DateTime creationTime = lastAccessTime;

				metadata = new SaveFileMetadata(
					name,
					0,
					lastAccessTime,
					creationTime
				);
			}

			string filePath = Path.Combine( _config.DataPath, SaveSlot.CalculateFileName( autoSave, metadata ) );
			_saveSlots[name] = new SaveSlot( filePath, metadata );
			return filePath;
		}

		/*
		===============
		TryGetSaveFile
		===============
		*/
		/// <summary>
		/// Finds the existing save file for the logical slot name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public bool TryGetSaveFile( string name, out string filePath )
		{
			if ( TryGetCachedSaveFile( name, out filePath ) ) {
				return true;
			}

			RefreshSlots();
			return TryGetCachedSaveFile( name, out filePath );
		}

		/*
		===============
		TryGetCachedSaveFile
		===============
		*/
		/// <summary>
		/// Finds a save file in the current slot cache without touching the filesystem.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="filePath"></param>
		/// <returns></returns>
		private bool TryGetCachedSaveFile( string name, out string filePath )
		{
			if ( _saveSlots.TryGetValue( name, out SaveSlot slotData ) && !string.IsNullOrWhiteSpace( slotData.FileName ) ) {
				filePath = slotData.FileName;
				return true;
			}

			filePath = string.Empty;
			return false;
		}

		/*
		===============
		GetMetadataList
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<SaveFileMetadata> GetMetadataList()
		{
			SaveFileMetadata[] metadata = new SaveFileMetadata[_saveSlots.Count];
			int index = 0;

			foreach ( var slot in _saveSlots ) {
				metadata[index++] = slot.Value.Metadata;
			}

			return metadata;
		}

		/*
		===============
		RefreshSlots
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void RefreshSlots()
		{
			_saveSlots.Clear();

			var slots = _fileSystem.GetFiles( _config.DataPath, "*.ngd", false );

			for ( int i = 0; i < slots.Count; i++ ) {
				using var reader = _fileSystem.OpenRead( new FileReadConfig { FilePath = slots[i] } );

				if ( reader == null || reader is not IFileReadStream fileReader ) {
					_category.PrintError( $"Error opening save data file '{slots[i]}'!" );
					continue;
				}

				_category.PrintLine( $"Adding save file '{slots[i]}' to data cache..." );

				var fileInfo = new FileInfo( fileReader.FilePath );

				SaveHeader header = SaveHeader.Deserialize(
					fileReader,
					out bool magicMatches
				);

				if ( !magicMatches ) {
					_category.PrintError( $"Invalid header magic in save data file '{fileReader.FilePath}'!" );
					continue;
				}

				_saveSlots[header.Name] = new SaveSlot(
					slots[i],
					new SaveFileMetadata(
						SaveName: header.Name,
						FileSize: reader.Length,
						LastAccessTime: fileInfo.LastAccessTimeUtc,
						CreationTime: fileInfo.CreationTimeUtc
					)
				);
			}
		}

		/*
		===============
		OnSaveFileChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnSaveFileChanged( object sender, FileSystemEventArgs e )
		{
			if ( ( e.ChangeType & ( WatcherChangeTypes.Changed | WatcherChangeTypes.Deleted | WatcherChangeTypes.Renamed ) ) != 0 ) {
				// something has changed, we don't know what, but refresh either way.
				RefreshSlots();
			}
		}
	};
};
