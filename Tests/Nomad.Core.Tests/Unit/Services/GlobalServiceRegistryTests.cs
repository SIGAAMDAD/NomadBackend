using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Nomad.Core.ServiceRegistry;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.ServiceRegistry.Services;
using GlobalLocator = Nomad.Core.ServiceRegistry.Globals.ServiceLocator;
using GlobalRegistry = Nomad.Core.ServiceRegistry.Globals.ServiceRegistry;

namespace Nomad.Core.Tests {
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Services")]
	[Category("Unit")]
	public class GlobalServiceRegistryTests {
		[SetUp]
		public void SetUp() {
			ResetGlobals();
		}

		[TearDown]
		public void TearDown() {
			ResetGlobals();
		}

		[Test]
		public void ServiceRegistry_Instance_LazilyCreatesServiceCollection() {
			IServiceRegistry first = GlobalRegistry.Instance;
			IServiceRegistry second = GlobalRegistry.Instance;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( first, Is.TypeOf<ServiceCollection>() );
				Assert.That( second, Is.SameAs( first ) );
			}
		}

		[Test]
		public void ServiceRegistry_Initialize_OverridesInstanceAndRejectsNull() {
			var registry = new ServiceCollection();

			InvokeInitialize( typeof( GlobalRegistry ), registry );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( GlobalRegistry.Instance, Is.SameAs( registry ) );
				Assert.That( () => InvokeInitialize( typeof( GlobalRegistry ), null ), Throws.TargetInvocationException.With.InnerException.TypeOf<ArgumentNullException>() );
			}
		}

		[Test]
		public void ServiceRegistry_StaticMethods_DelegateToCurrentRegistry() {
			var registry = new RecordingRegistry();
			InvokeInitialize( typeof( GlobalRegistry ), registry );
			var singleton = new ServiceLocatorDependency();

			GlobalRegistry.Register<IServiceLocatorDependency, ServiceLocatorDependency>( ServiceLifetime.Singleton );
			GlobalRegistry.AddSingleton<IServiceLocatorDependency>( singleton );
			GlobalRegistry.AddSingleton<IServiceLocatorSubject, ServiceLocatorSubject>();
			GlobalRegistry.AddTransient<IServiceScopeDependency, ServiceScopeDependency>();
			IServiceScope scope = GlobalRegistry.AddScoped<IServiceScopeSubject, ServiceScopeSubject>();
			bool registered = GlobalRegistry.IsRegistered<IServiceLocatorDependency>();
			var descriptors = GlobalRegistry.GetDescriptors().ToArray();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( registry.RegisterCount, Is.EqualTo( 1 ) );
				Assert.That( registry.AddSingletonInstance, Is.SameAs( singleton ) );
				Assert.That( registry.AddSingletonTypeCount, Is.EqualTo( 1 ) );
				Assert.That( registry.AddTransientCount, Is.EqualTo( 1 ) );
				Assert.That( registry.AddScopedCount, Is.EqualTo( 1 ) );
				Assert.That( scope, Is.SameAs( registry.Scope ) );
				Assert.That( registered, Is.True );
				Assert.That( descriptors, Is.EqualTo( registry.Descriptors ) );
			}
		}

		[Test]
		public void ServiceLocator_Instance_LazilyCreatesLocatorOverGlobalRegistry() {
			GlobalRegistry.AddSingleton<IServiceLocatorDependency, ServiceLocatorDependency>();

			IServiceLocator first = GlobalLocator.Instance;
			IServiceLocator second = GlobalLocator.Instance;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( first, Is.TypeOf<ServiceLocator>() );
				Assert.That( second, Is.SameAs( first ) );
				Assert.That( first.GetService<IServiceLocatorDependency>(), Is.Not.Null );
			}
		}

		[Test]
		public void ServiceLocator_Initialize_OverridesInstanceAndRejectsNull() {
			using var collection = new ServiceCollection();
			using var locator = new ServiceLocator( collection );

			InvokeInitialize( typeof( GlobalLocator ), locator );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( GlobalLocator.Instance, Is.SameAs( locator ) );
				Assert.That( () => InvokeInitialize( typeof( GlobalLocator ), null ), Throws.TargetInvocationException.With.InnerException.TypeOf<ArgumentNullException>() );
			}
		}

		[Test]
		public void ServiceLocator_StaticMethods_DelegateToCurrentLocator() {
			var locator = new RecordingLocator();
			InvokeInitialize( typeof( GlobalLocator ), locator );

			var required = GlobalLocator.GetService<IServiceLocatorDependency>();
			bool found = GlobalLocator.TryGetService<IServiceLocatorDependency>( out var optional );
			var services = GlobalLocator.GetServices<IServiceLocatorDependency>().ToArray();
			var instance = GlobalLocator.CreateInstance<IServiceLocatorDependency, ServiceLocatorDependency>();
			IServiceScope scope = GlobalLocator.CreateScope();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( required, Is.SameAs( locator.Dependency ) );
				Assert.That( found, Is.True );
				Assert.That( optional, Is.SameAs( locator.Dependency ) );
				Assert.That( services, Is.EqualTo( new[] { locator.Dependency } ) );
				Assert.That( instance, Is.SameAs( locator.Dependency ) );
				Assert.That( scope, Is.SameAs( locator.Scope ) );
				Assert.That( locator.GetServiceCount, Is.EqualTo( 1 ) );
				Assert.That( locator.TryGetServiceCount, Is.EqualTo( 1 ) );
				Assert.That( locator.GetServicesCount, Is.EqualTo( 1 ) );
				Assert.That( locator.CreateInstanceCount, Is.EqualTo( 1 ) );
				Assert.That( locator.CreateScopeCount, Is.EqualTo( 1 ) );
			}
		}

		private static void InvokeInitialize( Type globalType, object instance ) {
			globalType
				.GetMethod( "Initialize", BindingFlags.Static | BindingFlags.NonPublic )
				.Invoke( null, new[] { instance } );
		}

		private static void ResetGlobals() {
			typeof( GlobalLocator )
				.GetField( "_instance", BindingFlags.Static | BindingFlags.NonPublic )
				.SetValue( null, null );
			typeof( GlobalRegistry )
				.GetField( "_instance", BindingFlags.Static | BindingFlags.NonPublic )
				.SetValue( null, null );
		}

		private sealed class RecordingRegistry : IServiceRegistry {
			public readonly IServiceScope Scope = new RecordingScope();
			public ServiceDescriptor[] Descriptors { get; } = {
				ServiceDescriptor.Singleton<IServiceLocatorDependency>( new ServiceLocatorDependency() )
			};
			public int RegisterCount { get; private set; }
			public int AddSingletonTypeCount { get; private set; }
			public int AddTransientCount { get; private set; }
			public int AddScopedCount { get; private set; }
			public object AddSingletonInstance { get; private set; }

			public IServiceRegistry Register<TService, TImplementation>( ServiceLifetime lifetime )
				where TService : class
				where TImplementation : class, TService {
				RegisterCount++;
				return this;
			}

			public IServiceRegistry AddSingleton<TService>( TService instance )
				where TService : class {
				AddSingletonInstance = instance;
				return this;
			}

			public IServiceRegistry AddSingleton<TService, TImplementation>()
				where TService : class
				where TImplementation : class, TService {
				AddSingletonTypeCount++;
				return this;
			}

			public IServiceRegistry AddTransient<TService, TImplementation>()
				where TService : class
				where TImplementation : class, TService {
				AddTransientCount++;
				return this;
			}

			public IServiceScope AddScoped<TService, TImplementation>()
				where TService : class
				where TImplementation : class, TService {
				AddScopedCount++;
				return Scope;
			}

			public bool IsRegistered<TService>() where TService : class => true;
			public IEnumerable<ServiceDescriptor> GetDescriptors() => Descriptors;
			public void Dispose() { }
		}

		private sealed class RecordingLocator : IServiceLocator {
			public readonly IServiceLocatorDependency Dependency = new ServiceLocatorDependency();
			public readonly IServiceScope Scope = new RecordingScope();
			public int GetServiceCount { get; private set; }
			public int TryGetServiceCount { get; private set; }
			public int GetServicesCount { get; private set; }
			public int CreateInstanceCount { get; private set; }
			public int CreateScopeCount { get; private set; }
			public IServiceRegistry Collection { get; } = new ServiceCollection();

			public TService GetService<TService>() where TService : class {
				GetServiceCount++;
				return Dependency as TService;
			}

			public bool TryGetService<TService>( out TService service ) where TService : class {
				TryGetServiceCount++;
				service = Dependency as TService;
				return service != null;
			}

			public IEnumerable<TService> GetServices<TService>() where TService : class {
				GetServicesCount++;
				if ( Dependency is TService service ) {
					yield return service;
				}
			}

			public TService CreateInstance<TService, TImplementation>()
				where TService : class
				where TImplementation : class, TService {
				CreateInstanceCount++;
				return Dependency as TService;
			}

			public IServiceScope CreateScope() {
				CreateScopeCount++;
				return Scope;
			}

			public void Dispose() { }
		}

		private sealed class RecordingScope : IServiceScope {
			public IServiceLocator ServiceLocator => this;
			public IServiceRegistry Collection { get; } = new ServiceCollection();
			public TService GetService<TService>() where TService : class => throw new NotImplementedException();
			public bool TryGetService<TService>( out TService service ) where TService : class {
				service = null;
				return false;
			}
			public IEnumerable<TService> GetServices<TService>() where TService : class => Array.Empty<TService>();
			public TService CreateInstance<TService, TImplementation>()
				where TService : class
				where TImplementation : class, TService => throw new NotImplementedException();
			public IServiceScope CreateScope() => throw new NotSupportedException();
			public void Dispose() { }
		}
	}
}
