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
using Nomad.Core.ServiceRegistry;
using Nomad.Core.ServiceRegistry.Services;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	public interface IServiceMock { }
	public class ServiceMock : IServiceMock { }

	public interface IServiceMock1 { }
	public class ServiceMock1 : IServiceMock1 { }

	public interface IServiceMock2 { }
	public class ServiceMock2 : IServiceMock2 { }

	public interface IDisposableService : IDisposable
	{
		bool IsDisposed { get; }
	}
	public class DisposableService : IDisposableService
	{
		public bool IsDisposed => _isDisposed;
		private bool _isDisposed = false;

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_isDisposed = true;
		}
	}

	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Services")]
	[Category("Unit")]
	public class ServiceCollectionTests
	{
		private ServiceCollection _collection;

		[SetUp]
		public void Setup()
		{
			_collection = new ServiceCollection();
		}

		[TearDown]
		public void TearDown()
		{
			_collection?.Dispose();
		}

		[Test]
		public void Dispose_DisposeTwice_DoesNotThrow()
		{
			_collection?.Dispose();
			Assert.DoesNotThrow(() => _collection?.Dispose());
		}

		[Test]
		public void Register_WithInvalidLifetime_ThrowsArgumentOutOfRangeException()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _collection.Register<IServiceMock, ServiceMock>((ServiceLifetime)100));
		}

		[Test]
		public void Register_Singleton_CreatesValidSingleton()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Singleton);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
			}
		}

		[Test]
		public void AddSingleton_CreatesValidSingleton()
		{
			_collection.AddSingleton<IServiceMock, ServiceMock>();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
			}
		}

		[Test]
		public void AddTransient_CreatesValidTransient()
		{
			_collection.AddTransient<IServiceMock, ServiceMock>();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
			}
		}

		[Test]
		public void Register_Scoped_CreatesValidScoped()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Scoped);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
			}
		}

		[Test]
		public void Register_Transient_CreatesValidTransient()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Transient);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
			}
		}

		[Test]
		public void AddSingleton_WithObject_AddsSameObject()
		{
			IServiceMock mock = new ServiceMock();
			_collection.AddSingleton(mock);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
				Assert.That(descriptor.Instance, Is.SameAs(mock));
			}
		}

		[Test]
		public void AddSingleton_WithNull_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => _collection.AddSingleton<IServiceMock>(null));
		}

		[Test]
		public void AddScoped_AddsScopedObject()
		{
			_collection.AddScoped<IServiceMock, ServiceMock>();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor), Is.True);
				Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(ServiceMock)));
				Assert.That(descriptor.ServiceType, Is.EqualTo(typeof(IServiceMock)));
				Assert.That(descriptor.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
			}
		}

		[Test]
		public void Register_Singleton_IsRegistered()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Singleton);
			Assert.That(_collection.IsRegistered<IServiceMock>(), Is.True);
		}

		[Test]
		public void Register_Scoped_IsRegistered()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Scoped);
			Assert.That(_collection.IsRegistered<IServiceMock>(), Is.True);
		}

		[Test]
		public void Register_Transient_IsRegistered()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Transient);
			Assert.That(_collection.IsRegistered<IServiceMock>(), Is.True);
		}

		[Test]
		public void GetDescriptors_RegisterManyDescriptors_HasAllRegisteredDescriptors()
		{
			_collection.Register<IServiceMock, ServiceMock>(ServiceLifetime.Singleton);
			_collection.Register<IServiceMock1, ServiceMock1>(ServiceLifetime.Scoped);
			_collection.Register<IServiceMock2, ServiceMock2>(ServiceLifetime.Transient);

			var descriptors = _collection.GetDescriptors();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock), out var descriptor1), Is.True);
				Assert.That(descriptors, Does.Contain(descriptor1));

				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock1), out var descriptor2), Is.True);
				Assert.That(descriptors, Does.Contain(descriptor2));

				Assert.That(_collection.TryGetDescriptor(typeof(IServiceMock2), out var descriptor3), Is.True);
				Assert.That(descriptors, Does.Contain(descriptor3));
			}
		}

		[Test]
		public void CreateDisposableSingleton_IsDisposedWhenCollectionDisposes()
		{
			IDisposableService service = new DisposableService();

			_collection.AddSingleton(service);
			_collection.Dispose();

			Assert.That(service.IsDisposed, Is.True);
		}
	}
}