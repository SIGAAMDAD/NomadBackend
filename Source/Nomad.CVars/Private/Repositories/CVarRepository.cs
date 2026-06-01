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

using System.Collections.Generic;
using System.Collections.Concurrent;
using Nomad.Core.CVars;
using Nomad.Core.Util;
using Nomad.Core.Compatibility.Guards;
using System;
using Nomad.CVars.Private.Entities;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.CVars.Exceptions;

namespace Nomad.CVars.Private.Repositories
{
	/*
	===================================================================================

	CVarRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class CVarRepository
	{
		public ICollection<ICVar> CVars => _cvars.Values;
		private readonly ConcurrentDictionary<InternString, ICVar> _cvars = new();

		private readonly IGameEventRegistryService _eventFactory;
		private readonly ILoggerService _logger;

		/*
		===============
		CVarRepository
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="eventFactory"></param>
		public CVarRepository( ILoggerService logger, IGameEventRegistryService eventFactory )
		{
			_logger = logger ?? throw new ArgumentNullException( nameof( logger ) );
			_eventFactory = eventFactory ?? throw new ArgumentNullException( nameof( eventFactory ) );
		}

		/*
		===============
		AddCVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="createInfo"></param>
		/// <param name="eventFactory"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException"></exception>
		public ICVar<T> AddCVar<T>( CVarCreateInfo<T> createInfo, IGameEventRegistryService eventFactory )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( createInfo.Name );

			var name = new InternString( createInfo.Name );
			if ( _cvars.TryGetValue( name, out ICVar? var ) ) {
				if ( var is ICVar<T> value ) {
					return value;
				}
				throw new InvalidCastException( $"CVar {createInfo.Name} found in CVarSystem cache isn't a valid CVar object!" );
			}

			ICVar<T> cvar = new CVar<T>( eventFactory, createInfo );
			_cvars.TryAdd( name, cvar );
			return cvar;
		}

		/*
		===============
		Find
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ICVar? Find( string name )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );
			return _cvars.TryGetValue( new InternString( name ), out ICVar? cvar ) ? cvar : null;
		}

		/*
		===============
		TryFind
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="cvar"></param>
		/// <returns></returns>
		public bool TryFind( string name, out ICVar? cvar )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );
			return _cvars.TryGetValue( new InternString( name ), out cvar );
		}

		/*
		===============
		TryFind
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <param name="cvar"></param>
		/// <returns></returns>
		public bool TryFind<T>( string name, out ICVar<T>? cvar )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( _cvars.TryGetValue( new InternString( name ), out ICVar? var ) && var is ICVar<T> typedVar ) {
				cvar = typedVar;
				return true;
			}
			cvar = null;
			return false;
		}

		/*
		===============
		Unregister
		===============
		*/
		/// <summary>
		/// Removes the given CVar from the cache.
		/// </summary>
		/// <param name="cvar">The cvar to remove</param>
		public void Unregister( ICVar cvar )
		{
			ArgumentGuard.ThrowIfNull( cvar );
			_cvars.TryRemove( new InternString( cvar.Name ), out _ );
		}

		/*
		===============
		CVarExists
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool CVarExists( string name )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );
			return _cvars.ContainsKey( new InternString( name ) );
		}

		/*
		===============
		CVarExists
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool CVarExists<T>( string name )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );
			if ( _cvars.TryGetValue( new InternString( name ), out var cvar ) ) {
				return cvar is ICVar<T>;
			}
			return false;
		}

		/*
		===============
		GetCVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException"></exception>
		public ICVar<T>? GetCVar<T>( string name )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( !_cvars.TryGetValue( new InternString( name ), out var cvar ) ) {
				_logger.PrintError( $"CVarSystem.GetCVar: no cvar found for name '{name}'!" );
				return null;
			}
			if ( cvar is ICVar<T> typedVar ) {
				return typedVar;
			}
			throw new CVarTypeMismatchException( typeof( T ), cvar.Type.GetSystemType() );
		}

		/*
		===============
		GetCVar
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public ICVar? GetCVar( string name )
		{
			ArgumentGuard.ThrowIfNullOrEmpty( name );

			if ( !_cvars.TryGetValue( new InternString( name ), out var cvar ) ) {
				_logger.PrintError( $"CVarSystem.GetCVar: no cvar found for name '{name}'!" );
				return null;
			}
			return cvar;
		}

		/*
		===============
		GetCVarsWithValueType
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ICVar<T>[] GetCVarsWithValueType<T>()
		{
			List<ICVar<T>> cvars = new List<ICVar<T>>();

			foreach ( var cvar in _cvars ) {
				if ( cvar.Value is ICVar<T> typedVar ) {
					cvars.Add( typedVar );
				}
			}

			return cvars.ToArray();
		}
	};
};
