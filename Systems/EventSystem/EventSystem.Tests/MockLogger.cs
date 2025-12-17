using NomadCore.Systems.EventSystem.Services;
using NomadCore.Systems.ConsoleSystem.Services;
using NomadCore.Interfaces;
using NomadCore.Infrastructure;
using NomadCore.Systems.ConsoleSystem.Infrastructure;
using NomadCore.Systems.ConsoleSystem.Infrastructure.Sinks;
using NUnit.Framework;
using NomadCore.Domain.Models.Interfaces;
using NUnit.Framework.Internal.Execution;
using NomadCore.Systems.EventSystem.Domain;
using NomadCore.GameServices;
using NomadCore.Systems.EventSystem.Domain.Registries;
using Microsoft.Testing.Platform.Logging;
using NomadCore.Infrastructure.ServiceRegistry.Interfaces;
using System;

namespace NomadCore.Systems.EventSystem.Tests {
	public class MockLogger : ILoggerService {
		public void AddSink( ILoggerSink sink ) {
		}
		public void Dispose() {
		}

		public void Init( IServiceLocator locator ) {
		}

		public void InitCommandLineService( IGameService commandLine ) {
		}

		public void InitCommandService( IGameService commandService ) {
		}

		public void PrintDebug( string message ) {
			Console.WriteLine( $"DEBUG: {message}" );
		}

		public void PrintError( string message ) {
			Console.WriteLine( $"ERROR: {message}" );
		}

		public void PrintLine( string message ) {
			Console.WriteLine( message );
		}

		public void PrintWarning( string message ) {
			Console.WriteLine( $"WARNING: {message}" );
		}
	};
};