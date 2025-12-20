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
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure.Godot {
	/*
	===================================================================================
	
	GodotConsole
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public sealed partial class GodotConsole : Control {
		[Export]
		public bool Enabled { get; private set; } = true;
		[Export]
		public bool EnableOnReleaseBuild { get; private set; } = true;
		[Export]
		public bool PauseEnabled { get; private set; } = false;

		private readonly GodotCommandBuilder _commandBuilder;
		
		private readonly IGameEvent<EmptyEventArgs> _consoleOpened;
		private readonly IGameEvent<EmptyEventArgs> _consoleClosed;
		private readonly IGameEvent<HistoryPrevEventData> _historyPrev;
		private readonly IGameEvent<HistoryNextEventData> _historyNext;
		private readonly IGameEvent<EmptyEventArgs> _pageDown;
		private readonly IGameEvent<EmptyEventArgs> _pageUp;

		private bool _wasPausedAlready = false;

		/*
		===============
		GodotConsole
		===============
		*/
		public GodotConsole( ICommandBuilder builder, ICommandService commands, IGameEventRegistryService eventRegistry ) {
			ArgumentNullException.ThrowIfNull( builder );

			_consoleClosed = eventRegistry.GetEvent<EmptyEventArgs>( new( Constants.CONSOLE_CLOSED_EVENT ) );
			_consoleOpened = eventRegistry.GetEvent<EmptyEventArgs>( new( Constants.CONSOLE_OPENED_EVENT ) );
			_historyNext = eventRegistry.GetEvent<HistoryNextEventData>( new( Constants.HISTORY_NEXT_EVENT ) );
			_historyPrev = eventRegistry.GetEvent<HistoryPrevEventData>( new( Constants.HISTORY_PREV_EVENT ) );
			_pageDown = eventRegistry.GetEvent<EmptyEventArgs>( new( Constants.PAGE_DOWN_EVENT ) );
			_pageUp = eventRegistry.GetEvent<EmptyEventArgs>( new( Constants.PAGE_UP_EVENT ) );
			_commandBuilder = builder as GodotCommandBuilder ?? throw new InvalidOperationException( "Cannot create a GodotConsole without a GodotCommandBuilder!" );

			commands.RegisterCommand( new ConsoleCommand( "quit", OnQuit, "Closes the game application." ) );
			commands.RegisterCommand( new ConsoleCommand( "exit", OnQuit, "Exits the running application." ) );

			AddThemeFontOverride( "SourceCodePro-ExtraLight", ResourceLoader.Load<Font>( "res://Assets/Fonts/SourceCodePro-ExtraLight.ttf" ) );
		}

		/*
		===============
		ToggleConsole
		===============
		*/
		/// <summary>
		/// Handles internal processing mode flip-flopping.
		/// </summary>
		public void ToggleConsole() {
			if ( Enabled ) {
				Visible = !Visible;
			} else {
				Visible = false;
			}

			if ( Visible ) {
				_wasPausedAlready = GetTree().Paused;
				GetTree().Paused = _wasPausedAlready || PauseEnabled;
				_consoleOpened.Publish( EmptyEventArgs.Args );
			} else {
				AnchorBottom = 1.0f;
				if ( PauseEnabled && !_wasPausedAlready ) {
					GetTree().Paused = false;
				}
				_consoleClosed.Publish( EmptyEventArgs.Args );
			}
		}

		/*
		===============
		ToggleSize
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void ToggleSize() {
			AnchorBottom = AnchorBottom == 1.0f ? 1.9f : 1.0f;
		}

		/*
		===============
		HandleOpenConsole
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyEvent"></param>
		private void HandleToggleConsole( InputEventKey keyEvent ) {
			if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Quoteleft ) {
				if ( keyEvent.IsCommandOrControlPressed() ) {
					if ( Visible ) {
						ToggleSize();
					} else {
						ToggleConsole();
						ToggleSize();
					}
					GetViewport().SetInputAsHandled();
				} else {
					ToggleConsole();
					GetViewport().SetInputAsHandled();
				}
			} else if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Escape && Visible ) {
				ToggleConsole();
				GetViewport().SetInputAsHandled();
			}
		}

		/*
		===============
		OnQuit
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		private void OnQuit( in CommandExecutedEventData args ) {
			GetTree().Quit();
		}

		/*
		===============
		_EnterTree
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public override void _EnterTree() {
			base._EnterTree();

			Name = nameof( GodotConsole );
			AnchorBottom = 1.0f;
			AnchorRight = 1.0f;
			Visible = false;
			TopLevel = true;

			ProcessMode = ProcessModeEnum.Always;

			AddChild( _commandBuilder );
		}

		/*
		===============
		_Input
		===============
		*/
		/// <summary>
		/// Handles input for the console
		/// </summary>
		/// <param name="event"></param>
		public override void _Input( InputEvent @event ) {
			if ( @event is InputEventKey keyEvent && keyEvent != null && keyEvent.Pressed ) {
				// ~ or ESC key
				HandleToggleConsole( keyEvent );

				if ( Visible ) {
					if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Up ) {
						GetViewport().SetInputAsHandled();
						_historyPrev.Publish( new HistoryPrevEventData() );
					} else if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Down ) {
						GetViewport().SetInputAsHandled();
						_historyNext.Publish( new HistoryNextEventData() );
					} else if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Pageup ) {
						GetViewport().SetInputAsHandled();
						_pageUp.Publish( EmptyEventArgs.Args );
					} else if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Pagedown ) {
						GetViewport().SetInputAsHandled();
						_pageDown.Publish( EmptyEventArgs.Args );
					} else if ( keyEvent.GetPhysicalKeycodeWithModifiers() == Key.Tab ) {
						GetViewport().SetInputAsHandled();
						//Events.AutoComplete.Publish( EmptyEventArgs.Args );
					}
				}
			}
		}
	};
};