/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Nomad.Core.ServiceRegistry.Interfaces;

namespace Nomad.Core.ServiceRegistry.Services
{
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

        /// <summary>
        ///
        /// </summary>
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

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IServiceScope CreateScope()
        {
            return new ServiceScope(this);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public TService GetService<TService>() where TService : class
        {
            if (TryGetService<TService>(out var service))
            {
                return service;
            }
            throw new InvalidOperationException($"Service {typeof(TService)} not yet registered");
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public IEnumerable<TService> GetServices<TService>() where TService : class
        {
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="service"></param>
        /// <returns></returns>
        public bool TryGetService<TService>(out TService service) where TService : class
        {
            ServiceDescriptor descriptor = _collection.GetDescriptor(typeof(TService));
            if (descriptor == null)
            {
                service = default;
                return false;
            }
            service = (TService)ResolveService(descriptor);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        private object GetOrCreateSingleton(ServiceDescriptor descriptor)
        {
            if (descriptor.Instance != null)
            {
                return descriptor.Instance;
            }
            return _singletonCache.GetOrAdd(descriptor.ServiceType, _ => CreateServiceInstance(descriptor));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        private object CreateTransient(ServiceDescriptor descriptor)
        {
            return CreateServiceInstance(descriptor);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        private object GetOrCreateScoped(ServiceDescriptor descriptor)
        {
            Dictionary<Type, object>? scopeDict = _scopeInstances.Value;
            if (scopeDict.TryGetValue(descriptor.ServiceType, out var instance))
            {
                return instance;
            }
            instance = CreateServiceInstance(descriptor);
            scopeDict[descriptor.ServiceType] = instance;
            return instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="implementationType"></param>
        /// <returns></returns>
        private object CreateInstance(Type implementationType)
        {
            if (_factoryCache.TryGetValue(implementationType, out var factory))
            {
                return factory.Invoke(this);
            }
            return CreateInstanceWithReflection(implementationType);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
