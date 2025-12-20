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

using NomadCore.GameServices;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Infrastructure;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.ConsoleSystem.Services {
	/*
	===================================================================================
	
	CommandCacheService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class CommandCacheService : ICommandService {
		private readonly ConcurrentDictionary<InternString, ConsoleCommand> _commands = new ConcurrentDictionary<InternString, ConsoleCommand>();
		private readonly ILoggerService _logger;

		/*
		===============
		CommandCacheServices
		===============
		*/
		public CommandCacheService( ILoggerService logger ) {
			_logger = logger;

			RegisterCommand( new ConsoleCommand(
				name: "cmdlist",
				callback: OnListCommands,
				description: "Prints all available commands to the console."
			) );
		}
		
		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_commands.Clear();
		}

		/*
		===============
		RegisterCommand
		===============
		*/
		/// <summary>
		/// Registers a command into the global cache
		/// </summary>
		/// <param name="command">The command that's being added to the global cache.</param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void RegisterCommand( ConsoleCommand command ) {
			_commands[ command.Name ] = command;
		}

		/*
		===============
		GetCommand
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ConsoleCommand GetCommand( string command ) {
			ArgumentException.ThrowIfNullOrEmpty( command );
			return _commands[ new( command ) ];
		}

		/*
		===============
		TryGetCommand
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="command"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryGetCommand( string name, out ConsoleCommand command ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			return _commands.TryGetValue( new( name ), out command );
		}

		/*
		===============
		CommandExists
		===============
		*/
		/// <summary>
		/// Checks if a command by the name of <paramref name="command"/> exists in the cache.
		/// </summary>
		/// <param name="command">The command to check for.</param>
		/// <returns>True if <paramref name="command"/> has been registered.</returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool CommandExists( string command ) {
			ArgumentException.ThrowIfNullOrEmpty( command );
			return _commands.ContainsKey( new( command ) );
		}

		/*
		===============
		OnList_commands
		===============
		*/
		/// <summary>
		/// Lists all _commands currently stored in <see cref="_commands"/>.
		/// </summary>
		/// <param name="args"></param>
		private void OnListCommands( in CommandExecutedEventData args ) {
			IConsoleCommand[] commandList = [ .. _commands.Values ];
			
			for ( int i = 0; i < commandList.Length; i++ ) {
				_logger.PrintLine( $"{commandList[ i ].Name}: {commandList[ i ].Description}" );
			}
		}
	};
};