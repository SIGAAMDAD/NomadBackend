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

using Nomad.Core.FileSystem;
using NUnit.Framework;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.ServiceRegistry.Services;
using System;
using Nomad.Core.Engine.Services;
using Nomad.Core.Logger;
using Nomad.Save.Services;
using Moq;
using Nomad.Core.Events;
using Nomad.Events;
using Nomad.Core.CVars;
using Nomad.Save.Private;
using System.IO;
using Nomad.Core.ServiceRegistry;

namespace Nomad.Save.Tests
{
	[TestFixture]
	[Category("Nomad.Save")]
	[Category("Bootstrap")]
	[Category("Integration")]
	[Category("Unit")]
	public class SaveBootstrapperTests
	{
		private SaveBootstrapper _bootstrapper;
		private IServiceLocator _serviceLocator;
		private ServiceCollection _serviceRegistry;

		[SetUp]
		public void Setup()
		{
			_serviceRegistry = new ServiceCollection();
			_serviceLocator = new ServiceLocator(_serviceRegistry);

			_bootstrapper = new SaveBootstrapper();
		}

		[TearDown]
		public void TearDown()
		{
			_serviceLocator?.Dispose();
			_serviceRegistry?.Dispose();
		}

		[Test]
		public void CreateBootstrapper_InitializeWithValidServices_DoesNotThrow()
		{
			CreateValidServices();

			Assert.DoesNotThrow(() => _bootstrapper.Initialize(_serviceRegistry, _serviceLocator));
		}

		[Test]
		public void CreateBootstrapper_InitializeWithoutExistingServices_ThrowsInvalidOperationException()
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

		private void CreateValidServices()
		{
			var logger = new MockLogger();
			var eventRegistry = new GameEventRegistry(logger);
			var cvarSystem = new MockCVarSystem(eventRegistry);
			cvarSystem.SetCVar(Constants.CVars.AUTO_SAVE_ENABLED, true);
			cvarSystem.SetCVar(Constants.CVars.AUTO_SAVE_INTERVAL, 1000);
			cvarSystem.SetCVar(Constants.CVars.BACKUP_DIRECTORY, Path.GetTempPath());
			cvarSystem.SetCVar(Constants.CVars.CHECKSUM_ENABLED, true);
			cvarSystem.SetCVar(Constants.CVars.VERIFY_AFTER_WRITE, true);
			cvarSystem.SetCVar(Constants.CVars.LOG_SERIALIZATION_TREE, true);
			cvarSystem.SetCVar(Constants.CVars.COMPRESSION_ENABLED, true);
			cvarSystem.SetCVar(Constants.CVars.LOG_WRITE_TIMINGS, true);
			cvarSystem.SetCVar(Constants.CVars.DEBUG_LOGGING, true);
			cvarSystem.SetCVar(Constants.CVars.MAX_BACKUPS, 4);
			cvarSystem.SetCVar(Constants.CVars.DATA_PATH, Path.GetTempPath());

			var fileSystem = new Mock<IFileSystem>();
			fileSystem.Setup(p => p.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns((string path, string filter, bool recursive) => Directory.GetFiles(path, filter));

			_serviceRegistry.Register<IEngineService, MockEngineService>(ServiceLifetime.Singleton);
			_serviceRegistry.AddSingleton<ILoggerService>(logger);
			_serviceRegistry.AddSingleton<ICVarSystemService>(cvarSystem);
			_serviceRegistry.AddSingleton<IGameEventRegistryService>(eventRegistry);
			_serviceRegistry.AddSingleton(fileSystem.Object);
		}

		[Test]
		public void CreateBootstrapper_GetService_DoesNotThrow()
		{
			CreateValidServices();

			_bootstrapper.Initialize(_serviceRegistry, _serviceLocator);

			Assert.DoesNotThrow(() => _serviceLocator.GetService<IFileSystem>());
		}

		[Test]
		public void CreateBootstrapper_AndShutdownBootstrapper_DoesNotThrow()
		{
			// Arrange
			CreateValidServices();
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
			CreateValidServices();

			// Act
			_bootstrapper.Initialize(_serviceRegistry, _serviceLocator);

			// Assert
			Assert.DoesNotThrow(() => _serviceLocator.GetService<ISaveDataProvider>());
		}
	}
}
