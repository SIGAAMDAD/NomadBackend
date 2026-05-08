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

using Nomad.Core.CVars;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Logger;
using System;

namespace Nomad.Logger.Private.Sinks
{
	/*
	===================================================================================

	FileSink

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FileSink : SinkBase
	{
		private readonly IFileWriteStream? _writer = null;

		/*
		===============
		FileSink
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvarSystem"></param>
		/// <param name="fileSystem"></param>
		public FileSink( ICVarSystemService cvarSystem, IFileSystem fileSystem )
		{
			var logfile = cvarSystem.Register(
				new CVarCreateInfo<string> {
					Name = "console.LogFile",
					DefaultValue = "debug.log",
					Description = "The path to the console's logging file.",
					Flags = CVarFlags.Archive | CVarFlags.Developer,
					Validator = file => file.Length > 0
				}
			);

			try {
				_writer = fileSystem.OpenWrite(
					new FileWriteConfig {
						FilePath = logfile.Value,
						Append = false
					}
				) as IFileWriteStream;
			} catch ( Exception e ) {
				_writer?.Close();
				global::System.Console.WriteLine( $"FileSink: failed to create log file {logfile.Value} - {e.Message}" );
				throw;
			}
			global::System.Console.WriteLine( $"Sucessfully opened logfile at {DateTime.Now}." );
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
			if ( !isDisposed ) {
				_writer?.Dispose();
			}
			base.Dispose( disposing );
		}

		/*
		===============
		Print
		===============
		*/
		/// <summary>
		/// Writes a string of characters to the file stream asynchronously.
		/// </summary>
		/// <param name="message"></param>
		public override void Print( string message )
		{
			_writer?.WriteLine( message );
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// Clears the file stream.
		/// </summary>
		public override void Clear()
		{
			_writer?.SetLength( 0 );
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the contents of the stream writer to the logfile.
		/// </summary>
		public override void Flush()
		{
			_writer?.Flush();
		}
	};
};
