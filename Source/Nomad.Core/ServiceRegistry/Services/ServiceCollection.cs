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

using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using Nomad.Core.ServiceRegistry.Interfaces;

namespace Nomad.Core.ServiceRegistry.Services
{
    /// <summary>
    ///
    /// </summary>
    public sealed class ServiceCollection : IServiceRegistry
    {
        private readonly ConcurrentDictionary<Type, ServiceDescriptor> _descriptors = new ConcurrentDictionary<Type, ServiceDescriptor>();
        private readonly ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();

        /// <summary>
        ///
        /// </summary>
        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
            _descriptors.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="lifetime"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public TService Register<TService, TImplementation>(ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            ServiceDescriptor descriptor = lifetime switch
            {
                ServiceLifetime.Singleton => ServiceDescriptor.CreateSingleton<TService, TImplementation>(),
                ServiceLifetime.Transient => ServiceDescriptor.CreateTransient<TService, TImplementation>(),
                ServiceLifetime.Scoped => ServiceDescriptor.CreateScoped<TService, TImplementation>(),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
            };
            _descriptors[typeof(TService)] = descriptor;
            return (TService)descriptor.Instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="factory"></param>
        /// <param name="lifetime"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public TService Register<TService>(Func<IServiceLocator, TService> factory, ServiceLifetime lifetime)
            where TService : class
        {
            ServiceDescriptor descriptor = lifetime switch
            {
                ServiceLifetime.Singleton => ServiceDescriptor.CreateSingleton(factory),
                ServiceLifetime.Transient => ServiceDescriptor.CreateTransient(factory),
                ServiceLifetime.Scoped => throw new NotSupportedException("Scoped factory not supported yet"),
                _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
            };
            _descriptors[typeof(TService)] = descriptor;
            return (TService)descriptor.Instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="instance"></param>
        public TService RegisterSingleton<TService>(TService instance)
            where TService : class
        {
            var descriptor = ServiceDescriptor.CreateSingleton(instance);
            _descriptors[typeof(TService)] = descriptor;
            TrackDisposable(instance);
            return (TService)descriptor.Instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        public TService RegisterSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var descriptor = ServiceDescriptor.CreateSingleton<TService, TImplementation>();
            _descriptors[typeof(TService)] = descriptor;
            TrackDisposable(descriptor.Instance);
            return (TService)descriptor.Instance;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public bool IsRegistered<TService>()
            where TService : class
        {
            return _descriptors.ContainsKey(typeof(TService));
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ServiceDescriptor> GetDescriptors()
        {
            return _descriptors.Values;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="instance"></param>
        internal void TrackDisposable(object instance)
        {
            if (instance is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        internal ServiceDescriptor GetDescriptor(Type serviceType)
        {
            _descriptors.TryGetValue(serviceType, out var descriptor);
            return descriptor;
        }
    };
};
