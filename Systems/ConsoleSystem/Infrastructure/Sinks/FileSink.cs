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
using NomadCore.Abstractions.Services;
using NomadCore.Enums;
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Systems.ConsoleSystem.Interfaces.Abstractions;
using NomadCore.Utilities;
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

	public sealed class FileSink : SinkBase {
		private readonly System.IO.StreamWriter? Writer = null;

		/*
		===============
		FileSink
		===============
		*/
		public FileSink() {
			ICVar<string> logfile = ServiceRegistry.Get<ICVarSystemService>().Register(
				new CVarCreateInfo<string>(
					name: "console.LogFile",
					defaultValue: "user://debug.log",
					description: "",
					flags: CVarFlags.Archive | CVarFlags.Developer,
					validator: ( file ) => file.Length > 0
				)
			);

			try {
				using System.IO.FileStream stream = new System.IO.FileStream( logfile.Value, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write );
				Writer = new System.IO.StreamWriter( stream );
			} catch ( Exception e ) {
				Writer?.Close();
				GD.PrintErr( $"FileSink: failed to create log file {logfile.Value} - {e.Message}" );
			}
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
			Writer?.WriteAsync( message );
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
			Writer?.BaseStream.SetLength( 0 );
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
			Writer?.Flush();
		}
	};
};