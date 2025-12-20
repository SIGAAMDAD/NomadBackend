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
using NomadCore.Domain;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using NomadCore.Systems.ConsoleSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure.Godot {
	/*
	===================================================================================
	
	GodotCommandBuilder
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed partial class GodotCommandBuilder : LineEdit, ICommandBuilder {
		public int ArgumentCount => _arguments.Count;

		private readonly IGameEventBusService _eventBus;

		private readonly List<string> _arguments = new List<string>();
		private readonly StringBuilder _commandBuilder = new StringBuilder();

		public IGameEvent<TextEnteredEventData> TextEntered => _textEntered;
		private readonly IGameEvent<TextEnteredEventData> _textEntered;

		/*
		===============
		GodotCommandBuilder
		===============
		*/
		public GodotCommandBuilder( IGameEventBusService eventBus, IGameEventRegistryService eventFactory ) {
			ArgumentNullException.ThrowIfNull( eventBus );
			ArgumentNullException.ThrowIfNull( eventFactory );

			_eventBus = eventBus;

			_textEntered = eventFactory.GetEvent<TextEnteredEventData>( new( Constants.Events.Console.TEXT_ENTERED_EVENT ) );

			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.CONSOLE_OPENED_EVENT ) ).Subscribe( this, OnConsoleOpened );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.CONSOLE_CLOSED_EVENT ) ).Subscribe( this, OnConsoleClosed );
		}

		/*
		===============
		GetArgumentAt
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public string GetArgumentAt( int index ) {
			return _arguments[ index ];
		}

		/*
		===============
		GetArgs
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public string[] GetArgs() {
			return [ .. _arguments.Skip( 1 ) ];
		}

		/*
		===============
		OnHistoryPrev
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public void OnHistoryPrev( in HistoryPrevEventData args ) {
			//ResetAutocomplete();
			Text = args.Text;
		}

		/*
		===============
		OnHistoryNext
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		public void OnHistoryNext( in HistoryNextEventData args ) {
			if ( !args.EndReached ) {
				Text = args.Text;
				CaretColumn = Text.Length;
				//ResetAutocomplete();
			} else {
				Clear();
				//ResetAutocomplete();
			}
		}

		/*
		===============
		OnTextEntered
		===============
		*/
		public void OnTextEntered( string newText ) {
			//			ResetAutocomplete();
			Clear();
			CallDeferred( LineEdit.MethodName.Clear );

			if ( newText.Trim().Length > 0 ) {
				ParseLineInput( newText );
				TextEntered.Publish( new TextEnteredEventData() );
			}
		}

		/*
		===============
		ParseLineInput
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>s
		private void ParseLineInput( string text ) {
			bool inQuotes = false;
			bool escaped = false;

			_arguments.Clear();

			for ( int i = 0; i < text.Length; i++ ) {
				char c = text[ i ];

				if ( escaped ) {
					_commandBuilder.Append( c );
					escaped = false;
					continue;
				}

				switch ( c ) {
					case '\\':
						escaped = true;
						continue;
					case '\"':
						inQuotes = !inQuotes;
						continue;
					default:
						if ( char.IsWhiteSpace( c ) && !inQuotes ) {
							if ( _commandBuilder.Length > 0 ) {
								_arguments.Add( _commandBuilder.ToString() );
								_commandBuilder.Clear();
							}
							continue;
						}
						break;
				}

				_commandBuilder.Append( c );
			}

			if ( _commandBuilder.Length > 0 ) {
				_arguments.Add( _commandBuilder.ToString() );
			}
		}

		/*
		===============
		OnConsoleOpened
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="eventData"></param>
		/// <param name="args"></param>
		private void OnConsoleOpened( in EmptyEventArgs args ) {
			GrabFocus();
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
			Clear();
		}

		/*
		===============
		_Ready
		===============
		*/
		public override void _Ready() {
			base._Ready();

			Name = nameof( CommandLine );
			AnchorTop = 0.5f;
			AnchorRight = 1.0f;
			AnchorBottom = 0.5f;
			PlaceholderText = String.Empty;

			_eventBus.ConnectSignal( this, LineEdit.SignalName.TextSubmitted, this, Callable.From<string>( OnTextEntered ) );
		}
	};
};