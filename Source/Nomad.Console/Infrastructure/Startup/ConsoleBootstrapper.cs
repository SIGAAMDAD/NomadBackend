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
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Infrastructure.ServiceRegistry.Interfaces;
using NomadCore.Systems.ConsoleSystem.CVars.Services;
using NomadCore.Systems.ConsoleSystem.Infrastructure.Sinks;
using NomadCore.Systems.ConsoleSystem.Infrastructure.Godot;
using NomadCore.Systems.ConsoleSystem.Interfaces;
using NomadCore.Systems.ConsoleSystem.Services;
using System;
using NomadCore.Domain;

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
			
			LoggerService logger = services.GetService<ILoggerService>() as LoggerService;
			var eventFactory = services.GetService<IGameEventRegistryService>();
			var eventBus = services.GetService<IGameEventBusService>();

			var cvarSystem = registry.RegisterSingleton<ICVarSystemService>( new CVarSystem( eventFactory, logger ) );
			var configFile = cvarSystem.Register(
				new CVarCreateInfo<string>(
					Name: new( Constants.CVars.Console.DEFAULT_CONFIG_FILE ),
					DefaultValue: new( "res://Assets/Config/default.ini" ),
					Description: new( "The default configuration file." ),
					Flags: CVarFlags.Init | CVarFlags.ReadOnly
				)
			);
			cvarSystem.Load( configFile.Value );

			logger.Init( services );

			logger.AddSink( new GodotSink() );
			logger.AddSink( new FileSink( cvarSystem ) );

			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.CONSOLE_OPENED_EVENT ) );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.CONSOLE_CLOSED_EVENT ) );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.HISTORY_PREV_EVENT ) );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.HISTORY_NEXT_EVENT ) );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.AUTOCOMPLETE_EVENT ) );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.PAGE_UP_EVENT ) );
			eventFactory.GetEvent<EmptyEventArgs>( new( Constants.Events.Console.PAGE_DOWN_EVENT ) );

			var commandBuilder = new GodotCommandBuilder( eventBus, eventFactory );
			var commandService = registry.RegisterSingleton<ICommandService>( new CommandCacheService( logger ) );
			logger.InitCommandService( commandService );

			var console = new GodotConsole( commandBuilder, commandService, eventFactory );
			logger.InitCommandLineService(
				registry.RegisterSingleton<ICommandLineService>( new CommandLine( commandBuilder, commandService, logger, eventFactory ) )
			);

			logger.AddSink( new InGameSink( console, commandBuilder, eventFactory ) );

			rootNode.CallDeferred( Node.MethodName.AddChild, console );
		}
	};
};