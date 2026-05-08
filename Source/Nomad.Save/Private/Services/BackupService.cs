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
using System.Globalization;
using System.IO;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.FileSystem;
using Nomad.Save.Private.Entities;
using Nomad.Save.Private.ValueObjects;

namespace Nomad.Save.Private.Services
{
	/*
	===================================================================================

	BackupService

	===================================================================================
	*/
	/// <summary>
	/// Maintains a rolling idTech-style backup chain for save files.
	/// </summary>
	/// <remarks>
	/// Backups are simple file copies of a known-good primary save. The backup chain is
	/// stored in <see cref="SaveConfig.BackupPath"/> and pruned per logical save name by
	/// <see cref="SaveConfig.MaxBackups"/>.
	/// </remarks>

	internal sealed class BackupService : IDisposable
	{
		private const string SAVE_EXTENSION = ".ngd";
		private const string BACKUP_EXTENSION = ".ngd.backup";
		private const string BACKUP_TIME_FORMAT = "yyyyMMddHHmmssfffffff";

		private readonly List<BackupData> _backups = new List<BackupData>();
		private readonly SaveConfig _config;
		private readonly IFileSystem _fileSystem;
		private readonly object _lock = new object();

		private bool _isDisposed = false;

		public int BackupCount {
			get {
				ThrowIfDisposed();

				lock ( _lock ) {
					return _backups.Count;
				}
			}
		}

		/*
		===============
		BackupService
		===============
		*/
		/// <summary>
		/// Creates a new backup service.
		/// </summary>
		/// <param name="config">Save-system config.</param>
		/// <param name="fileSystem">File-system service.</param>
		public BackupService( SaveConfig config, IFileSystem fileSystem )
		{
			_config = config ?? throw new ArgumentNullException( nameof( config ) );
			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );

			if ( string.IsNullOrWhiteSpace( _config.BackupPath ) ) {
				throw new ArgumentException( "Backup path cannot be null or empty.", nameof( config ) );
			}
			if ( _config.MaxBackups < 0 ) {
				throw new ArgumentOutOfRangeException( nameof( config ), "MaxBackups cannot be negative." );
			}

			EnsureBackupDirectory();
			RefreshBackups();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Releases backup-service state.
		/// </summary>
		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			lock ( _lock ) {
				_backups.Clear();
			}

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		RefreshBackups
		===============
		*/
		/// <summary>
		/// Rebuilds the in-memory backup chain from disk.
		/// </summary>
		public void RefreshBackups()
		{
			ThrowIfDisposed();

			lock ( _lock ) {
				_backups.Clear();

				if ( !_fileSystem.DirectoryExists( _config.BackupPath ) ) {
					return;
				}

				IReadOnlyList<string> files = _fileSystem.GetFiles(
					_config.BackupPath,
					"*" + BACKUP_EXTENSION,
					false
				);

				for ( int i = 0; i < files.Count; i++ ) {
					if ( TryParseBackupPath( files[i], out BackupData backup ) ) {
						_backups.Add( backup );
					}
				}

				SortBackups( _backups );
			}
		}

		/*
		===============
		ListBackups
		===============
		*/
		/// <summary>
		/// Returns a snapshot of all known backups.
		/// </summary>
		/// <returns>Backup metadata snapshot.</returns>
		public IReadOnlyList<BackupData> ListBackups()
		{
			ThrowIfDisposed();

			lock ( _lock ) {
				return _backups.ToArray();
			}
		}

		/*
		===============
		ListBackups
		===============
		*/
		/// <summary>
		/// Returns a snapshot of backups for a logical save.
		/// </summary>
		/// <param name="fileName">Save name or save path.</param>
		/// <returns>Backup metadata snapshot.</returns>
		public IReadOnlyList<BackupData> ListBackups( string fileName )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( fileName, nameof( fileName ) );

			string saveName = GetSaveName( fileName );
			var result = new List<BackupData>();

			lock ( _lock ) {
				for ( int i = 0; i < _backups.Count; i++ ) {
					BackupData backup = _backups[i];

					if ( backup.SaveName.Equals( saveName, StringComparison.Ordinal ) ) {
						result.Add( backup );
					}
				}
			}

			SortBackups( result );
			return result.ToArray();
		}

		/*
		===============
		CreateBackup
		===============
		*/
		/// <summary>
		/// Creates a backup for an existing save file.
		/// </summary>
		/// <param name="fileName">Primary save file path.</param>
		/// <returns><c>true</c> if a backup was created; otherwise, <c>false</c>.</returns>
		public bool CreateBackup( string fileName )
		{
			return TryCreateBackup( fileName, out _ );
		}

		/*
		===============
		TryCreateBackup
		===============
		*/
		/// <summary>
		/// Creates a backup for an existing save file.
		/// </summary>
		/// <param name="fileName">Primary save file path.</param>
		/// <param name="backup">Created backup metadata.</param>
		/// <returns><c>true</c> if a backup was created; otherwise, <c>false</c>.</returns>
		public bool TryCreateBackup( string fileName, out BackupData backup )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( fileName, nameof( fileName ) );

			backup = BackupData.Empty;

			if ( _config.MaxBackups == 0 ) {
				return false;
			}
			if ( !_fileSystem.FileExists( fileName ) ) {
				return false;
			}

			lock ( _lock ) {
				EnsureBackupDirectory();

				string saveName = GetSaveName( fileName );
				int order = GetNextBackupOrder( saveName );
				DateTime createdUtc = DateTime.UtcNow;
				string backupPath = CreateBackupPath( saveName, createdUtc, order );

				try {
					_fileSystem.CopyFile( fileName, backupPath, true );

					backup = new BackupData(
						sourcePath: fileName,
						backupPath: backupPath,
						saveName: saveName,
						createdUtc: createdUtc,
						sizeBytes: _fileSystem.GetFileSize( backupPath ),
						order: order
					);

					_backups.Add( backup );
					SortBackups( _backups );
					PruneBackupsNoLock( saveName );

					return true;
				} catch {
					DeletePartialBackupNoThrow( backupPath );
					backup = BackupData.Empty;
					return false;
				}
			}
		}

		/*
		===============
		TryGetLatestBackup
		===============
		*/
		/// <summary>
		/// Gets the newest backup for a logical save.
		/// </summary>
		/// <param name="fileName">Save name or save path.</param>
		/// <param name="backup">Newest backup.</param>
		/// <returns><c>true</c> if a backup exists; otherwise, <c>false</c>.</returns>
		public bool TryGetLatestBackup( string fileName, out BackupData backup )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( fileName, nameof( fileName ) );

			string saveName = GetSaveName( fileName );

			lock ( _lock ) {
				backup = BackupData.Empty;
				bool found = false;

				for ( int i = 0; i < _backups.Count; i++ ) {
					BackupData candidate = _backups[i];

					if ( !candidate.SaveName.Equals( saveName, StringComparison.Ordinal ) ) {
						continue;
					}

					if ( !found || IsNewer( candidate, backup ) ) {
						backup = candidate;
						found = true;
					}
				}

				return found;
			}
		}

		/*
		===============
		RestoreLatestBackup
		===============
		*/
		/// <summary>
		/// Restores the newest backup to the supplied destination save path.
		/// </summary>
		/// <param name="destinationFileName">Destination primary save path.</param>
		/// <returns><c>true</c> if restored; otherwise, <c>false</c>.</returns>
		public bool RestoreLatestBackup( string destinationFileName )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( destinationFileName, nameof( destinationFileName ) );

			if ( !TryGetLatestBackup( destinationFileName, out BackupData backup ) ) {
				return false;
			}

			return RestoreBackup( backup, destinationFileName );
		}

		/*
		===============
		RestoreBackup
		===============
		*/
		/// <summary>
		/// Restores a specific backup to the supplied destination save path.
		/// </summary>
		/// <param name="backup">Backup metadata.</param>
		/// <param name="destinationFileName">Destination primary save path.</param>
		/// <returns><c>true</c> if restored; otherwise, <c>false</c>.</returns>
		public bool RestoreBackup( BackupData backup, string destinationFileName )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( destinationFileName, nameof( destinationFileName ) );

			if ( !backup.IsValid ) {
				return false;
			}
			if ( !_fileSystem.FileExists( backup.BackupPath ) ) {
				return false;
			}

			try {
				string? directory = Path.GetDirectoryName( destinationFileName );
				if ( !string.IsNullOrWhiteSpace( directory ) && !_fileSystem.DirectoryExists( directory ) ) {
					_fileSystem.CreateDirectory( directory );
				}

				_fileSystem.CopyFile( backup.BackupPath, destinationFileName, true );
				return true;
			} catch {
				return false;
			}
		}

		/*
		===============
		DeleteBackup
		===============
		*/
		/// <summary>
		/// Deletes a specific backup.
		/// </summary>
		/// <param name="backup">Backup metadata.</param>
		/// <returns><c>true</c> if deleted; otherwise, <c>false</c>.</returns>
		public bool DeleteBackup( BackupData backup )
		{
			ThrowIfDisposed();

			if ( !backup.IsValid ) {
				return false;
			}

			lock ( _lock ) {
				return DeleteBackupNoLock( backup );
			}
		}

		/*
		===============
		DeleteBackups
		===============
		*/
		/// <summary>
		/// Deletes all backups for a logical save.
		/// </summary>
		/// <param name="fileName">Save name or save path.</param>
		/// <returns>Number of backups deleted.</returns>
		public int DeleteBackups( string fileName )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( fileName, nameof( fileName ) );

			string saveName = GetSaveName( fileName );
			int deleted = 0;

			lock ( _lock ) {
				for ( int i = _backups.Count - 1; i >= 0; i-- ) {
					BackupData backup = _backups[i];

					if ( backup.SaveName.Equals( saveName, StringComparison.Ordinal ) && DeleteBackupNoLock( backup ) ) {
						deleted++;
					}
				}
			}

			return deleted;
		}

		/*
		===============
		PruneBackups
		===============
		*/
		/// <summary>
		/// Enforces the configured backup-chain length for a logical save.
		/// </summary>
		/// <param name="fileName">Save name or save path.</param>
		public void PruneBackups( string fileName )
		{
			ThrowIfDisposed();
			ThrowIfNullOrEmpty( fileName, nameof( fileName ) );

			string saveName = GetSaveName( fileName );

			lock ( _lock ) {
				PruneBackupsNoLock( saveName );
			}
		}

		/*
		===============
		DeleteBackupNoLock
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="backup"></param>
		/// <returns></returns>
		private bool DeleteBackupNoLock( BackupData backup )
		{
			try {
				if ( _fileSystem.FileExists( backup.BackupPath ) ) {
					_fileSystem.DeleteFile( backup.BackupPath );
				}

				for ( int i = _backups.Count - 1; i >= 0; i-- ) {
					if ( _backups[i].BackupPath.Equals( backup.BackupPath, StringComparison.OrdinalIgnoreCase ) ) {
						_backups.RemoveAt( i );
					}
				}

				return true;
			} catch {
				return false;
			}
		}

		/*
		===============
		PruneBackupsNoLock
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="saveName"></param>
		private void PruneBackupsNoLock( string saveName )
		{
			var matches = new List<BackupData>();

			for ( int i = 0; i < _backups.Count; i++ ) {
				BackupData backup = _backups[i];

				if ( backup.SaveName.Equals( saveName, StringComparison.Ordinal ) ) {
					matches.Add( backup );
				}
			}

			SortBackups( matches );

			for ( int i = _config.MaxBackups; i < matches.Count; i++ ) {
				DeleteBackupNoLock( matches[i] );
			}
		}

		/*
		===============
		EnsureBackupDirectory
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void EnsureBackupDirectory()
		{
			if ( !_fileSystem.DirectoryExists( _config.BackupPath ) ) {
				_fileSystem.CreateDirectory( _config.BackupPath );
			}
		}

		/*
		===============
		CreateBackupPath
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="saveName"></param>
		/// <param name="createdUtc"></param>
		/// <param name="order"></param>
		/// <returns></returns>
		private string CreateBackupPath( string saveName, DateTime createdUtc, int order )
		{
			string safeSaveName = SanitizeFileName( saveName );
			string timestamp = createdUtc.ToString( BACKUP_TIME_FORMAT, CultureInfo.InvariantCulture );
			string backupFileName = $"{safeSaveName}.bak{order:D4}.{timestamp}{BACKUP_EXTENSION}";

			return Path.Combine( _config.BackupPath, backupFileName );
		}

		/*
		===============
		TryParseBackupPath
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="backupPath"></param>
		/// <param name="backup"></param>
		/// <returns></returns>
		private bool TryParseBackupPath( string backupPath, out BackupData backup )
		{
			backup = BackupData.Empty;

			if ( string.IsNullOrWhiteSpace( backupPath ) ) {
				return false;
			}
			if ( !backupPath.EndsWith( BACKUP_EXTENSION, StringComparison.OrdinalIgnoreCase ) ) {
				return false;
			}
			if ( !_fileSystem.FileExists( backupPath ) ) {
				return false;
			}

			string fileName = Path.GetFileName( backupPath );
			string stem = fileName.Substring( 0, fileName.Length - BACKUP_EXTENSION.Length );

			int timestampSeparator = stem.LastIndexOf( '.' );
			if ( timestampSeparator < 0 ) {
				return false;
			}

			string timestampText = stem.Substring( timestampSeparator + 1 );
			string saveAndOrder = stem.Substring( 0, timestampSeparator );

			int bakSeparator = saveAndOrder.LastIndexOf( ".bak", StringComparison.Ordinal );
			if ( bakSeparator < 0 ) {
				return false;
			}

			string saveName = saveAndOrder.Substring( 0, bakSeparator );
			string orderText = saveAndOrder.Substring( bakSeparator + 4 );

			if ( string.IsNullOrWhiteSpace( saveName ) ) {
				return false;
			}
			if ( !int.TryParse( orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int order ) ) {
				return false;
			}
			if ( !TryParseTimestamp( timestampText, out DateTime createdUtc ) ) {
				createdUtc = _fileSystem.GetLastWriteTime( backupPath ).ToUniversalTime();
			}

			backup = new BackupData(
				sourcePath: string.Empty,
				backupPath: backupPath,
				saveName: saveName,
				createdUtc: createdUtc,
				sizeBytes: _fileSystem.GetFileSize( backupPath ),
				order: order
			);

			return true;
		}

		/*
		===============
		GetNextBackupOrder
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="saveName"></param>
		/// <returns></returns>
		private int GetNextBackupOrder( string saveName )
		{
			int maxOrder = 0;

			for ( int i = 0; i < _backups.Count; i++ ) {
				BackupData backup = _backups[i];

				if ( backup.SaveName.Equals( saveName, StringComparison.Ordinal ) && backup.Order > maxOrder ) {
					maxOrder = backup.Order;
				}
			}

			return maxOrder + 1;
		}

		/*
		===============
		DeletePartialBackupNoThrow
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="backupPath"></param>
		private void DeletePartialBackupNoThrow( string backupPath )
		{
			if ( string.IsNullOrWhiteSpace( backupPath ) ) {
				return;
			}

			try {
				if ( _fileSystem.FileExists( backupPath ) ) {
					_fileSystem.DeleteFile( backupPath );
				}
			} catch {
				// Preserve original backup failure behavior.
			}
		}

		/*
		===============
		GetSaveName
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private static string GetSaveName( string fileName )
		{
			string name = Path.GetFileName( fileName );

			if ( name.EndsWith( SAVE_EXTENSION, StringComparison.OrdinalIgnoreCase ) ) {
				return name.Substring( 0, name.Length - SAVE_EXTENSION.Length );
			}
			if ( name.EndsWith( BACKUP_EXTENSION, StringComparison.OrdinalIgnoreCase ) ) {
				return name.Substring( 0, name.Length - BACKUP_EXTENSION.Length );
			}

			return Path.GetFileNameWithoutExtension( name );
		}

		/*
		===============
		SanitizeFileName
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static string SanitizeFileName( string name )
		{
			char[] invalidChars = Path.GetInvalidFileNameChars();
			char[] chars = name.ToCharArray();

			for ( int i = 0; i < chars.Length; i++ ) {
				if ( Array.IndexOf( invalidChars, chars[i] ) >= 0 ) {
					chars[i] = '_';
				}
			}

			return new string( chars );
		}

		/*
		===============
		TryParseTimestamp
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		/// <param name="createdUtc"></param>
		/// <returns></returns>
		private static bool TryParseTimestamp( string text, out DateTime createdUtc )
		{
			return DateTime.TryParseExact(
				text,
				BACKUP_TIME_FORMAT,
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
				out createdUtc
			);
		}

		/*
		===============
		SortBackups
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="backups"></param>
		private static void SortBackups( List<BackupData> backups )
		{
			backups.Sort(
				( a, b ) => {
					int dateComparison = b.CreatedUtc.CompareTo( a.CreatedUtc );
					if ( dateComparison != 0 ) {
						return dateComparison;
					}

					return b.Order.CompareTo( a.Order );
				}
			);
		}

		/*
		===============
		IsNewer
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="candidate"></param>
		/// <param name="current"></param>
		/// <returns></returns>
		private static bool IsNewer( BackupData candidate, BackupData current )
		{
			int dateComparison = candidate.CreatedUtc.CompareTo( current.CreatedUtc );
			if ( dateComparison > 0 ) {
				return true;
			}
			if ( dateComparison < 0 ) {
				return false;
			}

			return candidate.Order > current.Order;
		}

		/*
		===============
		ThrowIfDisposed
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <exception cref="ObjectDisposedException"></exception>
		private void ThrowIfDisposed()
		{
			StateGuard.ThrowIfDisposed( _isDisposed, this );
		}

		/*
		===============
		ThrowIfNullOrEmpty
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <param name="paramName"></param>
		/// <exception cref="ArgumentException"></exception>
		private static void ThrowIfNullOrEmpty( string value, string paramName )
		{
			if ( string.IsNullOrWhiteSpace( value ) ) {
				throw new ArgumentException( "Value cannot be null or empty.", paramName );
			}
		}
	};
};
