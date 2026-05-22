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
using Gee.External.Capstone;
using Moq;
using Nomad.Core.Abstractions;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.ServiceRegistry.Services;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Bootstrap")]
	[Category("Integration")]
	[Category("UnitTests")]
	public class NomadFrameworkBootstrapperTests
	{
		public class MockService
		{
		}

		public class MockBootstrapper : IBootstrapper
		{
			public MockBootstrapper()
			{
			}

			public void Initialize(IServiceRegistry registry, IServiceLocator locator)
			{
				registry.AddSingleton(new MockService());
			}

			public void Shutdown()
			{
			}
		}

		private IServiceLocator _serviceLocator;
		private ServiceCollection _serviceRegistry;

		[SetUp]
		public void Setup()
		{
			_serviceRegistry = new ServiceCollection();
			_serviceLocator = new ServiceLocator(_serviceRegistry);
		}

		[TearDown]
		public void TearDown()
		{
			_serviceLocator?.Dispose();
			_serviceRegistry?.Dispose();
		}

		[Test]
		public void Create_WithValidParameters_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => new NomadFrameworkBootstrapper(_serviceRegistry, _serviceLocator));
		}

		[Test]
		public void Create_WithNullRegistry_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new NomadFrameworkBootstrapper(null!, _serviceLocator));
		}

		[Test]
		public void Create_WithNullLocator_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => new NomadFrameworkBootstrapper(_serviceRegistry, null!));
		}

		[Test]
		public void Dispose_DisposeAgain_DoesNotThrow()
		{
			var bootstrapper = new NomadFrameworkBootstrapper(_serviceRegistry, _serviceLocator);

			bootstrapper.Dispose();
			Assert.DoesNotThrow(() => bootstrapper.Dispose());
		}

		[Test]
		public void AddBootstrapper_WithInstanceThenBootstrap_InitializesSystem()
		{
			// Arrange
			var system = new Mock<IBootstrapper>();
			bool initialized = false;
			system.Setup(s => s.Initialize(It.IsAny<IServiceRegistry>(), It.IsAny<IServiceLocator>())).Callback(() => initialized = true);

			// Act
			var bootstrapper = new NomadFrameworkBootstrapper(_serviceRegistry, _serviceLocator)
				.AddBootstrapper(system.Object);
			bootstrapper.Bootstrap();

			// Assert
			Assert.That(initialized, Is.True);
		}

		[Test]
		public void AddBootstrapper_ConstructInBootstrapperThenBootstrap_InitializesSystem()
		{
			// Act
			var bootstrapper = new NomadFrameworkBootstrapper(_serviceRegistry, _serviceLocator)
				.AddBootstrapper<MockBootstrapper>();
			bootstrapper.Bootstrap();

			// Assert
			Assert.That(_serviceRegistry.IsRegistered<MockService>(), Is.True);
		}
	}
}