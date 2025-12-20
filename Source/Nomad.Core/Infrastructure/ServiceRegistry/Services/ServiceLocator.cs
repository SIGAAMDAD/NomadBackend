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

using NomadCore.Infrastructure.ServiceRegistry.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace NomadCore.Infrastructure.ServiceRegistry.Services
{
    /*
	===================================================================================
	
	ServiceLocator
	
	===================================================================================
	*/
    /// <summary>
    /// 
    /// </summary>

    public sealed class ServiceLocator(ServiceCollection collection) : IServiceLocator
    {
        public IServiceRegistry Collection => _collection;
        private readonly ServiceCollection _collection = collection;

        private readonly ConcurrentDictionary<Type, object> _singletonCache = new();
        private readonly ConcurrentDictionary<Type, Func<IServiceLocator, object>> _factoryCache = new();
        private readonly ConcurrentDictionary<Type, object> _scopedInstances = new();

        private readonly ThreadLocal<Dictionary<Type, object>> _scopeInstances = new(() => new Dictionary<Type, object>());

        private bool _isDisposed;

        /*
		===============
		Dispose
		===============
		*/
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _singletonCache.Clear();
            _factoryCache.Clear();
            _scopedInstances.Clear();
            _scopeInstances.Dispose();
            _collection.Dispose();
        }

        /*
		===============
		CreateInstance
		===============
		*/
        public TService CreateInstance<TService>() where TService : class
        {
            var type = typeof(TService);
            var constructor = GetConstructor(type);
            var parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                return (TService)Activator.CreateInstance(type);
            }

            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = ResolveService(_collection.GetDescriptor(parameters[i].ParameterType));
            }

            return (TService)constructor.Invoke(args);
        }

        /*
		===============
		CreateScope
		===============
		*/
        public IServiceScope CreateScope()
        {
            return new ServiceScope(this);
        }

        /*
		===============
		GetService
		===============
		*/
        public TService GetService<TService>() where TService : class
        {
            if (TryGetService<TService>(out var service))
            {
                return service;
            }
            throw new InvalidOperationException($"Service {typeof(TService)} not yet registered");
        }

        /*
		===============
		GetServices
		===============
		*/
        public IEnumerable<TService> GetServices<TService>() where TService : class
        {
            return null;
        }

        /*
		===============
		TryGetService
		===============
		*/
        public bool TryGetService<TService>(out TService service) where TService : class
        {
            var descriptor = _collection.GetDescriptor(typeof(TService));
            if (descriptor == null)
            {
                service = default;
                return false;
            }
            service = (TService)ResolveService(descriptor);
            return true;
        }

        /*
		===============
		ResolveService
		===============
		*/
        private object ResolveService(ServiceDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new InvalidOperationException("Service not registered");
            }

            return descriptor.Lifetime switch
            {
                ServiceLifetime.Singleton => GetOrCreateSingleton(descriptor),
                ServiceLifetime.Transient => CreateTransient(descriptor),
                ServiceLifetime.Scoped => GetOrCreateScoped(descriptor),
                _ => throw new ArgumentOutOfRangeException($"Invalid lifetime {descriptor.Lifetime}")
            };
        }

        /*
		===============
		GetOrCreateSingleton
		===============
		*/
        private object GetOrCreateSingleton(ServiceDescriptor descriptor)
        {
            if (descriptor.Instance != null)
            {
                return descriptor.Instance;
            }
            return _singletonCache.GetOrAdd(descriptor.ServiceType, _ => CreateServiceInstance(descriptor));
        }

        /*
		===============
		CreateTransient
		===============
		*/
        private object CreateTransient(ServiceDescriptor descriptor)
        {
            return CreateServiceInstance(descriptor);
        }

        /*
		===============
		GetOrCreateScoped
		===============
		*/
        private object GetOrCreateScoped(ServiceDescriptor descriptor)
        {
            var scopeDict = _scopeInstances.Value;
            if (scopeDict.TryGetValue(descriptor.ServiceType, out var instance))
            {
                return instance;
            }
            instance = CreateServiceInstance(descriptor);
            scopeDict[descriptor.ServiceType] = instance;
            return instance;
        }

        /*
		===============
		CreateServiceInstance
		===============
		*/
        private object CreateServiceInstance(ServiceDescriptor descriptor)
        {
            if (descriptor.Factory != null)
            {
                return descriptor.Factory.Invoke(this);
            }
            if (descriptor.ImplementationType != null)
            {
                return CreateInstance(descriptor.ImplementationType);
            }
            throw new InvalidOperationException($"Cannot create instance for {descriptor.ServiceType}");
        }

        /*
		===============
		CreateInstance
		===============
		*/
        private object CreateInstance(Type implementationType)
        {
            if (_factoryCache.TryGetValue(implementationType, out var factory))
            {
                return factory.Invoke(this);
            }
            return CreateInstanceWithReflection(implementationType);
        }

        /*
		===============
		CreateInstanceWithReflection
		===============
		*/
        private object CreateInstanceWithReflection(Type type)
        {
            var constructor = GetConstructor(type);
            var parameters = constructor.GetParameters();

            if (parameters.Length == 0)
            {
                var instance = Activator.CreateInstance(type);
                _collection.TrackDisposable(instance);
                return instance;
            }

            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var paramDescriptor = _collection.GetDescriptor(paramType);
                if (paramDescriptor == null)
                {
                    throw new InvalidOperationException($"Cannot resolve parameter {paramType} for {type}");
                }
                args[i] = ResolveService(paramDescriptor);
            }

            var instance2 = constructor.Invoke(args);
            _collection.TrackDisposable(instance2);
            return instance2;
        }

        /*
		===============
		GetConstructor
		===============
		*/
        private ConstructorInfo GetConstructor(Type type)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"No public constructor found for {type}");
            }
            return constructors[0];
        }

        private void BuildFactoryCache()
        {
            // TODO: source generation
        }
    };
};