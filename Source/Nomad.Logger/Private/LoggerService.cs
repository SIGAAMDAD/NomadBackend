/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text;

namespace Nomad.Logger.Private {
	/*
	===================================================================================
	
	LoggerService
	
	===================================================================================
	*/
	/// <summary>
	/// Handles the process of logging.
	/// </summary>

	public sealed class LoggerService : IDisposable, ILoggerService {
		private ICVar<LogLevel> _logDepth;
		private ICommandLineService _commandLine;
		private readonly List<ILoggerSink> _sinks = new List<ILoggerSink>();

		private readonly LockFreePooledQueue<string> _messageQueue = new LockFreePooledQueue<string>( 256 );
		private readonly object _lockObject = new object();

		/*
		===============
		Init
		===============
		*/
		/// <summary>
		/// Initializes the logging service
		/// </summary>
		/// <param name="locator"></param>
		/// <exception cref="Exception"></exception>
		public void Init( IServiceLocator locator ) {
			// create the logger thread
			_ = Task.Run( LoggerThreadAsync );

			var cvarSystem = locator.GetService<ICVarSystemService>();
			_logDepth = cvarSystem.Register(
				new CVarCreateInfo<LogLevel>(
					Name: new( Constants.CVars.Console.CONSOLE_LOG_LEVEL ),
					DefaultValue: LogLevel.Info,
					Description: new( "The verbosity of the logger." ),
					Flags: CVarFlags.Archive,
					Validator: value => value >= LogLevel.Error && value < LogLevel.Count
				)
			);
		}

		/*
		===============
		InitCommandService
		===============
		*/
		public void InitCommandService( IGameService commandService ) {
			var service = commandService as ICommandService ?? throw new InvalidCastException( nameof( commandService ) );
			service.RegisterCommand( new ConsoleCommand( "clear", OnClear, "Clears the console." ) );
			service.RegisterCommand( new ConsoleCommand( "echo", OnEcho, "Prints a string to the console." ) );
		}

		/*
		===============
		InitCommandLineService
		===============
		*/
		public void InitCommandLineService( IGameService commandLine ) {
			_commandLine = commandLine as ICommandLineService ?? throw new InvalidCastException( nameof( commandLine ) );
			_commandLine.TextEntered.Subscribe( this, OnTextEntered );
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
		private void PrintMessage( LogLevel level, in string message ) {
			if ( level > _logDepth?.Value ) {
				return;
			}
			_messageQueue.TryEnqueue( in message );
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
		public void PrintLine( in string message ) {
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
		public void PrintDebug( in string message ) {
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
		public void PrintWarning( in string message ) {
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
		public void PrintError( in string message ) {
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
		private void OnClear( in CommandExecutedEventData args ) {
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
		private void OnEcho( in CommandExecutedEventData args ) {
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

		/*
		===============
		LoggerThreadAsync
		===============
		*/
		private async Task LoggerThreadAsync() {
			try {
				while ( true ) {
					while ( _messageQueue.TryDequeue( out var message ) ) {
						for ( int i = 0; i < _sinks.Count; i++ ) {
							_sinks[ i ].Print( in message );
						}
					}
					await Task.Delay( 500 );
				}
			} catch ( Exception e ) {
				GD.PrintErr( $"Exception caught: {e}" );
			}
		}
	};
};