using System;
using System.Linq;
using NUnit.Framework;
using Nomad.Core.ServiceRegistry;
using Nomad.Core.ServiceRegistry.Services;

namespace Nomad.Core.Tests {
	public interface IServiceLocatorDependency { }
	public sealed class ServiceLocatorDependency : IServiceLocatorDependency { }

	public interface IServiceLocatorSubject { IServiceLocatorDependency Dependency { get; } }
	public sealed class ServiceLocatorSubject : IServiceLocatorSubject {
		public IServiceLocatorDependency Dependency { get; }
		public ServiceLocatorSubject( IServiceLocatorDependency dependency ) {
			Dependency = dependency;
		}
	}

	public interface IServiceLocatorDisposable : IDisposable {
		bool IsDisposed { get; }
	}
	public sealed class ServiceLocatorDisposable : IServiceLocatorDisposable {
		public bool IsDisposed { get; private set; }
		public void Dispose() {
			IsDisposed = true;
		}
	}

	public interface IServiceLocatorPrivateConstructor { }
	public sealed class ServiceLocatorPrivateConstructor : IServiceLocatorPrivateConstructor {
		private ServiceLocatorPrivateConstructor() {
		}
	}

	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Services")]
	[Category("Unit")]
	public class ServiceLocatorTests {
		private ServiceCollection _collection;
		private ServiceLocator _locator;

		[SetUp]
		public void SetUp() {
			_collection = new ServiceCollection();
			_locator = new ServiceLocator( _collection );
		}

		[TearDown]
		public void TearDown() {
			_locator?.Dispose();
			_collection?.Dispose();
		}

		[Test]
		public void Constructor_WhenCollectionIsNull_ThrowsArgumentNullException() {
			Assert.Throws<ArgumentNullException>( () => new ServiceLocator( null ) );
		}

		[Test]
		public void Collection_ReturnsRootCollection() {
			Assert.That( _locator.Collection, Is.SameAs( _collection ) );
		}

		[Test]
		public void TryGetService_WhenServiceIsMissing_ReturnsFalseAndNull() {
			bool found = _locator.TryGetService<IServiceLocatorDependency>( out var service );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( found, Is.False );
				Assert.That( service, Is.Null );
				Assert.That( () => _locator.GetService<IServiceLocatorDependency>(), Throws.InvalidOperationException );
			}
		}

		[Test]
		public void GetService_ForSingletonRegistration_CreatesAndCachesOneInstance() {
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();

			var first = _locator.GetService<IServiceLocatorDependency>();
			var second = _locator.GetService<IServiceLocatorDependency>();

			Assert.That( second, Is.SameAs( first ) );
		}

		[Test]
		public void GetService_ForExplicitSingletonInstance_ReturnsRegisteredInstance() {
			var instance = new ServiceLocatorDependency();
			_collection.AddSingleton<IServiceLocatorDependency>( instance );

			var resolved = _locator.GetService<IServiceLocatorDependency>();

			Assert.That( resolved, Is.SameAs( instance ) );
		}

		[Test]
		public void GetService_ForTransientRegistration_CreatesNewInstanceEachTime() {
			_collection.AddTransient<IServiceLocatorDependency, ServiceLocatorDependency>();

			var first = _locator.GetService<IServiceLocatorDependency>();
			var second = _locator.GetService<IServiceLocatorDependency>();

			Assert.That( second, Is.Not.SameAs( first ) );
		}

		[Test]
		public void GetService_UsesConstructorInjectionForDependencies() {
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();
			_collection.AddTransient<IServiceLocatorSubject, ServiceLocatorSubject>();

			var subject = _locator.GetService<IServiceLocatorSubject>();

			Assert.That( subject.Dependency, Is.SameAs( _locator.GetService<IServiceLocatorDependency>() ) );
		}

		[Test]
		public void TryGetService_ForScopedRegistrationFromRoot_ThrowsInvalidOperationException() {
			_collection.AddScoped<IServiceLocatorDependency, ServiceLocatorDependency>();

			Assert.Throws<InvalidOperationException>( () => _locator.TryGetService<IServiceLocatorDependency>( out _ ) );
		}

		[Test]
		public void GetServices_ReturnsOnlyResolvedCachedSingletonsMatchingRequestedType() {
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();
			_collection.AddSingleton<IServiceLocatorSubject, ServiceLocatorSubject>();

			Assert.That( _locator.GetServices<object>(), Is.Empty );

			var dependency = _locator.GetService<IServiceLocatorDependency>();
			var objects = _locator.GetServices<object>().ToArray();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( objects, Does.Contain( dependency ) );
				Assert.That( objects.Any( service => service is IServiceLocatorSubject ), Is.False );
			}
		}

		[Test]
		public void CreateInstance_CreatesUnregisteredInstancesAndUsesConstructorInjection() {
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();

			var first = _locator.CreateInstance<IServiceLocatorSubject, ServiceLocatorSubject>();
			var second = _locator.CreateInstance<IServiceLocatorSubject, ServiceLocatorSubject>();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( first, Is.Not.SameAs( second ) );
				Assert.That( first.Dependency, Is.SameAs( _locator.GetService<IServiceLocatorDependency>() ) );
			}
		}

		[Test]
		public void CreateInstance_WhenImplementationHasNoPublicConstructor_ThrowsInvalidOperationException() {
			Assert.Throws<InvalidOperationException>( () => _locator.CreateInstance<IServiceLocatorPrivateConstructor, ServiceLocatorPrivateConstructor>() );
		}

		[Test]
		public void CreateScope_ReturnsServiceScopeAttachedToThisLocator() {
			using var scope = _locator.CreateScope();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( scope, Is.TypeOf<ServiceScope>() );
				Assert.That( scope.Collection, Is.SameAs( _collection ) );
			}
		}

		[Test]
		public void Dispose_ClearsCachesDisposesCollectionAndIsIdempotent() {
			_collection.AddSingleton<IServiceLocatorDisposable, ServiceLocatorDisposable>();
			var service = _locator.GetService<IServiceLocatorDisposable>();

			_locator.Dispose();
			_locator.Dispose();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( service.IsDisposed, Is.True );
				Assert.That( _collection.GetDescriptors(), Is.Empty );
			}
		}
	}
}
