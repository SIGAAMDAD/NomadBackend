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
using Nomad.Console;
using Nomad.Console.Events;
using Nomad.Core.Engine.Services;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.Logger;

namespace Nomad.EngineUtils.Godot.Private.Console {
	/*
	===================================================================================

	GodotConsole

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class GodotConsole : IConsoleObject, IDisposable {
		public bool IsOpen => _isOpen;
		private bool _isOpen = false;

		private readonly ILoggerService _logger;
		private readonly IGameEventRegistryService _eventRegistry;

		private readonly CanvasLayer _canvasLayer;

		private readonly GodotConsoleInput _inputLine;
		private readonly GodotConsoleInputRouter _inputRouter;
		private readonly GodotConsoleOutputView _outputView;
		private readonly GodotConsoleSink _sink;

		private readonly ConsoleHistoryBuffer _history;
		private readonly ConsoleCommandHandler _commandHandler;

		private readonly ISubscriptionHandle _toggleRequested;

		private readonly ISubscriptionHandle _pageUp;
		private readonly ISubscriptionHandle _pageDown;

		private readonly ISubscriptionHandle _historyPrevious;
		private readonly ISubscriptionHandle _historyNext;

		public IGameEvent<ConsoleOpenedEventArgs> ConsoleOpened => _consoleOpenedEvent;
		private readonly IGameEvent<ConsoleOpenedEventArgs> _consoleOpenedEvent;

		public IGameEvent<ConsoleClosedEventArgs> ConsoleClosed => _consoleClosedEvent;
		private readonly IGameEvent<ConsoleClosedEventArgs> _consoleClosedEvent;

		private bool _isDisposed = false;

		/*
		===============
		GodotConsole
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="logger"></param>
		/// <param name="eventRegistry"></param>
		/// <param name="fileSystem"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public GodotConsole(
			Node owner,
			ILoggerService logger,
			IGameEventRegistryService eventRegistry,
			IFileSystem fileSystem
		) {
			ArgumentNullException.ThrowIfNull( owner );
			ArgumentNullException.ThrowIfNull( fileSystem );

			_logger = logger ?? throw new ArgumentNullException( nameof( logger ) );
			_eventRegistry = eventRegistry ?? throw new ArgumentNullException( nameof( eventRegistry ) );

			_canvasLayer = new CanvasLayer() {
				Name = nameof( GodotConsole ),
				Visible = false,
				ProcessMode = Node.ProcessModeEnum.Always
			};

			_inputLine = new GodotConsoleInput( _eventRegistry );
			_inputRouter = new GodotConsoleInputRouter( _eventRegistry, _inputLine );
			_outputView = new GodotConsoleOutputView();
			_sink = new GodotConsoleSink( _outputView );

			_history = new ConsoleHistoryBuffer(
				_eventRegistry,
				new ConsoleHistoryStore( fileSystem ),
				256
			);

			_commandHandler = new ConsoleCommandHandler(
				_eventRegistry,
				new ConsoleCommandParser(),
				_history
			);

			_consoleClosedEvent = _eventRegistry
				.GetEvent<ConsoleClosedEventArgs>(
					ConsoleClosedEventArgs.Name,
					ConsoleClosedEventArgs.NameSpace
				);

			_consoleOpenedEvent = _eventRegistry
				.GetEvent<ConsoleOpenedEventArgs>(
					ConsoleOpenedEventArgs.Name,
					ConsoleOpenedEventArgs.NameSpace
				);

			_toggleRequested = _eventRegistry
				.GetEvent<ConsoleToggleRequestedEventArgs>(
					ConsoleToggleRequestedEventArgs.Name,
					ConsoleToggleRequestedEventArgs.NameSpace
				)
				.Subscribe( OnToggleRequested );

			_pageUp = _eventRegistry
				.GetEvent<PageUpEventArgs>(
					PageUpEventArgs.Name,
					PageUpEventArgs.NameSpace
				)
				.Subscribe( OnPageUp );

			_pageDown = _eventRegistry
				.GetEvent<PageDownEventArgs>(
					PageDownEventArgs.Name,
					PageDownEventArgs.NameSpace
				)
				.Subscribe( OnPageDown );

			_historyPrevious = _eventRegistry
				.GetEvent<HistoryPrevEventArgs>(
					HistoryPrevEventArgs.Name,
					HistoryPrevEventArgs.NameSpace
				)
				.Subscribe( OnHistoryPrevious );

			_historyNext = _eventRegistry
				.GetEvent<HistoryNextEventArgs>(
					HistoryNextEventArgs.Name,
					HistoryNextEventArgs.NameSpace
				)
				.Subscribe( OnHistoryNext );

			_canvasLayer.CallDeferred( CanvasLayer.MethodName.AddChild, _outputView.Node );
			_canvasLayer.CallDeferred( CanvasLayer.MethodName.AddChild, _inputLine );
			_canvasLayer.CallDeferred( CanvasLayer.MethodName.AddChild, _inputRouter );

			owner.CallDeferred( Node.MethodName.AddChild, _canvasLayer );

			_logger.AddSink( _sink );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose() {
			if ( _isDisposed ) {
				return;
			}

			_historyNext?.Dispose();
			_historyPrevious?.Dispose();

			_pageDown?.Dispose();
			_pageUp?.Dispose();

			_toggleRequested?.Dispose();

			_commandHandler?.Dispose();
			_history?.Dispose();

			_sink?.Dispose();
			_outputView?.Dispose();

			_inputRouter?.Dispose();
			_inputLine?.Dispose();
			_canvasLayer?.Dispose();

			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		Show
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Show() {
			if ( _isOpen ) {
				return;
			}

			_isOpen = true;

			_canvasLayer.Show();
			_inputLine.Open();
			_outputView.ScrollToBottom();

			_consoleOpenedEvent.Publish( default );
		}

		/*
		===============
		Hide
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Hide() {
			if ( !_isOpen ) {
				return;
			}

			_isOpen = false;

			_inputLine.Close();
			_canvasLayer.Hide();

			_consoleClosedEvent.Publish( default );
		}

		/*
		===============
		Toggle
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Toggle() {
			if ( _isOpen ) {
				Hide();
			} else {
				Show();
			}
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Clear() {
			_outputView.Clear();
		}

		/*
		===============
		Print
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="message"></param>
		public void Print( string message ) {
			_outputView.Print( message );
		}

		/*
		===============
		ExecuteText
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="text"></param>
		public void ExecuteText( string text ) {
			_commandHandler.ExecuteText( text );
		}

		/*
		===============
		OnToggleRequested
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnToggleRequested( in ConsoleToggleRequestedEventArgs args ) {
			Toggle();
		}

		/*
		===============
		OnPageUp
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnPageUp( in PageUpEventArgs args ) {
			if ( _isOpen ) {
				_outputView.PageUp();
			}
		}

		/*
		===============
		OnPageDown
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnPageDown( in PageDownEventArgs args ) {
			if ( _isOpen ) {
				_outputView.PageDown();
			}
		}

		/*
		===============
		OnHistoryPrevious
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnHistoryPrevious( in HistoryPrevEventArgs args ) {
			if ( _isOpen ) {
				_inputLine.ApplyHistoryText( args.Text );
			}
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
		private void OnHistoryNext( in HistoryNextEventArgs args ) {
			if ( !_isOpen ) {
				return;
			}

			if ( args.EndReached ) {
				_inputLine.Clear();
				return;
			}

			_inputLine.ApplyHistoryText( args.Text );
		}
	};
};
