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

using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NomadCore.GameServices;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Systems.ConsoleSystem.Infrastructure;

namespace NomadCore.Systems.ConsoleSystem.Services {
	/*
	===================================================================================
	
	CommandLine
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class CommandLine : ICommandLineService {
		public int ArgumentCount => _commandBuilder.ArgumentCount;

		private readonly ICommandBuilder _commandBuilder;
		private readonly IHistory _history;

		//
		// autocomplete
		//
		private readonly List<string> _suggestions = new List<string>();
		private int _currentSuggest = 0;
		private bool _suggesting = false;

		private readonly ICommandService _commandService;
		private readonly ILoggerService _logger;

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
		public CommandLine( ICommandBuilder builder, ICommandService commandService, ILoggerService logger, IGameEventRegistryService eventFactory ) {
			ArgumentNullException.ThrowIfNull( builder );
			ArgumentNullException.ThrowIfNull( eventFactory );

			_commandBuilder = builder;
			_commandService = commandService;
			_logger = logger;
			_history = new History( builder, logger, eventFactory );

			_textEntered = eventFactory.GetEvent<TextEnteredEventData>( new( Constants.TEXT_ENTERED_EVENT ) );
			_unknownCommand = eventFactory.GetEvent<CommandExecutedEventData>( new( Constants.UNKNOWN_COMMAND_EVENT ) );
			_commandExecuted = eventFactory.GetEvent<CommandExecutedEventData>( new( Constants.COMMAND_EXECUTED_EVENT ) );

			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.CONSOLE_CLOSED_EVENT ) ).Subscribe( this, OnConsoleClosed );

			builder.TextEntered.Subscribe( this, OnCommandExecuted );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_textEntered?.Dispose();
			_unknownCommand?.Dispose();
			_commandExecuted?.Dispose();
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
		public void ExecuteCommand( string text ) {
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
			return _commandBuilder.GetArgumentAt( index );
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
			return _commandBuilder.GetArgs();
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
			string textCommand = _commandBuilder.ArgumentCount > 0 ? _commandBuilder.GetArgumentAt( 0 ) : String.Empty;

			if ( !string.IsNullOrEmpty( textCommand ) && _commandService.TryGetCommand( textCommand, out ConsoleCommand consoleCommand ) ) {
				var arguments = _commandBuilder.GetArgs();

				try {
					_commandExecuted.PublishAsync( new CommandExecutedEventData( consoleCommand, arguments.Length ) );
				} catch ( Exception ex ) {
					_logger.PrintError( $"CommandLine.OnCommandExecuted: error executing command - {ex.Message}" );
					throw;
				}
			} else if ( !string.IsNullOrEmpty( textCommand ) ) {
				_unknownCommand.Publish( new CommandExecutedEventData() );
				_logger.PrintWarning( $"CommandLine.OnCommandExecuted: command '{textCommand}' not found." );
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
		private void OnConsoleClosed( in EmptyEventArgs args ) {
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
			_suggestions.Clear();
			_currentSuggest = 0;
			_suggesting = false;
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