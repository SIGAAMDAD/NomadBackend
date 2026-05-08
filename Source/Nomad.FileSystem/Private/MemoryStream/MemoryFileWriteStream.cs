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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.FileSystem.Configs;

namespace Nomad.FileSystem.Private.MemoryStream
{
	/*
	===================================================================================

	MemoryFileWriteStream

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class MemoryFileWriteStream : MemoryWriteStream, IMemoryFileWriteStream
	{
		/// <summary>
		/// 
		/// </summary>
		public string FilePath => _filepath;
		private readonly string _filepath;

		/// <summary>
		/// 
		/// </summary>
		public bool IsOpen => buffer != null;

		/*
		===============
		MemoryFileWriteStream
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="config"></param>
		public MemoryFileWriteStream( MemoryFileWriteConfig config )
			: base( config )
		{
			_filepath = config.FilePath!;
			Open( config );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if ( isDisposed ) {
				return;
			}
			if ( disposing ) {
				Flush();
			}
			base.Dispose( disposing );
			isDisposed = true;
		}

		/*
		===============
		DisposeAsyncCore
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		protected override async ValueTask DisposeAsyncCore()
		{
			await FlushAsync();
			await base.DisposeAsyncCore();
		}

		/*
		===============
		Close
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Close()
		{
			Dispose();
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the memory file write stream to the underlying file.
		/// </summary>
		public override void Flush()
		{
			if ( buffer != null ) {
				using var stream = new System.IO.FileStream( _filepath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None );
				stream.Write( buffer.GetSlice( 0, (int)position ) );
				stream.Close();
			}
		}

		/*
		===============
		FlushAsync
		===============
		*/
		/// <summary>
		/// Asynchronously flushes the memory file write stream to the underlying file.
		/// </summary>
		/// <param name="ct">The cancellation token to use for the operation.</param>
		public override async ValueTask FlushAsync( CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();
			if ( buffer != null ) {
				using var stream = new System.IO.FileStream( _filepath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None );
				await stream.WriteAsync( buffer.AsMemory( 0, (int)position ), ct );
				stream.Close();
			}
		}

		/*
		===============
		Open
		===============
		*/
		/// <summary>
		/// Opens the memory file write stream with the specified file path, open mode, and access mode.
		/// </summary>
		/// <param name="config"></param>
		/// <returns><c>true</c> if the file was opened successfully; otherwise, <c>false.</c></returns>
		private bool Open( MemoryFileWriteConfig config )
		{
			if ( config.Append ) {
				throw new NotImplementedException();
			} else {
				length = config.InitialCapacity;
				buffer = AllocateBuffer( length );
			}
			return true;
		}

		/*
		===============
		WriteLine
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		public void WriteLine( string line )
			=> throw new NotImplementedException();

		/*
		===============
		WriteLine
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		public void WriteLine( ReadOnlySpan<char> line )
			=> throw new NotImplementedException();

		/*
		===============
		WriteLineAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async ValueTask WriteLineAsync( string line, CancellationToken ct = default )
			=> throw new NotImplementedException();

		/*
		===============
		WriteLineAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async ValueTask WriteLineAsync( ReadOnlyMemory<char> line, CancellationToken ct = default )
			=> throw new NotImplementedException();
	};
};
