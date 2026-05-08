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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Nomad.Core;
using System.Collections.Concurrent;
using Nomad.Core.Logger;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;

namespace Nomad.Logger.Private.Services
{
	/*
	===================================================================================
	
	LoggerService
	
	===================================================================================
	*/
	/// <summary>
	/// Handles the process of logging.
	/// </summary>

	internal sealed class LoggerService : ILoggerService
	{
		private ICVar<LogLevel>? _logDepth;
		private readonly List<ILoggerSink> _sinks = new List<ILoggerSink>();

		private readonly ILoggerCategory _defaultCategory;
		private readonly ConcurrentDictionary<string, LoggerCategory> _categories;
		private readonly MessageBuilder _messageBuilder = new MessageBuilder();

		private bool _isDisposed = false;

		/*
		===============
		LoggerService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public LoggerService()
		{
			_defaultCategory = new LoggerCategory( "Logger", LogLevel.Info, true, _messageBuilder );
			_categories = new ConcurrentDictionary<string, LoggerCategory> {
				["Default"] = _defaultCategory as LoggerCategory
			};
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
			if ( !_isDisposed ) {
				for ( int i = 0; i < _sinks.Count; i++ ) {
					_sinks[i]?.Dispose();
				}
				_sinks?.Clear();
				foreach ( var category in _categories ) {
					category.Value?.Dispose();
				}
				_categories?.Clear();
				_defaultCategory?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		CreateCategory
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="level"></param>
		/// <param name="enabled"></param>
		/// <returns></returns>
		public ILoggerCategory CreateCategory( string name, LogLevel level, bool enabled )
		{
			var category = new LoggerCategory( name, level, enabled, _messageBuilder );
			for ( int i = 0; i < _sinks.Count; i++ ) {
				category.AddSink( _sinks[i] );
			}
			return category;
		}

		/*
		===============
		InitConfig
		===============
		*/
		/// <summary>
		/// Initializes the logging service.
		/// </summary>
		/// <param name="cvarSystem"></param>
		public void InitConfig( ICVarSystemService cvarSystem )
		{
			_logDepth = cvarSystem.Register(
				new CVarCreateInfo<LogLevel> {
					Name = Constants.CVars.Console.CONSOLE_LOG_LEVEL,
					DefaultValue = LogLevel.Info,
					Description = "The verbosity of the logger.",
					Flags = CVarFlags.Archive,
					Validator = value => value >= LogLevel.Error && value < LogLevel.Count
				}
			);
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
		public void AddSink( ILoggerSink sink )
		{
			ArgumentGuard.ThrowIfNull( sink );
			_sinks.Add( sink );

			foreach ( var category in _categories ) {
				category.Value.AddSink( sink );
			}
		}

		/*
		===============
		PrintMessage
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="category"></param>
		/// <param name="level"></param>
		/// <param name="message"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private void PrintMessage( ILoggerCategory category, LogLevel level, string message )
		{
			if ( level < LogLevel.Info || level >= _logDepth?.Value ) {
				return;
			}
			category.PrintLine( message );
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
			PrintMessage( _defaultCategory, LogLevel.Info, message );
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
			PrintMessage( _defaultCategory, LogLevel.Debug, message );
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
			PrintMessage( _defaultCategory, LogLevel.Warning, message );
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
			PrintMessage( _defaultCategory, LogLevel.Error, message );
		}

		/*
		===============
		Clear
		===============
		*/
		/// <summary>
		/// Clears all sinks.
		/// </summary>
		public void Clear()
		{
			for ( int i = 0; i < _sinks.Count; i++ ) {
				_sinks[i].Clear();
			}
		}
	};
};
