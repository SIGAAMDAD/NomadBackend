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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.FileSystem.Streams;

namespace Nomad.FileSystem.Private
{
	/*
	===================================================================================

	BaseStream

	===================================================================================
	*/
	/// <summary>
	/// Base implementation of a data stream.
	/// </summary>

	internal abstract class BaseStream : IDataStream
	{
		/// <summary>
		/// Gets the length of the stream in bytes.
		/// </summary>
		public abstract long Length { get; set; }

		/// <summary>
		/// Gets or sets the current position within the stream.
		/// </summary>
		public abstract long Position { get; set; }

		/// <summary>
		/// Gets a value indicating whether the stream supports reading.
		/// </summary>
		public abstract bool CanRead { get; }

		/// <summary>
		/// Gets a value indicating whether the stream supports writing.
		/// </summary>
		public abstract bool CanWrite { get; }

		/// <summary>
		/// Gets a value indicating whether the stream supports seeking.
		/// </summary>
		public abstract bool CanSeek { get; }

		/// <summary>
		/// The disposal flag.
		/// </summary>
		protected bool isDisposed = false;

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Disposes the stream.
		/// </summary>
		public abstract void Dispose();

		/*
		===============
		DisposeAsync
		===============
		*/
		/// <summary>
		/// Asynchronously disposes the stream.
		/// </summary>
		/// <returns></returns>
		public abstract ValueTask DisposeAsync();

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written.
		/// </summary>
		public abstract void Flush();

		/*
		===============
		FlushAsync
		===============
		*/
		/// <summary>
		/// Asynchronously clears all buffers for this stream and causes any buffered data to be written.
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public abstract ValueTask FlushAsync( CancellationToken ct = default );

		/*
		===============
		Seek
		===============
		*/
		/// <summary>
		/// Sets the position within the current stream.
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="origin"></param>
		/// <returns></returns>
		public abstract long Seek( long offset, SeekOrigin origin );

		/*
		===============
		SetLength
		===============
		*/
		/// <summary>
		/// Sets the stream's length to <paramref name="length"/>.
		/// </summary>
		/// <param name="length">The new length of the stream.</param>
		public abstract void SetLength( long length );
	};
};
