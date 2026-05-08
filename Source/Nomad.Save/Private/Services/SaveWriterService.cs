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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using Nomad.Save.Exceptions;
using Nomad.Save.Interfaces;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.Repositories;
using Nomad.Save.Private.ValueObjects;
using Nomad.Save.ValueObjects;

namespace Nomad.Save.Private.Services
{
	/*
	===================================================================================

	SaveWriterService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SaveWriterService : ISaveWriterService
	{
		public int SectionCount => _sections.Count;

		private readonly ConcurrentDictionary<string, SaveSectionWriter> _sections = new ConcurrentDictionary<string, SaveSectionWriter>();

		private IMemoryFileWriteStream? _writer;

		private readonly SaveConfig _config;

		private readonly SlotRepository _slotRepository;
		private readonly IFileSystem _fileSystem;
		private readonly ILoggerCategory _category;

		private readonly AtomicWriterService _atomicWriter;

		private bool _isDisposed = false;

		/*
		===============
		SaveWriterService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="config"></param>
		/// <param name="atomicWriter"></param>
		/// <param name="slotRepository"></param>
		/// <param name="fileSystem"></param>
		/// <param name="logger"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SaveWriterService( SaveConfig config, AtomicWriterService atomicWriter, SlotRepository slotRepository, IFileSystem fileSystem, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );
			ArgumentGuard.ThrowIfNull( fileSystem, nameof( fileSystem ) );
			ArgumentGuard.ThrowIfNull( slotRepository, nameof( slotRepository ) );
			ArgumentGuard.ThrowIfNull( config, nameof( config ) );
			ArgumentGuard.ThrowIfNull( atomicWriter, nameof( atomicWriter ) );

			_slotRepository = slotRepository;
			_fileSystem = fileSystem;
			_config = config;
			_atomicWriter = atomicWriter;

			_category = logger.CreateCategory( nameof( SaveWriterService ), LogLevel.Info, true );
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
				_category?.Dispose();
				_slotRepository?.Dispose();
				_writer?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Save
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="gameVersion"></param>
		void ISaveWriterService.BeginSave( string name, GameVersion gameVersion )
		{
			string filepath = _slotRepository.AddSaveFile( name, false );

			_writer = _fileSystem.OpenWrite( new MemoryFileWriteConfig { FilePath = AtomicWriterService.GetAtomicPathName() } )
				as IMemoryFileWriteStream ?? throw new CreateSaveFileFailed( filepath );

			_category.PrintLine( $"Writing save data to {name} at {filepath}..." );
			{
				SaveHeader header = new SaveHeader( name, gameVersion, _sections.Count, Checksum.Empty );
				header.Serialize( _writer );
			}
		}

		/*
		===============
		EndSave
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="gameVersion"></param>
		void ISaveWriterService.EndSave( string name, GameVersion gameVersion )
		{
			if ( _writer == null ) {
				throw new InvalidOperationException( "EndSave called before a save operation was started." );
			}

			List<SaveSectionWriter> sections = _sections.Values.ToList();
			int sectionCount = sections.Count;
			foreach ( SaveSectionWriter section in sections ) {
				section.Dispose();
			}
			_sections.Clear();

			if ( _writer.Buffer == null ) {
				throw new InvalidOperationException( "EndSave operation called on a null write buffer!" );
			}
			Checksum checksum = Checksum.Compute( _writer.Buffer.Span );

			_writer.Seek( 0, System.IO.SeekOrigin.Begin );
			SaveHeader header = new SaveHeader( name, gameVersion, sectionCount, checksum );
			header.Serialize( _writer );

			_writer.Seek( 0, System.IO.SeekOrigin.End );

			if ( _config.DebugLogging ) {
				_category.PrintLine( $"\tMagic: {Constants.HEADER_MAGIC}" );
				_category.PrintLine( $"Finalized save data file:" );
				_category.PrintLine( $"\tSectionCount: {header.SectionCount}" );
				_category.PrintLine( $"\tSaveName: {header.Name}" );
				_category.PrintLine( $"\tChecksum64: {header.Checksum.Value}" );
				_category.PrintLine( $"\tGameVersion: {header.Version.ToInt()}" );
			}

			// clear the memory buffer
			string filepath = _slotRepository.AddSaveFile( name, false );
			_atomicWriter.FinalizeSaveData( filepath, _writer );
		}

		/*
		===============
		AddSection
		===============
		*/
		/// <summary>
		/// Creates a new section with the id of <paramref name="sectionId"/> and returns it.
		/// </summary>
		/// <param name="sectionId">The name of the section to add.</param>
		/// <returns>The new write-dedicated section.</returns>
		/// <exception cref="DuplicateSectionException">Thrown if <paramref name="sectionId"/> already exists in the section cache.</exception>
		public ISaveSectionWriter AddSection( string sectionId )
		{
			if ( _writer == null ) {
				throw new InvalidOperationException( "AddSection called before a save operation was started." );
			}
			if ( _sections.ContainsKey( sectionId ) ) {
				throw new DuplicateSectionException( sectionId );
			}
			SaveSectionWriter writer = new SaveSectionWriter( _config, _category, sectionId, _writer );
			_sections[sectionId] = writer;

			if ( _config.LogSerializationTree ) {
				_category.PrintLine( "$\t[Section] (NAME) {sectionId}" );
			}

			return writer;
		}
	};
};
