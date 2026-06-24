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
using System.Collections.Concurrent;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Logger;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.Repositories;
using Nomad.Save.Private.ValueObjects;
using Nomad.Save.Exceptions;

namespace Nomad.Save.Private.Services
{
	/*
	===================================================================================

	SaveReaderService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveReaderService : ISaveReaderService
	{
		public int SectionCount => _sections.Count;

		private readonly ConcurrentDictionary<string, SaveSectionReader> _sections;

		private readonly SlotRepository _slotRepository;
		private readonly SaveConfig _config;

		private readonly IFileSystem _fileSystem;
		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;

		/*
		===============
		SaveReaderService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="config"></param>
		/// <param name="slotRepository"></param>
		/// <param name="fileSystem"></param>
		/// <param name="logger"></param>
		public SaveReaderService( SaveConfig config, SlotRepository slotRepository, IFileSystem fileSystem, ILoggerService logger )
		{
			_slotRepository = slotRepository ?? throw new ArgumentNullException( nameof( slotRepository ) );
			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_config = config ?? throw new ArgumentNullException( nameof( config ) );

			_sections = new ConcurrentDictionary<string, SaveSectionReader>();

			_category = logger.CreateCategory( nameof( SaveReaderService ), LogLevel.Info, true );
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

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		void ISaveReaderService.Load( string name )
		{
			_sections.Clear();

			_category.PrintLine( $"Loading save data from {name}..." );

			if ( !_slotRepository.TryGetSaveFile( name, out string filepath ) ) {
				_category.PrintError( $"SaveReaderService.Load: couldn't open save file '{name}'!" );
				return;
			}

			_category.PrintLine( $"Loading save data from {filepath}..." );

			using var reader = _fileSystem.OpenRead( new MemoryFileReadConfig { FilePath = filepath, MaxCapacity = 8 * 1024 * 1024 } ) as IMemoryReadStream;
			if ( reader == null ) {
				_category.PrintError( $"SaveReaderService.Load: couldn't open save file '{filepath}'!" );
				return;
			}
			var header = SaveHeader.Deserialize( reader, out bool magicMatches );

			if ( _config.DebugLogging ) {
				_category.PrintLine( "Got save file metadata:" );
				_category.PrintLine( $"\tMagic: {Constants.HEADER_MAGIC}" );
				_category.PrintLine( $"\tSectionCount: {header.SectionCount}" );
				_category.PrintLine( $"\tSaveName: {header.Name}" );
				_category.PrintLine( $"\tChecksum64: {header.Checksum.Value}" );
				_category.PrintLine( $"\tGameVersion: {header.Version.ToInt()}" );
			}

			if ( header.SectionCount < 0 ) {
				throw new SaveFileCorruptException( reader.Position, "Invalid section count" );
			}
			for ( int i = 0; i < header.SectionCount; i++ ) {
				var section = new SaveSectionReader( _config, i, reader, _category );
				_sections[section.Name] = section;
			}

			_category.PrintLine( "...Finished loading save data" );
		}

		/*
		===============
		FindSection
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sectionName"></param>
		/// <returns></returns>
		public ISaveSectionReader? FindSection( string sectionName )
		{
			if ( !_sections.TryGetValue( sectionName, out var section ) ) {
				_category.PrintError( $"SaveReaderService.FindSection: section '{sectionName}' not found!" );
				return null;
			}
			return section;
		}
	};
};
