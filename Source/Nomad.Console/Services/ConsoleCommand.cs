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

using NomadCore.Domain.Models.Interfaces;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.ConsoleSystem.Events;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using System;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure {
	/*
	===================================================================================
	
	ConsoleCommand
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public record ConsoleCommand : IConsoleCommand {
		public InternString Name => _name;
		private readonly InternString _name;

		public InternString Description => _description;
		private readonly InternString _description;

		public IGameEvent<CommandExecutedEventData>.EventCallback Callback => _callback;
		private readonly IGameEvent<CommandExecutedEventData>.EventCallback _callback;

		/*
		===============
		ConsoleCommand
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="callback"></param>
		public ConsoleCommand( string name, IGameEvent<CommandExecutedEventData>.EventCallback callback ) {
			ArgumentException.ThrowIfNullOrEmpty( name );
			ArgumentNullException.ThrowIfNull( callback );

			_name = new( name );
			_callback = callback;
			_description = InternString.Empty;
		}

		/*
		===============
		ConsoleCommand
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="callback"></param>
		/// <param name="description"></param>
		public ConsoleCommand( string name, IGameEvent<CommandExecutedEventData>.EventCallback callback, string description )
			: this( name, callback )
		{
			ArgumentException.ThrowIfNullOrEmpty( description );
			_description = StringPool.Intern( description );
		}
	};
};