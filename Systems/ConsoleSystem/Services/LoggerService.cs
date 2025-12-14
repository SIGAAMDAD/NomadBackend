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

using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure {
	/*
	===================================================================================
	
	LoggerService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class LoggerService : IDisposable, ILoggerService {
		private readonly List<ILoggerSink> _sinks;
		private readonly ICVar<LogLevel> _logDepth;
		private readonly ICommandLine _commandLine;

		/*
		===============
		LoggerService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		/// <param name="commandService"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="sinks"></param>
		/// <exception cref="Exception"></exception>
		public LoggerService( ICommandLine commandLine, ICommandService commandService, ICVarSystemService cvarSystem ) {
			ArgumentNullException.ThrowIfNull( commandLine );

			commandLine.TextEntered.Subscribe( this, OnTextEntered );
			
			_commandLine = commandLine;
			_sinks = new List<ILoggerSink>();

			_logDepth = cvarSystem.GetCVar<LogLevel>( "console.LogLevel" ) ?? throw new Exception( "Missing CVar 'console.LogLevel'" );

			commandService.RegisterCommand( new ConsoleCommand( "clear", OnClear, "Clears the console." ) );
			commandService.RegisterCommand( new ConsoleCommand( "echo", OnEcho, "Prints a string to the console." ) );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			for ( int i = 0; i < _sinks.Count; i++ ) {
				_sinks[ i ].Dispose();
			}
		}

		/*
		===============
		AddSink
		===============
		*/
		/// <summary>
		/// Adds a sink stream to the global logger service.
		/// </summary>
		/// <param name="sink"></param>
		public void AddSink( ILoggerSink sink ) {
			ArgumentNullException.ThrowIfNull( sink );
			_sinks.Add( sink );
		}

		/*
		===============
		PrintMessage
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void PrintMessage( LogLevel level, string message ) {
			if ( level > _logDepth.Value ) {
				return;
			}
			for ( int i = 0; i < _sinks.Count; i++ ) {
				_sinks[ i ].Print( message );
			}
		}

		/*
		===============
		PrintLine
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void PrintLine( string message ) {
			PrintMessage( LogLevel.Info, message );
		}

		/*
		===============
		PrintDebug
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void PrintDebug( string message ) {
			PrintMessage( LogLevel.Debug, $"[color=light_blue]DEBUG: {message}[/color]\n" );
		}

		/*
		===============
		PrintWarning
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void PrintWarning( string message ) {
			PrintMessage( LogLevel.Warning, $"[color=gold]WARNING: {message}[/color]\n" );
		}

		/*
		===============
		PrintError
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void PrintError( string message ) {
			PrintMessage( LogLevel.Error, $"[color=red]ERROR: {message}[/color]\n" );
		}

		/*
		===============
		OnClear
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnClear( in ICommandExecutedEventData args ) {
			for ( int i = 0; i < _sinks.Count; i++ ) {
				_sinks[ i ].Clear();
			}
		}

		/*
		===============
		OnEcho
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnEcho( in ICommandExecutedEventData args ) {
			PrintLine( _commandLine.GetArgumentAt( 0 ) );
		}

		/*
		===============
		OnTextEntered
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		/// <exception cref="InvalidCastException"></exception>
		private void OnTextEntered( in TextEnteredEventData args ) {
			PrintLine( $"> {args.Text}" );
		}
	};
};