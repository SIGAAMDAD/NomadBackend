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

using NUnit.Framework;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.FileSystem;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.ServiceRegistry.Services;
using System;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry;
using Nomad.Core.Input;
using Nomad.Events;

namespace Nomad.Input.Tests
{
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bootstrap")]
	[Category("Integration")]
	[Category("UnitTests")]
	public class InputBootstrapperTests
	{
		private InputBootstrapper _bootstrapper;
		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;
		private IServiceLocator _serviceLocator;
		private ServiceCollection _serviceRegistry;

		[SetUp]
		public void Setup()
		{
			_serviceRegistry = new ServiceCollection();
			_serviceLocator = new ServiceLocator(_serviceRegistry);

			_bootstrapper = new InputBootstrapper();
			_eventRegistry = InputTestHelpers.CreateEventRegistry(out _logger);
		}

		[TearDown]
		public void TearDown()
		{
			_serviceLocator?.Dispose();
			_serviceRegistry?.Dispose();
			_eventRegistry?.Dispose();
			_logger?.Dispose();
		}

		[Test]
		public void CreateBootstrapper_InitializeWithValidServices_DoesNotThrow()
		{
			RegisterInputDependencies();

			Assert.DoesNotThrow(() => _bootstrapper.Initialize(_serviceRegistry, _serviceLocator));
		}

		[Test]
		public void CreateBootstrapper_InitializeWithoutRequiredServices_ThrowsInvalidOperationException()
		{
			Assert.Throws<InvalidOperationException>(() => _bootstrapper.Initialize(_serviceRegistry, _serviceLocator));
		}

		[Test]
		public void CreateBootstrapper_WithNullServiceRegistry_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => _bootstrapper.Initialize(null!, _serviceLocator));
		}

		[Test]
		public void CreateBootstrapper_WithNullServiceLocator_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => _bootstrapper.Initialize(_serviceRegistry, null!));
		}

		[Test]
		public void CreateBootstrapper_GetService_DoesNotThrow()
		{
			RegisterInputDependencies();

			_bootstrapper.Initialize(_serviceRegistry, _serviceLocator);

			Assert.DoesNotThrow(() => _serviceLocator.GetService<IInputSystem>());
		}

		[Test]
		public void CreateBootstrapper_AndShutdownBootstrapper_DoesNotThrow()
		{
			// Arrange
			RegisterInputDependencies();
			_bootstrapper.Initialize(_serviceRegistry, _serviceLocator);

			// Assert
			Assert.DoesNotThrow(() => _bootstrapper.Shutdown());
		}

		[Test]
		public void ShutdownBeforeInitialize_DoesNotThrow()
		{
			// Assert
			Assert.DoesNotThrow(() => _bootstrapper.Shutdown());
		}

		[Test]
		public void CreateBootstrapper_InitializeThenGetService_DoesNotThrow()
		{
			// Arrange
			RegisterInputDependencies();

			// Act
			_bootstrapper.Initialize(_serviceRegistry, _serviceLocator);

			// Assert
			Assert.DoesNotThrow(() => _serviceLocator.GetService<IInputSystem>());
		}

		private void RegisterInputDependencies()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": []
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			_serviceRegistry.AddSingleton<IFileSystem>(fileSystem.Object);
			_serviceRegistry.AddSingleton<ICVarSystemService>(cvarSystem);
			_serviceRegistry.AddSingleton<ILoggerService>(_logger);
			_serviceRegistry.AddSingleton<IGameEventRegistryService>(_eventRegistry);
		}
	}
}
