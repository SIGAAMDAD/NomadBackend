/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Godot;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.ConsoleSystem.Interfaces.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure.Sinks {
	/*
	===================================================================================
	
	FileSink
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FileSink : SinkBase {
		private readonly System.IO.StreamWriter? _writer = null;

		/*
		===============
		FileSink
		===============
		*/
		public FileSink( ICVarSystemService cvarSystem ) {
			ICVar<string> logfile = cvarSystem.Register(
				new CVarCreateInfo<string>(
					Name: StringPool.Intern( "console.LogFile" ),
					DefaultValue: "user://debug.log",
					Description: StringPool.Intern( "The path to the console's logging file." ),
					Flags: CVarFlags.Archive | CVarFlags.Developer,
					Validator: file => file.Length > 0
				)
			);

			try {
				using System.IO.FileStream stream = new System.IO.FileStream( FilePath.FromUserPath( logfile.Value ).OSPath, System.IO.FileMode.Create, System.IO.FileAccess.Write );
				_writer = new System.IO.StreamWriter( stream );
			} catch ( Exception e ) {
				_writer?.Close();
				GD.PrintErr( $"FileSink: failed to create log file {logfile.Value} - {e.Message}" );
				throw;
			}
			GD.Print( "Sucessfully opened logfile." );
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
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public override void Print( string message ) {
			_writer?.WriteAsync( message );
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// Clears the file stream.
		/// </summary>
		public override void Clear() {
			_writer?.BaseStream.SetLength( 0 );
		}

		/*
		===============
		Flush
		===============
		*/
		/// <summary>
		/// Flushes the contents of the stream writer to the logfile.
		/// </summary>
		public override void Flush() {
			_writer?.Flush();
		}
	};
};