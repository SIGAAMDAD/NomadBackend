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
using NomadCore.Infrastructure;
using NomadCore.Interfaces;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.ConsoleSystem.Services {
	/*
	===================================================================================
	
	CommandLine
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class CommandLine : ICommandLine {
		public int ArgumentCount => CommandBuilder.ArgumentCount;

		private readonly ICommandBuilder CommandBuilder;
		private readonly ILoggerService? Logger = ServiceRegistry.Get<ILoggerService>();

		//
		// autocomplete
		//
		private readonly List<string> Suggestions = new List<string>();
		private int CurrentSuggest = 0;
		private bool Suggesting = false;

		public IGameEvent<TextEnteredEventData> TextEntered => _textEntered;
		private readonly IGameEvent<TextEnteredEventData> _textEntered;

		public IGameEvent<CommandExecutedEventData> UnknownCommand => _unknownCommand;
		private readonly IGameEvent<CommandExecutedEventData> _unknownCommand;

		public IGameEvent<CommandExecutedEventData> CommandExecuted => _commandExecuted;
		private readonly IGameEvent<CommandExecutedEventData> _commandExecuted;

		/*
		===============
		CommandLine
		===============
		*/
		public CommandLine( ICommandBuilder? builder, IConsoleEvents? events, IGameEventBusService? eventBus ) {
			ArgumentNullException.ThrowIfNull( builder );
			ArgumentNullException.ThrowIfNull( events );
			ArgumentNullException.ThrowIfNull( eventBus );

			CommandBuilder = builder;

			_textEntered = eventBus.CreateEvent<TextEnteredEventData>( nameof( TextEntered ) );
			_unknownCommand = eventBus.CreateEvent<CommandExecutedEventData>( nameof( UnknownCommand ) );
			_commandExecuted = eventBus.CreateEvent<CommandExecutedEventData>( nameof( CommandExecuted ) );

			events.ConsoleClosed.Subscribe( this, OnConsoleClosed );

			builder.TextEntered.Subscribe( this, OnCommandExecuted );
		}

		/*
		===============
		Initialize
		===============
		*/
		public void Initialize() {
		}

		/*
		===============
		Shutdown
		===============
		*/
		public void Shutdown() {
		}

		/*
		===============
		ExecuteCommand
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void ExecuteCommand( string? text ) {
			ArgumentException.ThrowIfNullOrEmpty( text );
		}

		/*
		===============
		GetArgumentAt
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public string GetArgumentAt( int index ) {
			return CommandBuilder.GetArgumentAt( index );
		}

		/*
		===============
		GetArguments
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string[] GetArguments() {
			return CommandBuilder.GetArgs();
		}

		/*
		===============
		OnCommandExecuted
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="textEntered"></param>
		private void OnCommandExecuted( in TextEnteredEventData textEntered ) {
			string textCommand = CommandBuilder.ArgumentCount > 0 ? CommandBuilder.GetArgumentAt( 0 ) : String.Empty;

			var commandCache = ServiceRegistry.Get<ICommandService>();
			if ( !string.IsNullOrEmpty( textCommand ) && commandCache.TryGetCommand( textCommand, out IConsoleCommand consoleCommand ) ) {
				string[] arguments = CommandBuilder.GetArgs();

				try {
					consoleCommand.Callback.Invoke( new CommandExecutedEventData( consoleCommand, arguments ) );
					TextEntered.Publish( new CommandExecutedEventData( consoleCommand, arguments ) );
				} catch ( Exception ex ) {
					Logger?.PrintError( $"Error executing command: {ex.Message}" );
				}
			} else if ( !string.IsNullOrEmpty( textCommand ) ) {
				UnknownCommand.Publish( EmptyEventArgs.Args );
				Logger?.PrintError( "Command not found." );
			}
		}

		/*
		===============
		OnConsoleClosed
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="args"></param>
		private void OnConsoleClosed( in IGameEvent eventData, in IEventArgs args ) {
			//ResetAutocomplete();
		}

		/*
		===============
		ResetAutocomplete
		===============
		*/
		/// <summary>
		/// Clears the autocomplete buffer
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void ResetAutocomplete() {
			Suggestions.Clear();
			CurrentSuggest = 0;
			Suggesting = false;
		}

		/*
		===============
		Autocomplete
		===============
		*/
		/// <summary>
		/// I feel like this is self-explanatory
		/// </summary>
		/*
		private void OnAutoComplete( in IGameEvent eventData, in IEventArgs args ) {
			if ( Suggesting ) {
				if ( CurrentSuggest < Suggestions.Count ) {
					Text = Suggestions[ CurrentSuggest ];
					CaretColumn = Text.Length;
					CurrentSuggest = ( CurrentSuggest + 1 ) % Suggestions.Count;
				}
				return;
			}

			Suggesting = true;

			if ( Text.Contains( ' ' ) ) {
				string[] splitText = ParseLineInput( Text );
				if ( splitText.Length > 1 ) {
					string command = splitText[ 0 ];
					string paramInput = splitText[ 1 ];
					if ( CommandParameters.TryGetValue( command, out var parameters ) ) {
						for ( int i = 0; i < parameters.Length; i++ ) {
							if ( parameters[ i ].Contains( paramInput ) ) {
								Suggestions.Add( $"{command} {parameters[ i ]}" );
							}
						}
					}
				}
			} else {
				List<string>? sortedCommands = [ .. ConsoleCommands
					.Where( c => !c.Value.Hidden )
					.Select( c => c.Key )
					.OrderBy( c => c ) ];

				for ( int i = 0; i < sortedCommands.Count; i++ ) {
					if ( string.IsNullOrEmpty( Text ) || sortedCommands[ i ].Contains( Text ) ) {
						Suggestions.Add( sortedCommands[ i ] );
					}
				}
			}

			if ( Suggestions.Count > 0 ) {
				OnAutoComplete( in eventData, in args );
			} else {
				ResetAutocomplete();
			}
		}
		*/
	};
};