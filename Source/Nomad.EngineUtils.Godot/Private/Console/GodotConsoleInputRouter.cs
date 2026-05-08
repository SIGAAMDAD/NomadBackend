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

using System;
using Godot;
using Nomad.Console.Events;
using Nomad.Console;
using Nomad.Core.Events;

namespace Nomad.EngineUtils.Godot.Private.Console
{
	/*
	===================================================================================
	
	GodotConsoleInputRouter
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed partial class GodotConsoleInputRouter : Control, IConsoleInputRouter
	{
		private readonly GodotConsoleInput _inputLine;

		public IGameEvent<ConsoleToggleRequestedEventArgs> ConsoleToggleRequested => _toggleRequestedEvent;
		private readonly IGameEvent<ConsoleToggleRequestedEventArgs> _toggleRequestedEvent;

		public IGameEvent<PageUpEventArgs> PageUp => _pageUpEvent;
		private readonly IGameEvent<PageUpEventArgs> _pageUpEvent;

		public IGameEvent<PageDownEventArgs> PageDown => _pageDownEvent;
		private readonly IGameEvent<PageDownEventArgs> _pageDownEvent;

		public IGameEvent<HistoryPrevRequestedEventArgs> HistoryPrevRequested => _historyPreviousRequestedEvent;
		private readonly IGameEvent<HistoryPrevRequestedEventArgs> _historyPreviousRequestedEvent;

		public IGameEvent<HistoryNextRequestedEventArgs> HistoryNextRequested => _historyNextRequestedEvent;
		private readonly IGameEvent<HistoryNextRequestedEventArgs> _historyNextRequestedEvent;

		public GodotConsoleInputRouter(
			IGameEventRegistryService eventRegistry,
			GodotConsoleInput inputLine
		)
		{
			ArgumentNullException.ThrowIfNull( eventRegistry );

			_inputLine = inputLine ?? throw new ArgumentNullException( nameof( inputLine ) );

			_toggleRequestedEvent = eventRegistry.GetEvent<ConsoleToggleRequestedEventArgs>(
				ConsoleToggleRequestedEventArgs.Name,
				ConsoleToggleRequestedEventArgs.NameSpace
			);

			_pageUpEvent = eventRegistry.GetEvent<PageUpEventArgs>(
				PageUpEventArgs.Name,
				PageUpEventArgs.NameSpace
			);

			_pageDownEvent = eventRegistry.GetEvent<PageDownEventArgs>(
				PageDownEventArgs.Name,
				PageDownEventArgs.NameSpace
			);

			_historyPreviousRequestedEvent = eventRegistry.GetEvent<HistoryPrevRequestedEventArgs>(
				HistoryPrevRequestedEventArgs.Name,
				HistoryPrevRequestedEventArgs.NameSpace
			);

			_historyNextRequestedEvent = eventRegistry.GetEvent<HistoryNextRequestedEventArgs>(
				HistoryNextRequestedEventArgs.Name,
				HistoryNextRequestedEventArgs.NameSpace
			);
		}

		public override void _Ready()
		{
			base._Ready();

			Name = nameof( GodotConsoleInputRouter );
			MouseFilter = MouseFilterEnum.Ignore;
			ProcessMode = ProcessModeEnum.Always;

			AnchorRight = 1.0f;
			AnchorBottom = 1.0f;
		}

		public override void _UnhandledInput( InputEvent @event )
		{
			base._UnhandledInput( @event );

			if ( @event is not InputEventKey key || !key.Pressed || key.Echo ) {
				return;
			}

			Key keyCode = key.GetPhysicalKeycodeWithModifiers();

			if ( keyCode == Key.Quoteleft ) {
				_toggleRequestedEvent.Publish( default );

				GetViewport().SetInputAsHandled();
				return;
			}

			if ( !_inputLine.IsOpen ) {
				return;
			}

			switch ( keyCode ) {
				case Key.Pageup:
					_pageUpEvent.Publish( default );
					GetViewport().SetInputAsHandled();
					break;
				case Key.Pagedown:
					_pageDownEvent.Publish( default );
					GetViewport().SetInputAsHandled();
					break;
				case Key.Up:
					_historyPreviousRequestedEvent.Publish( new HistoryPrevRequestedEventArgs( _inputLine.Text ) );
					GetViewport().SetInputAsHandled();
					break;
				case Key.Down:
					_historyNextRequestedEvent.Publish( new HistoryNextRequestedEventArgs( _inputLine.Text ) );
					GetViewport().SetInputAsHandled();
					break;
			}
		}
	};
};
