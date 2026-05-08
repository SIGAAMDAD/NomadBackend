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
using System.IO;
using System.Runtime.CompilerServices;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Streams;
using Nomad.Save.Private.ValueObjects;

namespace Nomad.Save.Private.Services
{
	/*
	===================================================================================

	AtomicWriterService

	===================================================================================
	*/
	/// <summary>
	/// Finalizes save data through a temp-file replacement flow.
	/// </summary>
	/// <remarks>
	/// The atomic writer is responsible for committing a completed temp save file to the
	/// primary save path. If a primary save already exists, it is backed up before
	/// replacement.
	/// </remarks>

	internal sealed class AtomicWriterService
	{
		private readonly IFileSystem _fileSystem;
		private readonly BackupService _backupService;

		/*
		===============
		AtomicWriterService
		===============
		*/
		/// <summary>
		/// Creates a new atomic writer service.
		/// </summary>
		/// <param name="engineService">Engine service.</param>
		/// <param name="fileSystem">File-system service.</param>
		/// <param name="backupService">Backup service.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public AtomicWriterService( IEngineService engineService, IFileSystem fileSystem, BackupService backupService )
		{
			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_backupService = backupService ?? throw new ArgumentNullException( nameof( backupService ) );
		}

		/*
		===============
		GetAtomicPathName
		===============
		*/
		/// <summary>
		/// Creates a temporary save path.
		/// </summary>
		/// <returns>Temporary path.</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static string GetAtomicPathName()
		{
			return Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString( "N" ) + ".tmp" );
		}

		/*
		===============
		GetAtomicPathName
		===============
		*/
		/// <summary>
		/// Creates a temporary save path near the destination file.
		/// </summary>
		/// <param name="destinationFileName">Destination save path.</param>
		/// <returns>Temporary path.</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static string GetAtomicPathName( string destinationFileName )
		{
			if ( string.IsNullOrWhiteSpace( destinationFileName ) ) {
				return GetAtomicPathName();
			}

			string? directory = Path.GetDirectoryName( destinationFileName );
			if ( string.IsNullOrWhiteSpace( directory ) ) {
				return GetAtomicPathName();
			}

			return Path.Combine( directory, Guid.NewGuid().ToString( "N" ) + ".tmp" );
		}

		/*
		===============
		FinalizeSaveData
		===============
		*/
		/// <summary>
		/// Finalizes a save by backing up the old primary save and replacing it with the
		/// completed temporary save file.
		/// </summary>
		/// <param name="fileName">Final primary save path.</param>
		/// <param name="memoryWriter">Temporary save writer.</param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="FileNotFoundException"></exception>
		/// <exception cref="IOException"></exception>
		public void FinalizeSaveData( string fileName, IMemoryFileWriteStream memoryWriter )
		{
			if ( string.IsNullOrWhiteSpace( fileName ) ) {
				throw new ArgumentException( "Save file name cannot be null or empty.", nameof( fileName ) );
			}
			ArgumentGuard.ThrowIfNull( memoryWriter, nameof( memoryWriter ) );

			string tempFileName = memoryWriter.FilePath;

			try {
				memoryWriter.Flush();
				memoryWriter.Dispose();

				if ( string.IsNullOrWhiteSpace( tempFileName ) ) {
					throw new IOException( "Atomic save temp path is null or empty." );
				}
				if ( !_fileSystem.FileExists( tempFileName ) ) {
					throw new FileNotFoundException( "Atomic save temp file was not written.", tempFileName );
				}

				EnsureDestinationDirectory( fileName );

				if ( _fileSystem.FileExists( fileName ) ) {
					if ( !_backupService.TryCreateBackup( fileName, out BackupData backup ) ) {
						throw new IOException( $"Failed to create save backup before replacing '{fileName}'." );
					}
				}

				_fileSystem.ReplaceFile( tempFileName, fileName, null );

				if ( !_fileSystem.FileExists( fileName ) ) {
					throw new IOException( $"Atomic save replacement failed. Final save file '{fileName}' was not created." );
				}
			} finally {
				TryDeleteTempFile( tempFileName );
			}
		}

		/*
		===============
		EnsureDestinationDirectory
		===============
		*/
		/// <summary>
		/// Ensures the destination save directory exists.
		/// </summary>
		/// <param name="fileName">Final save path.</param>
		private void EnsureDestinationDirectory( string fileName )
		{
			string? directory = Path.GetDirectoryName( fileName );

			if ( !string.IsNullOrWhiteSpace( directory ) && !_fileSystem.DirectoryExists( directory ) ) {
				_fileSystem.CreateDirectory( directory );
			}
		}

		/*
		===============
		TryDeleteTempFile
		===============
		*/
		/// <summary>
		/// Attempts to delete a temporary save file.
		/// </summary>
		/// <param name="tempFileName">Temporary file path.</param>
		private void TryDeleteTempFile( string tempFileName )
		{
			if ( string.IsNullOrWhiteSpace( tempFileName ) ) {
				return;
			}

			try {
				if ( _fileSystem.FileExists( tempFileName ) ) {
					_fileSystem.DeleteFile( tempFileName );
				}
			} catch {
				// Suppress cleanup errors to preserve the original exception.
			}
		}
	};
};
