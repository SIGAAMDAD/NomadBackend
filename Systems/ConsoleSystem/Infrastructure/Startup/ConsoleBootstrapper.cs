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
using NomadCore.GameServices;
using NomadCore.Infrastructure.ServiceRegistry.Interfaces;
using NomadCore.Systems.ConsoleSystem.Infrastructure.Sinks;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using NomadCore.Systems.ConsoleSystem.Services;
using System;

namespace NomadCore.Systems.ConsoleSystem.Infrastructure.Startup {
	/*
	===================================================================================

	ConsoleBootstrapper

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public static class ConsoleBootstrapper {
		/*
		===============
		Console
		===============
		*/
		public static void Initialize( IServiceLocator services, IServiceRegistry registry, Node rootNode ) {
			ArgumentNullException.ThrowIfNull( rootNode );
			
			var logger = registry.RegisterSingleton<ILoggerService, LoggerService>();

			var eventFactory = services.GetService<IGameEventRegistryService>();
			var eventBus = services.GetService<IGameEventBusService>();
			var cvarSystem = services.GetService<ICVarSystemService>();

			var consoleOpened = eventFactory.GetEvent<IEventArgs>( "ConsoleOpened" );
			var consoleClosed = eventFactory.GetEvent<IEventArgs>( "ConsoleClosed" );
			var historyPrev = eventFactory.GetEvent<IEventArgs>( "HistoryPrev" );
			var historyNext = eventFactory.GetEvent<IEventArgs>( "HistoryNext" );
			var autoComplete = eventFactory.GetEvent<IEventArgs>( "AutoComplete" );
			var pageUp = eventFactory.GetEvent<IEventArgs>( "PageUp" );
			var pageDown = eventFactory.GetEvent<IEventArgs>( "PageDown" );

			var commandBuilder = new GodotCommandBuilder( eventBus, eventFactory );
			var commandService = registry.RegisterSingleton<ICommandService>( new CommandCacheService( logger ) );
			var console = new GodotConsole( commandBuilder, commandService, eventFactory );
			var commandLine = registry.RegisterSingleton<ICommandLine>( new CommandLine( commandBuilder, commandService, logger, eventFactory ) );

			logger.AddSink( new GodotSink() );
			logger.AddSink( new FileSink( cvarSystem ) );
			logger.AddSink( new InGameSink( console, commandBuilder, eventFactory ) );

			var history = new History( commandBuilder, logger, eventFactory );

			rootNode.CallDeferred( Node.MethodName.AddChild, console );
		}
	};
};