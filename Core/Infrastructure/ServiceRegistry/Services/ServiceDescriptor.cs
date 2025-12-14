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

using System.Runtime.CompilerServices;
using System;
using NomadCore.Infrastructure.ServiceRegistry.Interfaces;

namespace NomadCore.Infrastructure.ServiceRegistry.Services {
	/*
	===================================================================================
	
	ServiceDescriptor
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public class ServiceDescriptor( Type serviceType, Type? implementationType, ServiceLifetime lifetime, Func<IServiceLocator, object>? factory, object? instance = null ) : IEquatable<ServiceDescriptor> {
		public Type ServiceType { get; } = serviceType;
		public Type? ImplementationType { get; } = implementationType;
		public ServiceLifetime Lifetime { get; } = lifetime;
		public Func<IServiceLocator, object>? Factory { get; } = factory;
		public readonly object? Instance = instance;

		/*
		===============
		CreateSingleton
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ServiceDescriptor CreateSingleton<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			return new ServiceDescriptor( typeof( TService ), typeof( TImplementation ), ServiceLifetime.Singleton, null );
		}

		/*
		===============
		CreateSingleton
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ServiceDescriptor CreateSingleton<TService>( TService instance )
			where TService : class
		{
			return new ServiceDescriptor( typeof( TService ), instance.GetType(), ServiceLifetime.Singleton, null, instance );
		}

		/*
		===============
		CreateSingleton
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ServiceDescriptor CreateSingleton<TService>( Func<IServiceLocator, TService> factory )
			where TService : class
		{
			return new ServiceDescriptor( typeof( TService ), null, ServiceLifetime.Singleton, provider => factory( provider ) );
		}

		/*
		===============
		CreateTransient
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ServiceDescriptor CreateTransient<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			return new ServiceDescriptor( typeof( TService ), typeof( TImplementation ), ServiceLifetime.Transient, null );
		}

		/*
		===============
		CreateTransient
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ServiceDescriptor CreateTransient<TService>( Func<IServiceLocator, TService> factory )
			where TService : class
		{
			return new ServiceDescriptor( typeof( TService ), null, ServiceLifetime.Transient, provider => factory( provider ) );
		}

		/*
		===============
		CreateScope d
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static ServiceDescriptor CreateScoped<TService, TImplementation>()
			where TService : class
			where TImplementation : class, TService
		{
			return new ServiceDescriptor( typeof( TService ), typeof( TImplementation ), ServiceLifetime.Scoped, null );
		}

		/*
		===============
		Equals
		===============
		*/
		public bool Equals( ServiceDescriptor? other ) {
			return other is not null && other.ServiceType == ServiceType && other.ImplementationType == ImplementationType && other.Lifetime == Lifetime;
		}
	};
};