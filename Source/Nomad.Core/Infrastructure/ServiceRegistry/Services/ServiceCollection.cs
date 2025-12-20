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
using System.Collections.Concurrent;
using System.Threading;
using System;
using System.Collections.Generic;

namespace NomadCore.Infrastructure.ServiceRegistry.Services {
	/*
	===================================================================================
	
	ServiceCollection
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class ServiceCollection : IServiceRegistry {
		private readonly ConcurrentDictionary<Type, ServiceDescriptor> _descriptors = new ConcurrentDictionary<Type, ServiceDescriptor>();
		private readonly ConcurrentBag<IDisposable> _disposables = new ConcurrentBag<IDisposable>();
		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			foreach ( var disposable in _disposables ) {
				disposable.Dispose();
			}
			_disposables.Clear();
			_descriptors.Clear();
			_lock.Dispose();
		}
		
		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <typeparam name="TImplementation"></typeparam>
		/// <param name="lifetime"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public TService Register<TService, TImplementation>( ServiceLifetime lifetime )
			where TService : class
			where TImplementation : class, TService
		{
			var descriptor = lifetime switch {
				ServiceLifetime.Singleton => ServiceDescriptor.CreateSingleton<TService, TImplementation>(),
				ServiceLifetime.Transient => ServiceDescriptor.CreateTransient<TService, TImplementation>(),
				ServiceLifetime.Scoped => ServiceDescriptor.CreateScoped<TService, TImplementation>(),
				_ => throw new ArgumentOutOfRangeException( nameof( lifetime ) )
			};
			_descriptors[ typeof( TService ) ] = descriptor;
			return (TService)descriptor.Instance;
		}

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <param name="factory"></param>
		/// <param name="lifetime"></param>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public TService Register<TService>( Func<IServiceLocator, TService> factory, ServiceLifetime lifetime )
			where TService : class
		{
			var descriptor = lifetime switch {
				ServiceLifetime.Singleton => ServiceDescriptor.CreateSingleton( factory ),
				ServiceLifetime.Transient => ServiceDescriptor.CreateTransient( factory ),
				ServiceLifetime.Scoped => throw new NotSupportedException( "Scoped factory not supported yet" ),
				_ => throw new ArgumentOutOfRangeException( nameof( lifetime ) )
			};
			_descriptors[ typeof( TService ) ] = descriptor;
			return (TService)descriptor.Instance;
		}

		/*
		===============
		RegisterSingleton
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <param name="instance"></param>
		public TService RegisterSingleton<TService>( TService instance )
			where TService : class
		{
			var descriptor = ServiceDescriptor.CreateSingleton( instance );
			_descriptors[ typeof( TService ) ] = descriptor;
			TrackDisposable( instance );
			return (TService)descriptor.Instance;
		}

		/*
		===============
		RegisterSingleton
		===============
		*/
		public TService RegisterSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			var descriptor = ServiceDescriptor.CreateSingleton<TService, TImplementation>();
			_descriptors[ typeof( TService ) ] = descriptor;
			TrackDisposable( descriptor.Instance );
			return (TService)descriptor.Instance;
		}

		/*
		===============
		IsRegistered
		===============
		*/
		public bool IsRegistered<TService>()
			where TService : class
		{
			return _descriptors.ContainsKey( typeof( TService ) );
		}

		/*
		===============
		GetDescriptors
		===============
		*/
		public IEnumerable<ServiceDescriptor> GetDescriptors() {
			return _descriptors.Values;
		}

		/*
		===============
		TrackDisposable
		===============
		*/
		internal void TrackDisposable( object instance ) {
			if ( instance is IDisposable disposable ) {
				_disposables.Add( disposable );
			}
		}

		/*
		===============
		GetDescriptor
		===============
		*/
		internal ServiceDescriptor GetDescriptor( Type serviceType ) {
			_descriptors.TryGetValue( serviceType, out var descriptor );
			return descriptor;
		}
	};
};