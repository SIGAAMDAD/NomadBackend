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
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.Save.Interfaces;
using Nomad.Save.Exceptions;
using Nomad.Save.Services;
using Nomad.Save.ValueObjects;
using Nomad.Save.Private.Entities;
using Nomad.Core.Exceptions;
using Nomad.Save.Private.Registries;
using Nomad.Core.Engine.Services;
using Nomad.Save.Private.Repositories;
using Nomad.Core.CVars;
using Nomad.CVars;

namespace Nomad.Save.Private.Services
{
	/*
	===================================================================================

	SaveDataProvider

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>
	/// TODO: refactor this is doing WAY too much.

	internal sealed class SaveDataProvider : ISaveDataProvider
	{
		private readonly ISaveWriterService _writerService;
		private readonly ISaveReaderService _readerService;

		private readonly IFileSystem _vfs;

		private readonly ILoggerCategory _category;

		private readonly SlotRepository _slotRepository;
		private readonly BackupService _backupService;
		private readonly AtomicWriterService _atomicWriter;

		private volatile SaveConfig _config;

		private readonly ICVar<bool> _autoSaveEnabled;
		private readonly ICVar<int> _autoSaveInterval;

		public IGameEvent<SaveBeginEventArgs> SaveBegin => _saveBegin;
		private readonly IGameEvent<SaveBeginEventArgs> _saveBegin;

		public IGameEvent<LoadBeginEventArgs> LoadBegin => _loadBegin;
		private readonly IGameEvent<LoadBeginEventArgs> _loadBegin;

		private bool _isDisposed = false;

		/*
		===============
		SaveDataProvider
		===============
		*/
		/// <summary>
		/// Creates a new SaveDataProvider object.
		/// </summary>
		/// <param name="engineService"></param>
		/// <param name="eventFactory"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="fileSystem"></param>
		/// <param name="logger"></param>
		public SaveDataProvider( IEngineService engineService, IGameEventRegistryService eventFactory, ICVarSystemService cvarSystem, IFileSystem fileSystem, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( engineService, nameof( engineService ) );
			ArgumentGuard.ThrowIfNull( eventFactory, nameof( eventFactory ) );
			ArgumentGuard.ThrowIfNull( cvarSystem, nameof( cvarSystem ) );
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_saveBegin = eventFactory
				.GetEvent<SaveBeginEventArgs>(
					SaveBeginEventArgs.Name,
					SaveBeginEventArgs.NameSpace
				);

			_loadBegin = eventFactory
				.GetEvent<LoadBeginEventArgs>(
					LoadBeginEventArgs.Name,
					LoadBeginEventArgs.NameSpace
				);

			_vfs = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_category = logger.CreateCategory( nameof( Nomad.Save ), LogLevel.Info, true );

			_config = InitConfiguration( engineService, cvarSystem );

			_autoSaveEnabled = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.AUTO_SAVE_ENABLED );
			_autoSaveEnabled.ValueChanged.Subscribe( OnAutoSaveEnabledChanged );

			_autoSaveInterval = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.AUTO_SAVE_INTERVAL );
			_autoSaveInterval.ValueChanged.Subscribe( OnAutoSaveIntervalChanged );

			_backupService = new BackupService( _config, fileSystem );
			_atomicWriter = new AtomicWriterService( engineService, fileSystem, _backupService );

			_slotRepository = new SlotRepository( fileSystem, logger, _config );
			_readerService = new SaveReaderService( _config, _slotRepository, _vfs, logger );
			_writerService = new SaveWriterService( _config, _atomicWriter, _slotRepository, _vfs, logger );
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

			_readerService?.Dispose();
			_writerService?.Dispose();
			_backupService?.Dispose();
			_slotRepository?.Dispose();
			_category?.Dispose();

			_autoSaveEnabled.ValueChanged.Unsubscribe( OnAutoSaveEnabledChanged );
			_autoSaveInterval.ValueChanged.Unsubscribe( OnAutoSaveIntervalChanged );

			_saveBegin?.Dispose();
			_loadBegin?.Dispose();

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		DeleteSaveFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="saveName"></param>
		public void DeleteSaveFile( string saveName )
		{
			_slotRepository.RemoveSaveFile( saveName );
		}

		/*
		===============
		ListSaveFiles
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<SaveFileMetadata> ListSaveFiles()
		{
			return _slotRepository.GetMetadataList();
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="saveName"></param>
		/// <returns></returns>
		public async Task Load( string saveName )
		{
			try {
				_readerService.Load( saveName );
				_loadBegin.Publish( new LoadBeginEventArgs( _readerService ) );
			} catch ( FieldCorruptException fieldCorrupt ) {
				_category.PrintError( $"Field corruption: [FieldIndex] {fieldCorrupt.FieldIndex}, [FileOffset] {fieldCorrupt.FileOffset}, [Section] {fieldCorrupt.SectionName} - {fieldCorrupt.Message}" );
			} catch ( Exception e ) {
				_category.PrintError( $"Exception caught - {e}" );
			}
		}

		/*
		===============
		Save
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="saveName"></param>
		/// <param name="gameVersion"></param>
		/// <returns></returns>
		public async Task Save( string saveName, GameVersion gameVersion )
		{
			try {
				_writerService.BeginSave( saveName, gameVersion );
				_saveBegin.Publish( new SaveBeginEventArgs( _writerService ) );
				_writerService.EndSave( saveName, gameVersion );
			} catch ( Exception e ) {
				_category.PrintError( $"Exception caught - {e}" );
			}
		}

		/*
		===============
		InitConfiguration
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="engineService"></param>
		/// <param name="cvarSystem"></param>
		/// <exception cref="CVarMissing"></exception>
		private static SaveConfig InitConfiguration( IEngineService engineService, ICVarSystemService cvarSystem )
		{
			SaveCVarRegistry.RegisterCVars( engineService, cvarSystem );

			ICVar<string> dataPath = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.DATA_PATH );

			ICVar<string> backupPath = cvarSystem.GetCVarOrThrow<string>( Constants.CVars.BACKUP_DIRECTORY );
			ICVar<int> maxBackups = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.MAX_BACKUPS );

			ICVar<bool> autoSaveEnabled = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.AUTO_SAVE_ENABLED );
			ICVar<int> autoSaveInterval = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.AUTO_SAVE_INTERVAL );

			ICVar<bool> checksumsEnabled = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.CHECKSUM_ENABLED );
			ICVar<bool> verifyAfterWrite = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.VERIFY_AFTER_WRITE );
			ICVar<bool> logSerializationTree = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.LOG_SERIALIZATION_TREE );
			ICVar<bool> logWriteTimings = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.LOG_WRITE_TIMINGS );
			ICVar<bool> debugLogging = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.DEBUG_LOGGING );

			return new SaveConfig {
				DataPath = dataPath.Value,

				BackupPath = backupPath.Value,
				MaxBackups = maxBackups.Value,

				AutoSaveInterval = autoSaveInterval.Value,
				AutoSave = autoSaveEnabled.Value,

				ChecksumEnabled = checksumsEnabled.Value,
				VerifyAfterWrite = verifyAfterWrite.Value,
				LogSerializationTree = logSerializationTree.Value,
				LogWriteTimings = logWriteTimings.Value,
				DebugLogging = debugLogging.Value
			};
		}

		/*
		===============
		OnAutoSaveIntervalChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnAutoSaveIntervalChanged( in CVarValueChangedEventArgs<int> args )
		{
			_config = _config with { AutoSaveInterval = args.NewValue };
		}

		/*
		===============
		OnAutoSaveEnabledChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnAutoSaveEnabledChanged( in CVarValueChangedEventArgs<bool> args )
		{
			_config = _config with { AutoSave = args.NewValue };
		}
	};
};
