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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Engine.Services;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;
using Nomad.FileSystem.Private.FileStreams;
using System.IO;
using Nomad.FileSystem.Private.MemoryStream;
using Nomad.Core.Memory.Buffers;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;

namespace Nomad.FileSystem.Private.Services
{
	/*
	===================================================================================

	FileSystem

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FileSystemService : IFileSystem
	{
		private readonly IEngineService _engineService;
		private readonly RecursiveFileSearcher _searchHelper;

		private readonly ILoggerCategory _category;

		private bool _isDisposed = false;

		/*
		===============
		FileSystemService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="engineService"></param>
		/// <param name="logger"></param>
		public FileSystemService( IEngineService engineService, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( engineService );
			ArgumentGuard.ThrowIfNull( logger );

			_engineService = engineService;
			_category = logger.CreateCategory( nameof( FileSystem ), LogLevel.Info, true );

			_searchHelper = new RecursiveFileSearcher( _category );
			_searchHelper.AddSearchDirectory( engineService.GetStoragePath( StorageScope.StreamingAssets ) );
			_searchHelper.AddSearchDirectory( engineService.GetStoragePath( StorageScope.UserData ) );
			_searchHelper.AddSearchDirectory( Path.GetTempPath() );
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
				_searchHelper?.Dispose();
				_category?.Dispose();
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
		///
		/// </summary>
		/// <param name="directory"></param>
		public void AddSearchDirectory( string directory )
		{
			_searchHelper.AddSearchDirectory( ResolveDirectoryPath( directory ) ?? directory );
		}

		/*
		===============
		CopyFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="destinationPath"></param>
		/// <param name="overwrite"></param>
		public void CopyFile( string sourcePath, string destinationPath, bool overwrite )
		{
			File.Copy( sourcePath, destinationPath, overwrite );
		}

		/*
		===============
		ReplaceFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sourcePath"></param>
		/// <param name="destPath"></param>
		/// <param name="destBackupPath"></param>
		public void ReplaceFile( string sourcePath, string destPath, string destBackupPath )
		{
			//
			// NOTE: File.Replace does not work reliably with the VFS nor does it play very nice with temporary files. So this is here
			// because it is the only way to reliably replace files across platforms. In testing, the temporary file existed, and File.Replace
			// was given a valid file path (File.Exists succeeded on it), yet File.Replace threw FileNotFoundException each time it was attempted,
			// even when the source file exists and is accessible, likely due to VFS path translation issues. No fix has been found yet, and this
			// works for now.
			//
			// It mimics the exact same behavior as File.Replace, but within the VFS. If there is a way to make File.Replace behave, then
			// do tell and open a PR, but until then, this is what we've got.
			//

			// TODO: this copies only the contents, not the metadata, of the file. Do find a way of copying the metadata in the long run.

			ArgumentGuard.ThrowIfNullOrEmpty( sourcePath );
			ArgumentGuard.ThrowIfNullOrEmpty( destPath );

			if ( sourcePath.Equals( destPath, StringComparison.OrdinalIgnoreCase ) ) {
				return; // nothing to do
			}

			// ensure the source file exists
			if ( !FileExists( sourcePath ) ) {
				throw new FileNotFoundException( "Source file not found", sourcePath );
			}

			string destDir = Path.GetDirectoryName( destPath );
			string tempFileName = Path.GetRandomFileName();
			string tempPath = Path.Combine( destDir, tempFileName );

			try {
				using ( var sourceStream = OpenRead( new FileReadConfig { FilePath = sourcePath } ) )
				using ( var tempStream = OpenWrite( new FileWriteConfig { FilePath = tempPath } ) ) {
					sourceStream.WriteToStream( tempStream );
				}
				if ( !string.IsNullOrEmpty( destBackupPath ) && FileExists( destPath ) ) {
					// if the backup already exists delete it, File.Replace does this already
					if ( FileExists( destBackupPath ) ) {
						DeleteFile( destBackupPath );
					}
					MoveFile( destPath, destBackupPath );
				}
				if ( FileExists( destPath ) ) {
					DeleteFile( destPath );
				}

				MoveFile( tempPath, destPath );
			} catch {
				// cleanup the temp file if it still exists
				if ( FileExists( tempPath ) ) {
					DeleteFile( tempPath );
				}
				throw;
			}
		}

		/*
		===============
		CreateDirectory
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		public void CreateDirectory( string path )
		{
			Directory.CreateDirectory( path );
		}

		/*
		===============
		DirectoryExists
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool DirectoryExists( string path )
		{
			if ( string.IsNullOrWhiteSpace( path ) ) {
				return false;
			}

			return Directory.Exists( path ) || ResolveDirectoryPath( path ) != null;
		}

		/*
		===============
		DeleteDirectory
		===============
		*/
		/// <summary>
		/// Deletes a directory at the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="recursive"></param>
		public void DeleteDirectory( string path, bool recursive )
		{
			Directory.Delete( path, recursive );
		}

		/*
		===============
		DeleteFile
		===============
		*/
		/// <summary>
		/// Deletes a file at the specified path.
		/// </summary>
		/// <param name="path">The path of the file to delete.</param>
		public void DeleteFile( string path )
		{
			File.Delete( path );
		}

		/*
		===============
		FileExists
		===============
		*/
		/// <summary>
		/// Checks if a file exists at the specified path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool FileExists( string path )
		{
			if ( string.IsNullOrWhiteSpace( path ) ) {
				return false;
			}

			if ( Path.IsPathRooted( path ) ) {
				return File.Exists( path ) || _searchHelper.FindFile( path ) != null;
			}

			return _searchHelper.FindFile( path ) != null || File.Exists( path );
		}

		/*
		===============
		GetConfigPath
		===============
		*/
		/// <summary>
		/// Gets the configuration path.
		/// </summary>
		/// <returns></returns>
		public string GetConfigPath()
		{
			return _engineService.GetStoragePath( "Config", StorageScope.UserData );
		}

		/*
		===============
		GetResourcePath
		===============
		*/
		/// <summary>
		/// Gets the resource path.
		/// </summary>
		/// <returns>The resource path.</returns>
		public string GetResourcePath()
		{
			return _engineService.GetStoragePath( StorageScope.StreamingAssets );
		}

		/*
		===============
		GetSavePath
		===============
		*/
		/// <summary>
		/// Gets the save directory.
		/// </summary>
		/// <returns>The save directory path.</returns>
		public string GetSavePath()
		{
			return _engineService.GetStoragePath( "SaveData", StorageScope.UserData );
		}

		/*
		===============
		GetUserDataPath
		===============
		*/
		/// <summary>
		/// Gets the user data directory.
		/// </summary>
		/// <returns>The user data directory path.</returns>
		public string GetUserDataPath()
		{
			return _engineService.GetStoragePath( StorageScope.UserData );
		}

		/*
		===============
		GetDirectories
		===============
		*/
		/// <summary>
		/// Gets the directories at the specified path.
		/// </summary>
		/// <param name="path">The path to get directories from.</param>
		/// <returns>A list of directory paths.</returns>
		public IReadOnlyList<string> GetDirectories( string path )
		{
			string? resolvedPath = ResolveDirectoryPath( path );
			return resolvedPath == null
				? Array.Empty<string>()
				: Directory.GetDirectories( resolvedPath );
		}

		/*
		===============
		GetFiles
		===============
		*/
		/// <summary>
		/// Gets the files at the specified path.
		/// </summary>
		/// <param name="path">The path to get files from.</param>
		/// <param name="searchPattern">The search pattern to use.</param>
		/// <param name="recursive">Whether to search recursively.</param>
		/// <returns>A list of file paths.</returns>
		public IReadOnlyList<string> GetFiles( string path, string searchPattern, bool recursive )
		{
			string? resolvedPath = ResolveDirectoryPath( path );
			return resolvedPath == null
				? Array.Empty<string>()
				: Directory.GetFiles( resolvedPath, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
		}

		/*
		===============
		GetFileSize
		===============
		*/
		/// <summary>
		/// Gets the size of the file at the specified path.
		/// </summary>
		/// <param name="path">The path of the file to get the size of.</param>
		/// <returns>The size of the file in bytes.</returns>
		public long GetFileSize( string path )
		{
			var info = new FileInfo( path );
			return info.Length;
		}

		/*
		===============
		GetLastWriteTime
		===============
		*/
		/// <summary>
		/// Gets the last write time of the file at the specified path.
		/// </summary>
		/// <param name="path">The path of the file to get the last write time of.</param>
		/// <returns>The last write time of the file.</returns>
		public DateTime GetLastWriteTime( string path )
		{
			var info = new FileInfo( path );
			return info.LastWriteTime;
		}

		/*
		===============
		MoveFile
		===============
		*/
		/// <summary>
		/// Moves a file from the source path to the destination path.
		/// </summary>
		/// <param name="sourcePath">The path of the file to move.</param>
		/// <param name="destinationPath">The path to move the file to.</param>
		/// <param name="overwrite"></param>
		public void MoveFile( string sourcePath, string destinationPath, bool overwrite = false )
		{
#if NETSTANDARD2_1
			if ( overwrite && FileExists( destinationPath ) ) {
				DeleteFile( destinationPath );
			}
			File.Move( sourcePath, destinationPath );
#else
			File.Move( sourcePath, destinationPath, overwrite );
#endif
		}

		/*
		===============
		OpenRead
		===============
		*/
		/// <summary>
		/// Opens a read stream for the specified file path.
		/// </summary>
		/// <param name="config">How to create the stream and rules around how it should be handled.</param>
		/// <returns>The read stream for the specified file path.</returns>
		public IReadStream? OpenRead( IReadConfig config )
		{
			ArgumentGuard.ThrowIfNull( config );

			switch ( config.Type ) {
				case StreamType.File: {
						var fileConfig = config as FileReadConfig ?? throw new InvalidCastException();
						var fullPath = _searchHelper.FindFile( fileConfig.FilePath );
						if ( fullPath == null ) {
							_category.PrintLine( $"FileSystemService.OpenRead: couldn't find file '{fileConfig.FilePath}'" );
							return null;
						}
						fileConfig = fileConfig with { FilePath = fullPath };
						return new FileReadStream( fileConfig );
					}
				case StreamType.MemoryFile: {
						var memoryFileConfig = config as MemoryFileReadConfig ?? throw new InvalidCastException();
						var fullPath = _searchHelper.FindFile( memoryFileConfig.FilePath );
						if ( fullPath == null ) {
							_category.PrintLine( $"FileSystemService.OpenRead: couldn't find file '{memoryFileConfig.FilePath}'" );
							return null;
						}
						memoryFileConfig = memoryFileConfig with { FilePath = fullPath };
						return new MemoryFileReadStream( memoryFileConfig );
					}
				case StreamType.Memory:
					return new MemoryReadStream(
						config is MemoryReadConfig memoryConfig ? memoryConfig : throw new InvalidCastException()
					);
				default:
					throw new ArgumentOutOfRangeException( nameof( config ) );
			}
		}

		/*
		===============
		OpenReadAsync
		===============
		*/
		/// <summary>
		/// Opens a read stream for the specified file path asynchronously.
		/// </summary>
		/// <param name="config">How to create the stream and rules around how it should be handled.</param>
		/// <param name="ct">The cancellation token.</param>
		/// <returns>The read stream for the specified file path.</returns>
		public async ValueTask<IReadStream?> OpenReadAsync( IReadConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return OpenRead( config );
		}

		/*
		===============
		OpenWrite
		===============
		*/
		/// <summary>
		/// Opens a write stream for the specified file path.
		/// </summary>
		/// <param name="config">How to create the stream and rules around how it should be handled.</param>
		/// <returns>The write stream for the specified file path.</returns>
		public IWriteStream? OpenWrite( IWriteConfig config )
		{
			ArgumentGuard.ThrowIfNull( config );

			return config.Type switch {
				StreamType.File => new FileWriteStream(
					config is FileWriteConfig fileConfig ? fileConfig : throw new InvalidCastException()
				),
				StreamType.MemoryFile => new MemoryFileWriteStream(
					config is MemoryFileWriteConfig memoryFileConfig ? memoryFileConfig : throw new InvalidCastException()
				),
				StreamType.Memory => new MemoryWriteStream(
					config is MemoryWriteConfig memoryConfig ? memoryConfig : throw new InvalidCastException()
				),
				_ => throw new ArgumentOutOfRangeException( nameof( config ) )
			};
		}

		/*
		===============
		OpenWriteAsync
		===============
		*/
		/// <summary>
		/// Opens a write stream with the specified configuration asynchronously.
		/// </summary>
		/// <param name="config">How to create the stream and rules around how it should be handled.</param>
		/// <param name="ct">The cancellation token.</param>
		/// <returns>The write stream with the specified configuration.</returns>
		public async ValueTask<IWriteStream?> OpenWriteAsync( IWriteConfig config, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			return OpenWrite( config );
		}

		/*
		===============
		CreateStream
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="accessMode"></param>
		/// <param name="type"></param>
		/// <param name="outputFile"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public IDataStream CreateStream( FileAccess accessMode, StreamType type, string outputFile = "", int length = 0 )
		{
			throw new NotImplementedException();
		}

		/*
		===============
		LoadFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="IOException"></exception>
		public IBufferHandle? LoadFile( string path )
		{
			var fullPath = _searchHelper.FindFile( path );
			if ( fullPath == null ) {
				return null;
			}
			using var stream = OpenRead( new FileReadConfig { FilePath = fullPath } ) ?? throw new IOException();

			var handle = new PooledBufferHandle( (int)stream.Length );
			stream.Read( handle.Span, 0, handle.Length );

			return handle;
		}

		/*
		===============
		LoadFileAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="IOException"></exception>
		public async ValueTask<IBufferHandle?> LoadFileAsync( string path, CancellationToken ct = default )
		{
			var fullPath = _searchHelper.FindFile( path );
			if ( fullPath == null ) {
				return null;
			}

			using var stream = OpenRead( new FileReadConfig { FilePath = fullPath } ) ?? throw new IOException();

			var handle = new PooledBufferHandle( (int)stream.Length );
			await stream.ReadAsync( handle.Memory, ct );

			return handle;
		}

		/*
		===============
		WriteFile
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		public void WriteFile( string path, byte[] buffer, int offset, int length )
		{
			using var stream = OpenWrite( new FileWriteConfig { FilePath = path } ) ?? throw new IOException( $"Error opening file {path}" );
			stream.Write( buffer, offset, length );
		}

		/*
		===============
		WriteFileAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="path"></param>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="ct"></param>
		public async ValueTask WriteFileAsync( string path, ReadOnlyMemory<byte> buffer, int offset, int length, CancellationToken ct = default )
		{
			ArgumentGuard.ThrowIfNull( buffer );

			ct.ThrowIfCancellationRequested();
			using var stream = await OpenWriteAsync( new FileWriteConfig { FilePath = path }, ct ) ?? throw new IOException( $"Error opening file {path}" );
			await stream.WriteAsync( buffer.Slice( offset, length ), ct );
		}

		/*
		===============
		ResolveDirectoryPath
		===============
		*/
		/// <summary>
		/// Resolves an existing directory path either directly from the OS path or through the VFS search roots.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string? ResolveDirectoryPath( string path )
		{
			if ( string.IsNullOrWhiteSpace( path ) ) {
				return null;
			}

			if ( Path.IsPathRooted( path ) && Directory.Exists( path ) ) {
				return Path.GetFullPath( path );
			}

			string? resolvedPath = _searchHelper.FindDirectory( path );
			if ( resolvedPath != null ) {
				return resolvedPath;
			}

			return Directory.Exists( path )
				? Path.GetFullPath( path )
				: null;
		}
	};
};
