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
using System.Threading.Tasks;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Memory.Buffers;

namespace Nomad.FileSystem.Private.MemoryStream
{
	/*
	===================================================================================

	MemoryFileReadStream

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class MemoryFileReadStream : MemoryReadStream, IMemoryFileReadStream
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
		MemoryFileReadStream
		===============
		*/
		/// <summary>
		/// Initializes a new instance of the MemoryFileReadStream class with the specified file path.
		/// </summary>
		/// <param name="config"></param>
		public MemoryFileReadStream( MemoryFileReadConfig config )
			: base( config, false )
		{
			_filepath = config.FilePath;
			Open( _filepath );
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
		Open
		===============
		*/
		/// <summary>
		/// Opens the memory file read stream with the specified file path, open mode, and access mode.
		/// </summary>
		/// <param name="filepath">The path to the file to open.</param>
		/// <returns><c>true</c> if the file was opened successfully; otherwise, <c>false.</c></returns>
		private bool Open( string filepath )
		{
			position = 0;

			try {
				FileInfo info = new FileInfo( filepath );
				buffer = AllocateBuffer( info.Length );
				length = buffer.Length;
				return true;
			} catch ( IOException ) {
				throw;
			}
		}

		/*
		===============
		AllocateBuffer
		===============
		*/
		/// <summary>
		/// Creates a new <see cref="IBufferHandle"/> based on the allocation strategy provided.
		/// </summary>
		/// <param name="length"></param>
		/// <returns></returns>
		protected override IBufferHandle AllocateBuffer( long length )
		{
			switch ( strategy ) {
				case AllocationStrategy.FromFile: {
						byte[] fileBuffer = File.ReadAllBytes( _filepath );
						return new SharedBufferHandle( fileBuffer, fileBuffer.Length );
					}
				case AllocationStrategy.Pooled:
				case AllocationStrategy.Standard: {
						var fileBuffer = base.AllocateBuffer( length );
						using var stream = new BinaryReader( new FileStream( _filepath, FileMode.Open, FileAccess.Read ) );
						stream.Read( fileBuffer.Span );
						return fileBuffer;
					}
				default:
					throw new IndexOutOfRangeException( nameof( strategy ) );
			}
		}
	};
};
