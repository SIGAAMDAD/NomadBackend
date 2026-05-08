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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Collections;
using Nomad.Core.Logger;

namespace Nomad.Logger.Private
{
	/*
	===================================================================================

	LoggerCategory

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class LoggerCategory : ILoggerCategory
	{
		public string Name => _name;
		private readonly string _name;

		public LogLevel Level => _level;
		private readonly LogLevel _level;

		public bool Enabled {
			get => _enabled;
			set => _enabled = value;
		}
		private bool _enabled;

		private readonly List<ILoggerSink> _sinks = new List<ILoggerSink>();
		private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
		private readonly MessageBuilder _builder;

		/*
		===============
		LoggerCategory
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="level"></param>
		/// <param name="enabled"></param>
		/// <param name="builder"></param>
		public LoggerCategory( string name, LogLevel level, bool enabled, MessageBuilder builder )
		{
			_name = name;
			_level = level;
			_enabled = enabled;
			_builder = builder;

			var printThread = new Thread( LoggerThreadAsync ) {
				IsBackground = true
			};
			printThread.Start();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose()
		{
			_sinks.Clear();
		}

		/*
		===============
		AddSink
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sink"></param>
		public void AddSink( ILoggerSink sink )
		{
			_sinks.Add( sink );
		}

		/*
		===============
		RemoveSink
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sink"></param>
		public void RemoveSink( ILoggerSink sink )
		{
			_sinks.Remove( sink );
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
		public void PrintLine( string message )
		{
			_messageQueue.Enqueue( _builder.FormatMessage( this, LogLevel.Info, message, true ) );
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
		public void PrintWarning( string message )
		{
			_messageQueue.Enqueue( message );
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
		public void PrintError( string message )
		{
			_messageQueue.Enqueue( message );
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
		public void PrintDebug( string message )
		{
			_messageQueue.Enqueue( message );
		}

		/*
		===============
		LoggerThreadAsync
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		private async void LoggerThreadAsync()
		{
			try {
				while ( true ) {
					while ( _messageQueue.TryDequeue( out var message ) ) {
						for ( int i = 0; i < _sinks.Count; i++ ) {
							_sinks[i].Print( message );
						}
					}
					await Task.Delay( 250 );
				}
			} catch ( Exception e ) {
				global::System.Console.WriteLine( $"LoggerThreadAsync: exception caught - {e}" );
			}
		}
	};
};
