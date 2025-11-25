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
using NomadCore.Interfaces;
using NomadCore.Systems.ConsoleSystem.Events;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using NomadCore.Infrastructure;
using NomadCore.Systems.ConsoleSystem.CVars.Common;

namespace NomadCore.Systems.ConsoleSystem {
	/*
	===================================================================================
	
	History
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class History : IHistory {
		private static readonly string CONSOLE_HISTORY_FILE = "user://history.txt";
		private const int CONSOLE_HISTORY_MAX = 32;

		private readonly Queue<string> ConsoleHistory = new Queue<string>();
		private readonly string HistoryPath;
		private int HistoryIndex = 0;

		public IGameEvent<HistoryPrevEventData> HistoryPrev => _historyPrev;
		private readonly IGameEvent<HistoryPrevEventData> _historyPrev;

		public IGameEvent<HistoryNextEventData> HistoryNext => _historyNext;
		private readonly IGameEvent<HistoryNextEventData> _historyNext;

		private readonly ILoggerService Logger;

		/*
		===============
		History
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="eventBus"></param>
		/// <param name="console"></param>
		/// <param name="events"></param>
		public History( ICommandBuilder? builder, IGameEventBusService? eventBus, IConsoleEvents? events ) {
			ArgumentNullException.ThrowIfNull( builder );
			ArgumentNullException.ThrowIfNull( eventBus );
			ArgumentNullException.ThrowIfNull( events );

			Logger = ServiceRegistry.Get<ILoggerService>();

			builder.TextEntered.Subscribe( this, OnTextEntered );

			events.HistoryPrev.Subscribe( this, OnHistoryPrev );
			events.HistoryNext.Subscribe( this, OnHistoryNext );

			_historyPrev = eventBus.CreateEvent<HistoryPrevEventData>( nameof( HistoryPrev ) );
			_historyNext = eventBus.CreateEvent<HistoryNextEventData>( nameof( HistoryNext ) );

			HistoryPath = ProjectSettings.GlobalizePath( CONSOLE_HISTORY_FILE );

			LoadHistory();
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// Clears the console history buffer and deletes the history file.
		/// </summary>
		public void Clear() {
			ConsoleHistory.Clear();
			HistoryIndex = 0;
			File.Delete( HistoryPath );
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
		public void OnTextEntered( in TextEnteredEventData args ) {
			AddInputHistory( args.Text );
			SaveHistory();
		}

		/*
		===============
		SaveHistory
		===============
		*/
		/// <summary>
		/// Saves the console history list to <see cref="CONSOLE_HISTORY_FILE"/>.
		/// </summary>
		private void SaveHistory() {
			Logger?.PrintLine( "History.SaveHistory: writing command line history to disk..." );
			try {
				using StreamWriter writer = new StreamWriter( HistoryPath );
				foreach ( var history in ConsoleHistory ) {
					writer.WriteLine( history );
				}
			} catch ( Exception e ) {
				Logger?.PrintError( $"History.SaveHistory: couldn't write console command history data to file '{HistoryPath}'! Exception: {e}" );
			}
		}

		/*
		===============
		OnHistoryPrev
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		private void OnHistoryPrev( in IGameEvent eventData, in IEventArgs args ) {
			if ( HistoryIndex > 0 ) {
				HistoryIndex--;
				if ( HistoryIndex >= 0 ) {
					HistoryPrev.Publish( new HistoryPrevEventData( ConsoleHistory.ElementAt( HistoryIndex ) ) );
				}
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
		private void OnHistoryNext( in IGameEvent eventData, in IEventArgs args ) {
			if ( HistoryIndex < ConsoleHistory.Count ) {
				HistoryIndex++;
				HistoryNext.Publish( new HistoryNextEventData( HistoryIndex < ConsoleHistory.Count, ConsoleHistory.ElementAt( HistoryIndex ) ) );
			}
		}

		/*
		===============
		LoadHistory
		===============
		*/
		private void LoadHistory() {
			try {
				using StreamReader reader = new StreamReader( CONSOLE_HISTORY_FILE );

				string? text;
				while ( ( text = reader.ReadLine() ) != null ) {
					AddInputHistory( text );
				}
			} catch ( Exception ) {
			}
		}

		/*
		===============
		AddInputHistory
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		private void AddInputHistory( string text ) {
			if ( ConsoleHistory.Count >= CONSOLE_HISTORY_MAX ) {
				ConsoleHistory.Dequeue();
			}
			ConsoleHistory.Enqueue( text );
			HistoryIndex = ConsoleHistory.Count;
		}
	};
};