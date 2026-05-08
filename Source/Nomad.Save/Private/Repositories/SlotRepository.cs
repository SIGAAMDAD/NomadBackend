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
		private readonly ILoggerService _logger;

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
		public SlotRepository( IFileSystem fileSystem, ILoggerService logger, SaveConfig config )
		{
			_fileSystem = fileSystem;
			_logger = logger;
			_config = config;

			_fileWatcher = new FileSystemWatcher( _config.DataPath, "*.ngd" );
			_fileWatcher.Changed += OnSaveFileChanged;

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
			if ( !_isDisposed ) {
				_fileWatcher?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
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
			DateTime lastAccessTime = DateTime.Now;
			SaveFileMetadata metadata;
			string fileName = string.Empty;

			if ( _saveSlots.TryGetValue( name, out var slotData ) ) {
				// update the metadata
				metadata = slotData.Metadata;
				_saveSlots[name] = slotData with {
					Metadata = slotData.Metadata with {
						LastAccessYear = lastAccessTime.Year,
						LastAccessMonth = lastAccessTime.Month,
						LastAccessDay = lastAccessTime.Day
					}
				};
			} else {
				// ensure we don't have any issues where the lastAccessTime is before the creationTime
				var creationTime = lastAccessTime;

				metadata = new SaveFileMetadata(
					name,
					0,
					lastAccessTime.Year,
					lastAccessTime.Month,
					lastAccessTime.Day,
					creationTime.Year,
					creationTime.Month,
					creationTime.Day
				);
				_saveSlots.Add( name, new SaveSlot( fileName, metadata ) );
			}

			fileName = SaveSlot.CalculateFileName( autoSave, metadata );
			return $"{_config.DataPath}/{fileName}";
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
			var slots = _fileSystem.GetFiles( _config.DataPath, "*.ngd", false );

			for ( int i = 0; i < slots.Count; i++ ) {
				using var reader = _fileSystem.OpenRead( new FileReadConfig { FilePath = slots[i] } );
				if ( reader == null || reader is not IFileReadStream fileReader ) {
					_logger.PrintError( $"SlotRepository.RefreshSlots: error opening save data file '{slots[i]}'!" );
					continue;
				}

				_logger.PrintLine( $"SlotRepository.RefreshSlots: adding save file '{slots[i]}' to data cache..." );

				var fileInfo = new FileInfo( fileReader.FilePath );
				DateTime lastAccessTime = fileInfo.LastAccessTime;
				DateTime creationTime = fileInfo.CreationTime;

				var header = SaveHeader.Deserialize( fileReader, out bool magicMatches );
				_saveSlots[header.Name] = new SaveSlot(
					slots[i],
					new SaveFileMetadata(
						SaveName: header.Name,
						FileSize: reader.Length,
						LastAccessYear: lastAccessTime.Year,
						LastAccessMonth: lastAccessTime.Month,
						LastAccessDay: lastAccessTime.Day,
						CreationYear: creationTime.Year,
						CreationMonth: creationTime.Month,
						CreationDay: creationTime.Day
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
			if ( e.ChangeType.HasFlag( WatcherChangeTypes.Changed ) ) {
				// something has changed, we don't know what, but refresh either way.
				RefreshSlots();
			}
		}
	};
};
