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

using NomadCore.Abstractions.Services;
using NomadCore.Enums;
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Systems.ConsoleSystem.CVars.Common;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;
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

	public sealed class LoggerService : ILoggerService {
		private readonly ILoggerSink[] Sinks;
		private readonly CVar<LogLevel>? LogDepth;

		/*
		===============
		LoggerService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="commandLine"></param>
		/// <param name="sinks"></param>
		public LoggerService( ICommandLine? commandLine, ILoggerSink[]? sinks ) {
			ArgumentNullException.ThrowIfNull( commandLine );
			ArgumentNullException.ThrowIfNull( sinks );

			commandLine.TextEntered.Subscribe( this, OnTextEntered );
			Sinks = sinks;

			LogDepth = (CVar<LogLevel>?)ServiceRegistry.Get<ICVarSystemService>().GetCVar<LogLevel>( "console.LogLevel" );
		}

		/*
		===============
		Initialize
		===============
		*/
		/// <summary>
		/// Initializes the logging systems
		/// </summary>
		public void Initialize() {
			ServiceRegistry.Get<ICommandService>().RegisterCommand( new ConsoleCommand( "clear", OnClear, "Clears the console." ) );
			ServiceRegistry.Get<ICommandService>().RegisterCommand( new ConsoleCommand( "echo", OnEcho, "Prints a string to the console." ) );
		}

		/*
		===============
		Shutdown
		===============
		*/
		public void Shutdown() {
			for ( int i = 0; i < Sinks.Length; i++ ) {
				Sinks[ i ].Flush();
			}
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
			if ( level > LogDepth ) {
				return;
			}
			for ( int i = 0; i < Sinks.Length; i++ ) {
				Sinks[ i ].Print( message );
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
		public void PrintLine( string? message ) {
			ArgumentException.ThrowIfNullOrEmpty( message );

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
		public void PrintDebug( string? message ) {
			ArgumentException.ThrowIfNullOrEmpty( message );

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
		public void PrintWarning( string? message ) {
			ArgumentException.ThrowIfNullOrEmpty( message );

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
		public void PrintError( string? message ) {
			ArgumentException.ThrowIfNullOrEmpty( message );

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
			for ( int i = 0; i < Sinks.Length; i++ ) {
				Sinks[ i ].Clear();
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
			PrintLine( ServiceRegistry.Get<ICommandLine>().GetArgumentAt( 0 ) );
		}

		/*
		===============
		OnTextEntered
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="args"></param>
		/// <exception cref="InvalidCastException"></exception>
		private void OnTextEntered( in IGameEvent eventData, in IEventArgs args ) {
			if ( args is TextEnteredEventData textEntered ) {
				PrintLine( $"> {textEntered.Text}" );
			} else {
				throw new InvalidCastException( nameof( args ) );
			}
		}
	};
};