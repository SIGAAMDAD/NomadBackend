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
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;

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
		private const string CONSOLE_HISTORY_FILE = "user://history.txt";
		private const int CONSOLE_HISTORY_MAX = 32;

		private readonly Queue<string> _consoleHistory = new Queue<string>();
		private readonly FilePath _historyPath;
		private int _historyIndex = 0;

		public IGameEvent<HistoryPrevEventData> HistoryPrev => _historyPrev;
		private readonly IGameEvent<HistoryPrevEventData> _historyPrev;

		public IGameEvent<HistoryNextEventData> HistoryNext => _historyNext;
		private readonly IGameEvent<HistoryNextEventData> _historyNext;

		private readonly ILoggerService _logger;

		/*
		===============
		History
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="builder"></param>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		/// <param name="events"></param>
		public History( ICommandBuilder builder, ILoggerService logger, IGameEventRegistryService eventFactory ) {
			ArgumentNullException.ThrowIfNull( builder );
			ArgumentNullException.ThrowIfNull( eventFactory );
			ArgumentNullException.ThrowIfNull( logger );

			_logger = logger;

			builder.TextEntered.Subscribe( this, OnTextEntered );

			eventFactory.GetEvent<IEventArgs>( nameof( HistoryPrev ) ).Subscribe( this, OnHistoryPrev );
			eventFactory.GetEvent<IEventArgs>( nameof( HistoryNext ) ).Subscribe( this, OnHistoryNext );

			_historyPrev = eventFactory.GetEvent<HistoryPrevEventData>( nameof( HistoryPrev ) );
			_historyNext = eventFactory.GetEvent<HistoryNextEventData>( nameof( HistoryNext ) );

			_historyPath = new FilePath( CONSOLE_HISTORY_FILE, PathType.User );

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
			_consoleHistory.Clear();
			_historyIndex = 0;
			File.Delete( _historyPath.OSPath );
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
			_logger?.PrintLine( "History.SaveHistory: writing command line history to disk..." );
			try {
				using StreamWriter writer = new StreamWriter( _historyPath.OSPath );
				foreach ( var history in _consoleHistory ) {
					writer.WriteLine( history );
				}
			} catch ( Exception e ) {
				_logger?.PrintError( $"History.SaveHistory: couldn't write console command history data to file '{_historyPath}'! Exception: {e}" );
				throw;
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
		private void OnHistoryPrev( in IEventArgs args ) {
			if ( _historyIndex > 0 ) {
				_historyIndex--;
				if ( _historyIndex >= 0 ) {
					HistoryPrev.Publish( new HistoryPrevEventData( _consoleHistory.ElementAt( _historyIndex ) ) );
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
		private void OnHistoryNext( in IEventArgs args ) {
			if ( _historyIndex < _consoleHistory.Count ) {
				_historyIndex++;
				HistoryNext.Publish( new HistoryNextEventData( _historyIndex < _consoleHistory.Count, _consoleHistory.ElementAt( _historyIndex ) ) );
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
			if ( _consoleHistory.Count >= CONSOLE_HISTORY_MAX ) {
				_consoleHistory.Dequeue();
			}
			_consoleHistory.Enqueue( text );
			_historyIndex = _consoleHistory.Count;
		}
	};
};