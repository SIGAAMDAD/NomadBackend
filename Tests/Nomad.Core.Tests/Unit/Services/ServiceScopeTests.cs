using System;
using System.Linq;
using NUnit.Framework;
using Nomad.Core.ServiceRegistry;
using Nomad.Core.ServiceRegistry.Services;

namespace Nomad.Core.Tests {
	public interface IServiceScopeDependency { }
	public sealed class ServiceScopeDependency : IServiceScopeDependency { }

	public interface IServiceScopeSubject { IServiceScopeDependency Dependency { get; } }
	public sealed class ServiceScopeSubject : IServiceScopeSubject {
		public IServiceScopeDependency Dependency { get; }
		public ServiceScopeSubject( IServiceScopeDependency dependency ) {
			Dependency = dependency;
		}
	}

	public interface IServiceScopeDisposable : IDisposable {
		bool IsDisposed { get; }
	}
	public sealed class ServiceScopeDisposable : IServiceScopeDisposable {
		public bool IsDisposed { get; private set; }
		public void Dispose() {
			IsDisposed = true;
		}
	}

	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Services")]
	[Category("Unit")]
	public class ServiceScopeTests {
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
		public void Constructor_WhenRootIsNull_ThrowsArgumentNullException() {
			Assert.Throws<ArgumentNullException>( () => new ServiceScope( null ) );
		}

		[Test]
		public void CollectionAndServiceLocator_ReturnExpectedObjects() {
			using var scope = new ServiceScope( _locator );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( scope.Collection, Is.SameAs( _collection ) );
				Assert.That( scope.ServiceLocator, Is.SameAs( scope ) );
			}
		}

		[Test]
		public void TryGetService_WhenServiceIsMissing_ReturnsFalseAndNull() {
			using var scope = new ServiceScope( _locator );

			bool found = scope.TryGetService<IServiceScopeDependency>( out var service );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( found, Is.False );
				Assert.That( service, Is.Null );
				Assert.That( () => scope.GetService<IServiceScopeDependency>(), Throws.InvalidOperationException );
			}
		}

		[Test]
		public void GetService_ForScopedRegistration_CachesWithinScopeOnly() {
			_collection.AddScoped<IServiceScopeDependency, ServiceScopeDependency>();
			using var firstScope = new ServiceScope( _locator );
			using var secondScope = new ServiceScope( _locator );

			var first = firstScope.GetService<IServiceScopeDependency>();
			var second = firstScope.GetService<IServiceScopeDependency>();
			var fromOtherScope = secondScope.GetService<IServiceScopeDependency>();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( second, Is.SameAs( first ) );
				Assert.That( fromOtherScope, Is.Not.SameAs( first ) );
			}
		}

		[Test]
		public void GetService_ForScopedRegistration_UsesScopeForConstructorInjection() {
			_collection.AddScoped<IServiceScopeDependency, ServiceScopeDependency>();
			_collection.AddScoped<IServiceScopeSubject, ServiceScopeSubject>();
			using var scope = new ServiceScope( _locator );

			var subject = scope.GetService<IServiceScopeSubject>();

			Assert.That( subject.Dependency, Is.SameAs( scope.GetService<IServiceScopeDependency>() ) );
		}

		[Test]
		public void TryGetService_ForSingletonAndTransientRegistrations_DelegatesToRootLocator() {
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();
			_collection.AddTransient<IServiceScopeDependency, ServiceScopeDependency>();
			using var scope = new ServiceScope( _locator );

			var singletonA = scope.GetService<IServiceLocatorDependency>();
			var singletonB = _locator.GetService<IServiceLocatorDependency>();
			var transientA = scope.GetService<IServiceScopeDependency>();
			var transientB = scope.GetService<IServiceScopeDependency>();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( singletonA, Is.SameAs( singletonB ) );
				Assert.That( transientB, Is.Not.SameAs( transientA ) );
			}
		}

		[Test]
		public void GetServices_ReturnsScopedCacheAndRootSingletonCacheMatches() {
			_collection.AddScoped<IServiceScopeDependency, ServiceScopeDependency>();
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();
			using var scope = new ServiceScope( _locator );

			var scoped = scope.GetService<IServiceScopeDependency>();
			var singleton = _locator.GetService<IServiceLocatorDependency>();
			var services = scope.GetServices<object>().ToArray();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( services, Does.Contain( scoped ) );
				Assert.That( services, Does.Contain( singleton ) );
			}
		}

		[Test]
		public void CreateInstance_DelegatesToRootLocator() {
			_collection.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();
			using var scope = new ServiceScope( _locator );

			var instance = scope.CreateInstance<IServiceLocatorSubject, ServiceLocatorSubject>();

			Assert.That( instance.Dependency, Is.SameAs( _locator.GetService<IServiceLocatorDependency>() ) );
		}

		[Test]
		public void CreateScope_FromScope_ThrowsNotSupportedException() {
			using var scope = new ServiceScope( _locator );

			Assert.Throws<NotSupportedException>( () => scope.CreateScope() );
		}

		[Test]
		public void Dispose_DisposesCachedScopedDisposablesClearsCacheAndIsIdempotent() {
			_collection.AddScoped<IServiceScopeDisposable, ServiceScopeDisposable>();
			var scope = new ServiceScope( _locator );
			var service = scope.GetService<IServiceScopeDisposable>();

			scope.Dispose();
			scope.Dispose();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( service.IsDisposed, Is.True );
				Assert.That( scope.GetServices<object>(), Is.Empty );
			}
		}
	}
}
